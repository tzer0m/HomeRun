function pad(value) {
  return String(value).padStart(2, "0");
}

const countdown = document.getElementById("countdown");
if (countdown && countdown.dataset.target) {
  const target = Number(countdown.dataset.target);
  const timer = setInterval(() => {
    const left = Math.floor((target - Date.now()) / 1000);
    if (left <= 0) {
      clearInterval(timer);
      setTimeout(() => location.reload(), 2000);
      return;
    }
    countdown.textContent = `${pad(Math.floor(left / 3600))}:${pad(Math.floor((left % 3600) / 60))}:${pad(left % 60)}`;
  }, 1000);
}
