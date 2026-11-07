const stepsElement = document.getElementById("steps");

if (stepsElement) {
  const steps = JSON.parse(stepsElement.textContent);
  const get = (id) => document.getElementById(id);
  let index = -1;
  let endTime = 0;
  let remaining = 0;
  let paused = false;
  let timer = null;
  let lastBeep = 0;
  let audio = null;
  let wakeLock = null;
  let running = false;

  function show(id, visible) {
    get(id).classList.toggle("d-none", !visible);
  }

  function beep(frequency, milliseconds) {
    if (!audio) {
      return;
    }
    const oscillator = audio.createOscillator();
    const gain = audio.createGain();
    gain.gain.value = 0.2;
    oscillator.frequency.value = frequency;
    oscillator.connect(gain);
    gain.connect(audio.destination);
    oscillator.start();
    oscillator.stop(audio.currentTime + milliseconds / 1000);
  }

  function speak(text) {
    if (!("speechSynthesis" in window)) {
      return;
    }
    speechSynthesis.cancel();
    speechSynthesis.speak(new SpeechSynthesisUtterance(text));
  }

  async function requestWakeLock() {
    try {
      wakeLock = await navigator.wakeLock.request("screen");
    } catch {
      wakeLock = null;
    }
  }

  function format(seconds) {
    const minutes = Math.floor(seconds / 60);
    return minutes > 0 ? `${minutes}:${String(seconds % 60).padStart(2, "0")}` : String(seconds);
  }

  function spokenDetail(detail) {
    return detail.replace(/(\d+)s\b/g, "$1 seconds");
  }

  function finish() {
    clearInterval(timer);
    running = false;
    show("run", false);
    show("complete", true);
    speak("Complete");
    if (wakeLock) {
      wakeLock.release();
    }
  }

  function goTo(next) {
    clearInterval(timer);
    index = next;
    if (index >= steps.length) {
      finish();
      return;
    }
    const step = steps[index];
    paused = false;
    get("pause-button").textContent = "Pause";
    get("progress").style.width = `${(index / steps.length) * 100}%`;
    get("round").textContent = step.round;
    get("title").textContent = step.title;
    get("title").className = step.kind === "work" ? "display-4" : "display-4 text-info";
    get("detail").textContent = step.kind === "ready" ? "" : step.detail;
    get("description").textContent = step.description;
    get("next").textContent = step.next ? `Next: ${step.next}` : "";
    const timed = step.seconds > 0;
    show("done-button", !timed);
    show("pause-button", timed);
    get("step-countdown").textContent = timed ? format(step.seconds) : "";
    if (step.kind === "work") {
      speak(timed ? step.title : `${step.title}. ${spokenDetail(step.detail)}`);
    } else if (step.kind === "rest") {
      speak("Rest");
    }
    if (timed) {
      remaining = step.seconds * 1000;
      endTime = Date.now() + remaining;
      lastBeep = 0;
      timer = setInterval(tick, 100);
    }
  }

  function tick() {
    if (paused) {
      return;
    }
    const left = Math.max(0, endTime - Date.now());
    const seconds = Math.ceil(left / 1000);
    get("step-countdown").textContent = format(seconds);
    if (seconds === 1 && seconds !== lastBeep) {
      lastBeep = seconds;
      beep(880, 120);
    }
    if (left === 0) {
      beep(1320, 400);
      goTo(index + 1);
    }
  }

  get("start-button").addEventListener("click", () => {
    audio = new AudioContext();
    running = true;
    requestWakeLock();
    show("start", false);
    show("run", true);
    goTo(0);
  });

  get("done-button").addEventListener("click", () => goTo(index + 1));
  get("skip-button").addEventListener("click", () => goTo(index + 1));

  get("pause-button").addEventListener("click", () => {
    if (paused) {
      endTime = Date.now() + remaining;
      paused = false;
    } else {
      remaining = endTime - Date.now();
      paused = true;
    }
    get("pause-button").textContent = paused ? "Resume" : "Pause";
  });

  document.addEventListener("visibilitychange", () => {
    if (running && document.visibilityState === "visible") {
      requestWakeLock();
    }
  });
}
