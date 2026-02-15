const bgMusic = document.getElementById("bgMusic");
const clickSound = document.getElementById("clickSound");
const soundToggleBtn = document.getElementById("soundToggle");
// Load saved setting (default: true)
let soundEnabled = localStorage.getItem("soundEnabled") !== "false";

bgMusic.volume = 0.25;
clickSound.volume = 0.4;

let musicStarted = false;

function startMusicOnce() {
    if (musicStarted) return;
    musicStarted = true;
    bgMusic.play().catch(() => { });
}

document.addEventListener("click", startMusicOnce, { once: true });

document.addEventListener("click", e => {
    if (e.target.closest("button")) {
        clickSound.currentTime = 0;
        clickSound.play().catch(() => { });
    }
});
bgMusic.addEventListener("play", () => {
    document.body.classList.add("music-on");
});

const doodles = ["✏️", "⭐", "🖍️", "📐", "📏", "✨", "🎨", "➰"];

const doodleLayer = document.getElementById("doodleLayer");

function spawnDoodle() {
    const d = document.createElement("div");
    d.className = "doodle";
    d.innerText = doodles[Math.floor(Math.random() * doodles.length)];

    const x = Math.random() * window.innerWidth;
    const z = Math.random() * -300;   // depth
    const duration = 20 + Math.random() * 20;

    // depth-based scale
    const scale = 1 + Math.abs(z) / 300;

    d.style.setProperty("--x", `${x}px`);
    d.style.setProperty("--z", `${z}px`);
    d.style.left = `${x}px`;
    d.style.animationDuration = `${duration}s`;
    d.style.transform = `scale(${scale})`;

    doodleLayer.appendChild(d);
    setTimeout(() => d.remove(), duration * 1000);
}


// spawn slowly (music-friendly)
setInterval(spawnDoodle, 2500);

applySoundState();

soundToggleBtn.addEventListener("click", () => {
    soundEnabled = !soundEnabled;
    localStorage.setItem("soundEnabled", soundEnabled);
    applySoundState();
});

function applySoundState() {
    bgMusic.muted = !soundEnabled;
    clickSound.muted = !soundEnabled;
    soundToggleBtn.innerText = soundEnabled ? "🔊" : "🔇";
}

function dimMusic() {
    bgMusic.volume = 0.1;
}

function restoreMusic() {
    bgMusic.volume = 0.25;
}
