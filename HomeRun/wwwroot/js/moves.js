// Move animations: simple SVG figures animated between a few joint-angle poses per move, keyed by the move names in plan.json.
(() => {
  // Segment lengths and line widths, in figure units (about 166 tall).
  const L = { torso: 52, neck: 7, headR: 11, upper: 32, fore: 30, thigh: 42, shin: 40, foot: 13 };
  const W = { torso: 24, torsoFront: 30, pelvis: 13, thigh: 15, shin: 12, foot: 8, upper: 11, fore: 9, neck: 8 };
  const COLOURS = { near: "#d6d9de", far: "#7f858e", farFront: "#b3b8bf", hiNear: "#4d94ff", hiFar: "#2c64bd", hiFarFront: "#3b7ee6", floor: "#454c53", shadow: "rgba(0,0,0,0.35)", prop: "#5a6168" };
  const SHOULDER_WIDTH = 18;
  const HIP_WIDTH = 8;

  const rad = (d) => (d * Math.PI) / 180;
  const dir = (a) => [Math.cos(rad(a)), Math.sin(rad(a))];
  const add = (p, q) => [p[0] + q[0], p[1] + q[1]];
  const sub = (p, q) => [p[0] - q[0], p[1] - q[1]];
  const mul = (p, k) => [p[0] * k, p[1] * k];
  const angle = (p, q) => (Math.atan2(q[1] - p[1], q[0] - p[0]) * 180) / Math.PI;

  // Two bone IK: finds the middle joint so the end reaches the target, bending one way or the other.
  function ik(root, target, a, b, bend) {
    const v = sub(target, root);
    const d = Math.min(Math.max(Math.hypot(v[0], v[1]), Math.abs(a - b) + 0.01), a + b - 0.01);
    const base = Math.atan2(v[1], v[0]);
    const alpha = Math.acos(Math.min(1, Math.max(-1, (a * a + d * d - b * b) / (2 * a * d))));
    const joint = add(root, [a * Math.cos(base + bend * alpha), a * Math.sin(base + bend * alpha)]);
    return [joint, add(root, [d * Math.cos(base), d * Math.sin(base)])];
  }

  // A limb is either two absolute angles, or a point to stay planted on. "s" foreshortens each segment.
  function limb(root, spec, a, b, offset) {
    const s = spec.s ?? [1, 1];
    if (spec.to) {
      return ik(root, add(spec.to, offset), a * s[0], b * s[1], spec.bend ?? 1);
    }
    const joint = add(root, mul(dir(spec.a[0]), a * s[0]));
    return [joint, add(joint, mul(dir(spec.a[1]), b * s[1]))];
  }

  // Turns a pose into joint positions. Side-on moves nudge the far limbs for depth; front-on and top-down moves spread them across the body.
  function solve(pose, move) {
    const ts = pose.ts ?? 1;
    const torso = mul(dir(pose.torso), L.torso * ts);
    const hip = pose.root === "shoulder" ? sub(pose.at, torso) : pose.at;
    const shoulder = pose.root === "shoulder" ? pose.at : add(hip, torso);
    const headAngle = pose.head ?? pose.torso;
    const result = { hip, shoulder, neck: add(shoulder, mul(dir(headAngle), L.neck * ts)), head: add(shoulder, mul(dir(headAngle), (L.neck + L.headR) * ts)) };
    const spread = move.view === "front" || move.view === "top";
    const perp = dir(pose.torso - 90);
    for (const side of ["N", "F"]) {
      const sign = side === "N" ? 1 : -1;
      const offset = spread || side === "N" ? [0, 0] : move.depth;
      const armRoot = spread ? add(shoulder, mul(perp, SHOULDER_WIDTH * sign)) : add(shoulder, offset);
      const legRoot = spread ? add(hip, mul(perp, HIP_WIDTH * sign)) : add(hip, offset);
      const [elbow, hand] = limb(armRoot, pose["arm" + side], L.upper, L.fore, offset);
      const [knee, ankle] = limb(legRoot, pose["leg" + side], L.thigh, L.shin, offset);
      const footAngle = pose["foot" + side] ?? angle(knee, ankle) + 90;
      result["arm" + side] = [armRoot, elbow, hand];
      result["leg" + side] = [legRoot, knee, ankle, add(ankle, mul(dir(footAngle), L.foot))];
    }
    return result;
  }

  function lerp(x, y, t) {
    if (y === undefined) {
      return x;
    }
    if (typeof x === "number") {
      return x + (y - x) * t;
    }
    if (Array.isArray(x)) {
      return x.map((v, i) => lerp(v, y[i], t));
    }
    if (typeof x === "object") {
      const out = {};
      for (const k of Object.keys(x)) {
        out[k] = k === "bend" || k === "root" ? x[k] : lerp(x[k], y[k], t);
      }
      return out;
    }
    return x;
  }

  const ease = (t) => 0.5 - Math.cos(Math.PI * t) / 2;

  // Works out how long each transition takes, using the week's slow lowering time for lowering phases when there is one.
  function timeline(move, lowerSeconds) {
    return move.frames.map((f) => ({ hold: f.hold ?? 0, d: lowerSeconds > 0 && f.lower ? lowerSeconds : f.d }));
  }

  function poseAt(move, lowerSeconds, seconds) {
    const times = timeline(move, lowerSeconds);
    const cycle = times.reduce((s, x) => s + x.hold + x.d, 0);
    let t = seconds % cycle;
    for (let i = 0; i < times.length; i++) {
      if (t < times[i].hold) {
        return move.frames[i].p;
      }
      t -= times[i].hold;
      if (t < times[i].d) {
        return lerp(move.frames[i].p, move.frames[(i + 1) % times.length].p, ease(t / times[i].d));
      }
      t -= times[i].d;
    }
    return move.frames[0].p;
  }

  // The outline points of any props, used for drawing and for fitting the view.
  function propShapes(move) {
    const shapes = [];
    for (const prop of move.props ?? []) {
      if (prop.type === "chair") {
        const x = prop.x, w = 32, h = 44;
        shapes.push([[x, h], [x + w, h]], [[x + 2, h], [x + 2, 0]], [[x + w - 2, h], [x + w - 2, 0]], [[x + 2, h], [x + 2, h + 44]]);
      }
      if (prop.type === "wall") {
        shapes.push([[prop.x, 0], [prop.x, 150]]);
      }
    }
    return shapes;
  }

  // Finds a fixed view box that fits the whole move, so the camera never moves.
  function bounds(move) {
    const floor = move.view !== "top";
    let minX = Infinity, maxX = -Infinity, minY = floor ? 0 : Infinity, maxY = -Infinity;
    const grow = (p, r) => {
      minX = Math.min(minX, p[0] - r);
      maxX = Math.max(maxX, p[0] + r);
      minY = Math.min(minY, p[1] - r);
      maxY = Math.max(maxY, p[1] + r);
    };
    const times = timeline(move, 0);
    const cycle = times.reduce((s, x) => s + x.hold + x.d, 0);
    for (let s = 0; s < cycle; s += 0.05) {
      const j = solve(poseAt(move, 0, s), move);
      [j.hip, j.shoulder, ...j.armN, ...j.armF, ...j.legN, ...j.legF].forEach((p) => grow(p, 9));
      grow(j.head, L.headR);
    }
    propShapes(move).flat().forEach((p) => grow(p, 3));
    if (floor) {
      minY = 0;
    }
    const pad = 14;
    let w = maxX - minX + pad * 2, h = maxY - minY + pad * 2;
    const cx = (minX + maxX) / 2, cy = (minY + maxY) / 2;
    if (w / h < 4 / 3) {
      w = (h * 4) / 3;
    } else {
      h = (w * 3) / 4;
    }
    // Floor moves sit on the bottom edge; top-down moves are centred.
    const top = floor ? minY - pad + h : cy + h / 2;
    return { x: cx - w / 2, y: -top, w, h };
  }

  const NS = "http://www.w3.org/2000/svg";
  function el(name, attrs, parent) {
    const e = document.createElementNS(NS, name);
    for (const k in attrs) {
      e.setAttribute(k, attrs[k]);
    }
    parent.appendChild(e);
    return e;
  }

  // Builds the SVG for a move and returns an update function for each frame.
  function figure(container, move) {
    const b = bounds(move);
    const front = move.view === "front" || move.view === "top";
    const svg = el("svg", { viewBox: `${b.x} ${b.y} ${b.w} ${b.h}`, preserveAspectRatio: "xMidYMid meet" }, container);
    const g = el("g", { transform: "scale(1,-1)" }, svg);
    if (move.view !== "top") {
      el("line", { x1: b.x, y1: 0, x2: b.x + b.w, y2: 0, stroke: COLOURS.floor, "stroke-width": 1.5 }, g);
    }
    for (const [p, q] of propShapes(move)) {
      el("line", { x1: p[0], y1: p[1], x2: q[0], y2: q[1], stroke: COLOURS.prop, "stroke-width": 5, "stroke-linecap": "round" }, g);
    }
    const shadow = move.view === "top" ? null : el("ellipse", { cy: -1, ry: 3, fill: COLOURS.shadow }, g);
    const colour = (part, side) => {
      const hi = move.hi.includes(part);
      if (side === "N") {
        return hi ? COLOURS.hiNear : COLOURS.near;
      }
      return hi ? (front ? COLOURS.hiFarFront : COLOURS.hiFar) : front ? COLOURS.farFront : COLOURS.far;
    };
    const seg = (part, side) => el("line", { stroke: colour(part, side), "stroke-width": W[part], "stroke-linecap": "round" }, g);
    const parts = {};
    const torso = () => {
      parts.torso = el("line", { stroke: colour("torso", "N"), "stroke-width": front ? W.torsoFront : W.torso, "stroke-linecap": "round" }, g);
      parts.pelvis = el("circle", { r: front ? W.pelvis + 2 : W.pelvis, fill: colour("pelvis", "N") }, g);
      parts.neck = el("line", { stroke: COLOURS.near, "stroke-width": W.neck, "stroke-linecap": "round" }, g);
    };
    // Side-on: far limbs, body, near limbs. Front-on and top-down: body first, then all limbs.
    if (front) {
      torso();
    }
    parts.armF = [seg("upper", "F"), seg("fore", "F")];
    parts.legF = [seg("thigh", "F"), seg("shin", "F"), seg("foot", "F")];
    if (!front) {
      torso();
    }
    parts.head = el("circle", { r: L.headR, fill: COLOURS.near }, g);
    parts.legN = [seg("thigh", "N"), seg("shin", "N"), seg("foot", "N")];
    parts.armN = [seg("upper", "N"), seg("fore", "N")];
    const set = (line, p, q) => {
      line.setAttribute("x1", p[0]);
      line.setAttribute("y1", p[1]);
      line.setAttribute("x2", q[0]);
      line.setAttribute("y2", q[1]);
    };
    return (pose) => {
      const j = solve(pose, move);
      for (const key of ["armF", "legF", "legN", "armN"]) {
        parts[key].forEach((line, i) => set(line, j[key][i], j[key][i + 1]));
      }
      set(parts.torso, j.hip, j.shoulder);
      set(parts.neck, j.shoulder, j.neck);
      parts.pelvis.setAttribute("cx", j.hip[0]);
      parts.pelvis.setAttribute("cy", j.hip[1]);
      parts.head.setAttribute("cx", j.head[0]);
      parts.head.setAttribute("cy", j.head[1]);
      if (shadow) {
        const xs = [j.hip[0], j.shoulder[0], j.legN[3][0], j.legF[3][0], j.armN[2][0], j.armF[2][0]];
        shadow.setAttribute("cx", (Math.min(...xs) + Math.max(...xs)) / 2);
        shadow.setAttribute("rx", (Math.max(...xs) - Math.min(...xs)) / 2 + 10);
      }
    };
  }

  // x is to the right and y is up, with the floor at y = 0. Side-on figures face right.
  // Each frame holds for "hold" seconds, then takes "d" seconds to reach the next one. "lower" marks a lowering phase.

  const both = (spec) => ({ N: spec, F: spec });
  const pose = (base, extra) => ({ ...base, ...extra });
  const onHips = (hip) => ({ to: [hip[0] + 3, hip[1] + 6], bend: -1 });
  const UP = 400;

  const MOVES = {};
  function move(def) {
    MOVES[def.name] = { depth: [3, 1], view: "side", ...def };
  }

  // Daily stretching.
  (() => {
    const lizard = { at: [-12, 34], torso: 30, head: 36, armN: { to: [24, 3], bend: -1 }, armF: { to: [27, 3], bend: -1 }, legN: { to: [-88, 14], bend: 1 }, legF: { to: [32, 5], bend: 1 }, footN: -70, footF: 0 };
    const sink = pose(lizard, { at: [-15, 29], torso: 27 });
    move({ name: "Lizard Lunge", reps: "3 × 8s Each Side", tags: ["Hip Flexors", "Groin"], hi: ["thigh", "pelvis"], desc: "Low lunge with both hands inside the front foot and the back leg straight.", frames: [{ p: lizard, hold: 0.6, d: 1.6 }, { p: sink, hold: 2, d: 1.4 }] });
    const reach = pose(lizard, { torso: 42, head: 70, armN: { to: [30, UP], bend: -1 } });
    move({ name: "Lunge With Reach", reps: "3 × 8s Each Side", tags: ["Hip Flexors", "Upper Back"], hi: ["torso", "upper", "fore"], desc: "From the lizard lunge, rotate and reach the arm on the front-leg side to the sky.", frames: [{ p: lizard, hold: 0.6, d: 1.2 }, { p: reach, hold: 2.4, d: 1.2 }] });
    const split = { at: [-48, 48], torso: 8, head: 0, armN: { to: [20, 3], bend: -1 }, armF: { to: [24, 3], bend: -1 }, legN: { to: [-88, 10], bend: 1 }, legF: { to: [30, 8], bend: 1 }, footN: -170, footF: 80 };
    move({ name: "Half Splits", reps: "3 × 8s Each Side", tags: ["Hamstrings"], hi: ["thigh"], desc: "From the lunge, shift hips back, straighten the front leg and fold over it.", frames: [{ p: lizard, hold: 0.6, d: 1.4 }, { p: split, hold: 2.4, d: 1.4 }] });

    // Front leg tucked across in front of the hips, back leg extended straight behind.
    const upright = { at: [0, 14], torso: 100, head: 122, armN: { to: [4, 3], bend: -1 }, armF: { to: [6, 3], bend: -1 }, legN: { a: [188, 180] }, legF: { a: [-8, 190], s: [1, 0.45] }, footN: 200, footF: 200 };
    const forward = pose(upright, { torso: 25, head: 18, armN: { to: [60, 3], bend: -1 }, armF: { to: [56, 3], bend: -1 } });
    move({ name: "Pigeon Stretch", reps: "30s Each Side", tags: ["Hips", "Glutes", "Hip Flexors"], hi: ["pelvis", "thigh"], desc: "Front leg tucked in front of your hips and back leg extended straight behind, sit tall, then fold forward over the front leg.", frames: [{ p: upright, hold: 2.4, d: 1.6 }, { p: forward, hold: 2.4, d: 1.6 }] });

    const plank = { at: [-9.3, 27.7], torso: 10.3, head: 14, ...((s) => ({ armN: s, armF: s }))({ to: [72, 4], bend: -1 }), legN: { to: [-90, 13], bend: -1 }, legF: { to: [-90, 13], bend: -1 }, footN: -70, footF: -70 };
    move({ name: "Plank", reps: "60s", tags: ["Core"], hi: ["torso"], desc: "Forearms on the floor, body in a straight line from head to heels, and hold.", frames: [{ p: plank, hold: 0.4, d: 1.6 }, { p: pose(plank, { at: [-9.3, 26.2], torso: 9.4 }), hold: 0.4, d: 1.6 }] });

    const side = { at: [-12, 34], torso: 16, head: 20, armN: { a: [-90, 0], s: [1, 0.35] }, armF: { a: [90, 90] }, legN: { to: [-90, 6], bend: -1 }, legF: { to: [-92, 18], bend: -1 } };
    move({ name: "Side Plank", reps: "30s Each Side", tags: ["Obliques", "Core"], hi: ["torso"], view: "front", desc: "On one forearm with hips lifted so the body is in a straight line, and hold.", frames: [{ p: side, hold: 0.4, d: 1.6 }, { p: pose(side, { at: [-12, 32], torso: 15 }), hold: 0.4, d: 1.6 }] });

    // On hands and knees, with the extended arm and leg on opposite sides.
    const table = { at: [0, 48], torso: 16.8, head: 5, armN: { to: [50, 3], bend: -1 }, armF: { to: [50, 3], bend: -1 }, legN: { a: [-90, 180] }, legF: { a: [-90, 180] }, footN: 180, footF: 180 };
    const reachA = pose(table, { head: 8, armN: { to: [220, 80], bend: -1 }, legF: { a: [-185, 175] }, footF: 260 });
    const reachB = pose(table, { head: 8, armF: { to: [220, 80], bend: -1 }, legN: { a: [-185, 175] }, footN: 260 });
    move({ name: "Superman Hold", reps: "15s Each Side", tags: ["Lower Back", "Glutes", "Core"], hi: ["torso", "pelvis"], desc: "On hands and knees, extend one arm forward and the opposite leg back, and hold.", frames: [{ p: table, hold: 0.5, d: 1.0 }, { p: reachA, hold: 2.5, d: 1.0 }, { p: table, hold: 0.5, d: 1.0 }, { p: reachB, hold: 2.5, d: 1.0 }] });
    // The near hand holds the far ankle, so the far leg's depth offset is allowed for, and the reverse for the other side.
    const holdA = pose(table, { head: 10, armN: { to: [-5.6, 79], bend: -1 }, legF: { a: [-190, 35] }, footF: 120 });
    const holdB = pose(table, { head: 10, armF: { to: [-11.6, 77.2], bend: -1 }, legN: { a: [-190, 35] }, footN: 120 });
    move({ name: "Single-Arm Single-Leg Superman", reps: "15s Each Side", tags: ["Quads", "Lower Back", "Shoulders"], hi: ["torso", "thigh"], desc: "On hands and knees, extend one arm and the opposite leg, then bend the knee and hold the ankle behind you with the extended hand.", frames: [{ p: table, hold: 0.4, d: 0.9 }, { p: reachA, hold: 0.2, d: 1.1 }, { p: holdA, hold: 2.5, d: 1.1 }, { p: reachA, d: 0.9 }, { p: table, hold: 0.4, d: 0.9 }, { p: reachB, hold: 0.2, d: 1.1 }, { p: holdB, hold: 2.5, d: 1.1 }, { p: reachB, d: 0.9 }] });

    const straddle = { at: [0, 13], torso: 90, head: 90, armN: { to: [28, 14], bend: 1 }, armF: { to: [-28, 14], bend: -1 }, legN: { a: [-8, -8] }, legF: { a: [188, 188] }, footN: 90, footF: 90 };
    const toRight = pose(straddle, { torso: 40, head: 30, armN: { to: [88, 6], bend: 1 }, armF: { to: [80, 10], bend: 1 } });
    const toLeft = pose(straddle, { torso: 140, head: 150, armN: { to: [-80, 10], bend: -1 }, armF: { to: [-88, 6], bend: -1 } });
    move({ name: "Seated Straddle Toe Touch", reps: "20s Each Side", tags: ["Hamstrings", "Groin"], hi: ["thigh"], view: "front", desc: "Sit with legs apart at 90 degrees and reach towards the toes of one foot.", frames: [{ p: straddle, hold: 0.5, d: 1.4 }, { p: toRight, hold: 2, d: 1.4 }, { p: straddle, hold: 0.5, d: 1.4 }, { p: toLeft, hold: 2, d: 1.4 }] });

    const butterfly = { at: [0, 13], torso: 90, head: 90, armN: { to: [6, 12], bend: 1 }, armF: { to: [-6, 12], bend: -1 }, legN: { a: [8, 190] }, legF: { a: [172, -10] }, footN: 270, footF: 270 };
    const fold = pose(butterfly, { ts: 0.72, legN: { a: [2, 184] }, legF: { a: [178, -4] } });
    move({ name: "Butterfly Stretch", reps: "30s", tags: ["Groin", "Hips"], hi: ["thigh"], view: "front", desc: "Sit with the soles of both feet together, knees out, and lean forward.", frames: [{ p: butterfly, hold: 0.6, d: 1.6 }, { p: fold, hold: 2.4, d: 1.4 }] });

    const supine = { at: [0, 0], torso: 180, head: 180, armN: { a: [90, 90] }, armF: { a: [-90, -90] }, legN: { a: [90, 0], s: [0.25, 1] }, legF: { a: [90, 0], s: [0.25, 1] }, footN: 0, footF: 0 };
    const twistA = pose(supine, { head: 200, legN: { a: [80, -5], s: [1, 1] }, legF: { a: [80, -5], s: [1, 1] }, footN: -5, footF: -5 });
    const twistB = pose(supine, { head: 160, legN: { a: [-80, 5], s: [1, 1] }, legF: { a: [-80, 5], s: [1, 1] }, footN: 5, footF: 5 });
    move({ name: "Supine Spinal Twist", reps: "30s Each Side", tags: ["Spine", "Lower Back"], hi: ["torso"], view: "top", desc: "On your back with arms out, drop both knees to one side and look the other way.", frames: [{ p: supine, hold: 0.4, d: 1.4 }, { p: twistA, hold: 2.4, d: 1.4 }, { p: supine, hold: 0.4, d: 1.4 }, { p: twistB, hold: 2.4, d: 1.4 }] });
  })();

  // Strength A.
  (() => {
    const stand = { at: [0, 85], torso: 89, head: 89, armN: { a: [-88, -86] }, armF: { a: [-88, -86] }, legN: { to: [0, 5], bend: 1 }, legF: { to: [0, 5], bend: 1 }, footN: 0, footF: 0 };
    const bottom = { at: [-26, 44], torso: 52, head: 72, armN: { a: [4, 2] }, armF: { a: [4, 2] }, legN: { to: [0, 5], bend: 1 }, legF: { to: [0, 5], bend: 1 }, footN: 0, footF: 0 };
    move({ name: "Squats", reps: "15 Reps", tags: ["Quads", "Glutes"], hi: ["thigh", "pelvis"], depth: [4, 1], desc: "Feet shoulder-width apart, sit hips back and down with chest up, then stand.", frames: [{ p: stand, hold: 0.5, d: 1.2, lower: true }, { p: bottom, hold: 0.2, d: 1.0 }] });

    const top = { at: [-14.2, 44.2], torso: 22.4, head: 26, armN: { to: [37, 3], bend: -1 }, armF: { to: [37, 3], bend: -1 }, legN: { to: [-90, 13], bend: -1 }, legF: { to: [-90, 13], bend: -1 }, footN: -70, footF: -70 };
    const down = pose(top, { at: [-8.3, 19.7], torso: 4.7, head: 10 });
    move({ name: "Push-Ups", reps: "6 Reps", tags: ["Chest", "Triceps", "Shoulders"], hi: ["upper", "fore", "torso"], desc: "Hands under shoulders, body straight, lower chest to the floor and press back up.", frames: [{ p: top, hold: 0.3, d: 1.1, lower: true }, { p: down, hold: 0.15, d: 0.9 }] });

    const together = { at: [18, 85], torso: 89, head: 89, armN: onHips([18, 85]), armF: onHips([18, 85]), legN: { to: [15, 5], bend: 1 }, legF: { to: [22, 5], bend: 1 }, footN: 0, footF: 0 };
    const liftN = { at: [12, 80], torso: 88, head: 89, armN: onHips([12, 80]), armF: onHips([12, 80]), legN: { to: [-8, 22], bend: 1 }, legF: { to: [22, 5], bend: 1 }, footN: -45, footF: 0 };
    const downN = { at: [-8, 48], torso: 86, head: 88, armN: onHips([-8, 48]), armF: onHips([-8, 48]), legN: { to: [-50, 12], bend: 1 }, legF: { to: [22, 5], bend: 1 }, footN: -75, footF: 0 };
    const togetherF = pose(together, { legN: { to: [22, 5], bend: 1 }, legF: { to: [15, 5], bend: 1 } });
    const liftF = pose(liftN, { legN: { to: [22, 5], bend: 1 }, legF: { to: [-8, 22], bend: 1 }, footN: 0, footF: -45 });
    const downF = pose(downN, { legN: { to: [22, 5], bend: 1 }, legF: { to: [-50, 12], bend: 1 }, footN: 0, footF: -75 });
    move({ name: "Reverse Lunges", reps: "10 Reps Each Leg", tags: ["Quads", "Glutes"], hi: ["thigh", "pelvis"], desc: "Step one foot back, lower the back knee towards the floor, then drive back to standing.", frames: [{ p: together, hold: 0.3, d: 0.5 }, { p: liftN, d: 0.9, lower: true }, { p: downN, hold: 0.15, d: 0.6 }, { p: liftN, d: 0.4 }, { p: togetherF, hold: 0.3, d: 0.5 }, { p: liftF, d: 0.9, lower: true }, { p: downF, hold: 0.15, d: 0.6 }, { p: liftF, d: 0.4 }] });

    const table = { at: [0, 12], torso: 180, head: 180, armN: { a: [90, 90] }, armF: { a: [90, 90] }, legN: { a: [90, 0] }, legF: { a: [90, 0] } };
    const extA = pose(table, { armN: { a: [172, 172] }, legF: { a: [12, 8] } });
    const extB = pose(table, { armF: { a: [172, 172] }, legN: { a: [12, 8] } });
    move({ name: "Dead Bugs", reps: "10 Reps Each Side", tags: ["Core"], hi: ["torso"], depth: [3, 2], desc: "On your back with arms up and knees bent, lower the opposite arm and leg slowly, keeping your back flat.", frames: [{ p: table, hold: 0.3, d: 1.4, lower: true }, { p: extA, hold: 0.3, d: 1.0 }, { p: table, hold: 0.3, d: 1.4, lower: true }, { p: extB, hold: 0.3, d: 1.0 }] });

    const bridgeDown = { root: "shoulder", at: [-40, 12], torso: 180, head: 180, armN: { a: [-6, -4] }, armF: { a: [-6, -4] }, legN: { to: [44, 5], bend: 1 }, legF: { to: [44, 5], bend: 1 }, footN: 0, footF: 0 };
    move({ name: "Glute Bridges", reps: "15 Reps", tags: ["Glutes", "Hamstrings"], hi: ["pelvis", "thigh"], depth: [3, 2], desc: "On your back with knees bent, drive hips up, squeeze glutes, then lower.", frames: [{ p: bridgeDown, hold: 0.3, d: 1.0 }, { p: pose(bridgeDown, { torso: 210 }), hold: 0.6, d: 1.2, lower: true }] });
  })();

  // Strength B.
  (() => {
    const stand = { at: [0, 82], torso: 88, head: 88, armN: onHips([0, 82]), armF: onHips([0, 82]), legN: { to: [-58, 48], bend: 1 }, legF: { to: [22, 5], bend: 1 }, footN: 190, footF: 0 };
    const bottom = pose(stand, { at: [-8, 48], torso: 84, head: 86, armN: onHips([-8, 48]), armF: onHips([-8, 48]) });
    move({ name: "Split Squats", reps: "10 Reps Each Leg", tags: ["Quads", "Glutes"], hi: ["thigh", "pelvis"], props: [{ type: "chair", x: -80 }], desc: "Back foot on a chair, lower the back knee towards the floor, then stand.", frames: [{ p: stand, hold: 0.4, d: 1.2, lower: true }, { p: bottom, hold: 0.2, d: 1.0 }] });

    const dipTop = { root: "shoulder", at: [-25, 106], torso: 92, head: 90, armN: { to: [-30, 46], bend: -1 }, armF: { to: [-30, 46], bend: -1 }, legN: { to: [26, 5], bend: 1 }, legF: { to: [26, 5], bend: 1 }, footN: 0, footF: 0 };
    const dipBottom = pose(dipTop, { at: [-22, 80], torso: 96 });
    move({ name: "Chair Dips", reps: "6 Reps", tags: ["Triceps", "Shoulders"], hi: ["upper", "fore"], props: [{ type: "chair", x: -60 }], desc: "Hands on a chair behind you, bend elbows to lower hips, then press back up.", frames: [{ p: dipTop, hold: 0.3, d: 1.1, lower: true }, { p: dipBottom, hold: 0.15, d: 0.9 }] });

    const sit = { at: [0, 44], torso: 90, head: 90, armN: { to: [30, 50], bend: -1 }, armF: { to: [30, 50], bend: -1 }, legN: { to: [42, 5], bend: 1 }, legF: { to: [42, 5], bend: 1 }, footN: 0, footF: 0 };
    move({ name: "Wall Sit", reps: "40s", tags: ["Quads"], hi: ["thigh"], props: [{ type: "wall", x: -15 }], desc: "Back against a wall, thighs parallel to the floor, and hold.", frames: [{ p: sit, hold: 0.4, d: 1.6 }, { p: pose(sit, { at: [0, 43] }), hold: 0.4, d: 1.6 }] });

    const slDown = { root: "shoulder", at: [-40, 12], torso: 180, head: 180, armN: { a: [-6, -4] }, armF: { a: [-6, -4] }, legN: { a: [45, 45] }, legF: { to: [44, 5], bend: 1 }, footN: 135, footF: 0 };
    const slUp = pose(slDown, { torso: 210, legN: { a: [30, 30] }, footN: 120 });
    move({ name: "Single-Leg Glute Bridges", reps: "10 Reps Each Leg", tags: ["Glutes", "Hamstrings"], hi: ["pelvis", "thigh"], depth: [3, 2], desc: "Glute bridge with one leg extended, driving up through the planted foot.", frames: [{ p: slDown, hold: 0.3, d: 1.0 }, { p: slUp, hold: 0.6, d: 1.2, lower: true }] });

    const pikeTop = { at: [0, 92], torso: -35, head: -75, armN: { to: [45, 3], bend: -1 }, armF: { to: [45, 3], bend: -1 }, legN: { to: [-24, 14], bend: -1 }, legF: { to: [-24, 14], bend: -1 }, footN: -70, footF: -70 };
    const pikeBottom = pose(pikeTop, { torso: -68, head: -88 });
    move({ name: "Pike Push-Ups", reps: "5 Reps", tags: ["Shoulders", "Triceps"], hi: ["upper", "fore"], desc: "Hips high in an inverted V, bend elbows to lower your head towards the floor, then press up.", frames: [{ p: pikeTop, hold: 0.3, d: 1.1, lower: true }, { p: pikeBottom, hold: 0.15, d: 0.9 }] });
  })();

  // Cardio circuit.
  (() => {
    const closed = { at: [0, 86], torso: 90, head: 90, armN: { a: [-82, -86] }, armF: { a: [-98, -94] }, legN: { to: [5, 5], bend: 1 }, legF: { to: [-5, 5], bend: -1 } };
    // The left arm's angles go negative so it swings out to the left rather than across the body.
    const open = { at: [0, 82], torso: 90, head: 90, armN: { a: [70, 82] }, armF: { a: [-250, -262] }, legN: { to: [34, 5], bend: 1 }, legF: { to: [-34, 5], bend: -1 } };
    move({ name: "Jumping Jacks", reps: "40s", tags: ["Full Body"], hi: ["thigh", "shin", "upper"], view: "front", desc: "Jump feet wide while raising arms overhead, then jump back together.", frames: [{ p: closed, hold: 0.05, d: 0.3 }, { p: open, hold: 0.05, d: 0.3 }] });

    const plank = { at: [-14.2, 44.2], torso: 22.4, head: 26, armN: { to: [37, 3], bend: -1 }, armF: { to: [37, 3], bend: -1 }, footN: -70, footF: -70 };
    const climbA = pose(plank, { legN: { to: [2, 16], bend: 1 }, legF: { to: [-90, 13], bend: 1 }, footN: -40 });
    const climbB = pose(plank, { legN: { to: [-90, 13], bend: 1 }, legF: { to: [2, 16], bend: 1 }, footF: -40 });
    move({ name: "Mountain Climbers", reps: "40s", tags: ["Core", "Cardio"], hi: ["torso", "thigh"], desc: "In a high plank, drive knees to the chest alternately and quickly.", frames: [{ p: climbA, hold: 0.05, d: 0.3 }, { p: climbB, hold: 0.05, d: 0.3 }] });

    const squat = { at: [-26, 44], torso: 50, head: 70, armN: { a: [-120, -110] }, armF: { a: [-120, -110] }, legN: { to: [0, 5], bend: 1 }, legF: { to: [0, 5], bend: 1 }, footN: 0, footF: 0 };
    const air = { at: [0, 128], torso: 88, head: 88, armN: { a: [75, 80] }, armF: { a: [75, 80] }, legN: { to: [0, 46], bend: 1 }, legF: { to: [0, 46], bend: 1 }, footN: -60, footF: -60 };
    const land = { at: [-4, 82], torso: 82, head: 85, armN: { a: [-60, -50] }, armF: { a: [-60, -50] }, legN: { to: [0, 5], bend: 1 }, legF: { to: [0, 5], bend: 1 }, footN: 0, footF: 0 };
    move({ name: "Squat Jumps", reps: "40s", tags: ["Quads", "Glutes", "Calves"], hi: ["thigh", "pelvis", "shin"], desc: "Squat down, then jump up and land softly.", frames: [{ p: squat, hold: 0.15, d: 0.35 }, { p: air, hold: 0.1, d: 0.35 }, { p: land, d: 0.35 }] });

    const kneeA = { at: [0, 87], torso: 91, head: 90, armN: { a: [-130, -60] }, armF: { a: [-40, 40] }, legN: { to: [42, 47], bend: 1 }, legF: { to: [2, 5], bend: 1 }, footN: -10, footF: 0 };
    const kneeB = { at: [0, 87], torso: 91, head: 90, armN: { a: [-40, 40] }, armF: { a: [-130, -60] }, legN: { to: [2, 5], bend: 1 }, legF: { to: [42, 47], bend: 1 }, footN: 0, footF: -10 };
    move({ name: "High Knees", reps: "40s", tags: ["Hip Flexors", "Cardio"], hi: ["thigh"], desc: "Run on the spot, driving knees up to hip height.", frames: [{ p: kneeA, hold: 0.05, d: 0.28 }, { p: kneeB, hold: 0.05, d: 0.28 }] });

    const bStand = { at: [0, 85], torso: 89, head: 89, armN: { to: [6, 78], bend: -1 }, armF: { to: [6, 78], bend: -1 }, legN: { to: [0, 5], bend: 1 }, legF: { to: [0, 5], bend: 1 }, footN: 0, footF: 0 };
    const bSquat = { at: [-14, 40], torso: 40, head: 50, armN: { to: [32, 3], bend: -1 }, armF: { to: [32, 3], bend: -1 }, legN: { to: [0, 5], bend: 1 }, legF: { to: [0, 5], bend: 1 }, footN: 0, footF: 0 };
    const bPlank = { at: [-17, 44], torso: 22.4, head: 26, armN: { to: [34, 3], bend: -1 }, armF: { to: [34, 3], bend: -1 }, legN: { to: [-93, 13], bend: 1 }, legF: { to: [-93, 13], bend: 1 }, footN: -70, footF: -70 };
    const bJump = { at: [0, 98], torso: 88, head: 88, armN: { to: [10, UP], bend: -1 }, armF: { to: [10, UP], bend: -1 }, legN: { to: [0, 18], bend: 1 }, legF: { to: [0, 18], bend: 1 }, footN: -40, footF: -40 };
    move({ name: "Burpees", reps: "40s", tags: ["Full Body"], hi: ["thigh", "upper", "torso"], desc: "Squat, kick feet back to a plank, jump feet in, then jump up.", frames: [{ p: bStand, hold: 0.2, d: 0.45 }, { p: bSquat, d: 0.3 }, { p: bPlank, hold: 0.1, d: 0.3 }, { p: bSquat, d: 0.35 }, { p: bJump, d: 0.35 }] });
  })();

  // Everything currently animating, driven by one animation frame loop.
  const players = [];
  let running = false;

  function loop(now) {
    for (const p of players) {
      p.update(poseAt(p.move, p.lowerSeconds, (now - p.start) / 1000));
    }
    running = players.length > 0;
    if (running) {
      requestAnimationFrame(loop);
    }
  }

  window.MoveAnimations = {
    // Whether there is an animation for a move name from the plan.
    has: (name) => name in MOVES,

    // Draws a move into a container and loops it until stopped, or returns null if there is no animation for it.
    play(container, name, lowerSeconds = 0) {
      container.innerHTML = "";
      const m = MOVES[name];
      if (!m) {
        return null;
      }
      const player = { update: figure(container, m), move: m, lowerSeconds, start: performance.now() };
      players.push(player);
      if (!running) {
        running = true;
        requestAnimationFrame(loop);
      }
      return {
        stop() {
          const index = players.indexOf(player);
          if (index >= 0) {
            players.splice(index, 1);
          }
        },
      };
    },
  };
})();
