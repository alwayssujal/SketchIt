const canvas = document.getElementById("drawCanvas");
const ctx = canvas.getContext("2d");

function resizeCanvas() {
    const parent = canvas.parentElement;
    canvas.width = parent.clientWidth;
    canvas.height = parent.clientHeight;
    console.log("parent height", parent.clientHeight);
}

window.addEventListener("resize", resizeCanvas);
resizeCanvas();

let drawing = false;
let strokes = [];
let currentStroke = [];

function getPos(e) {
    const rect = canvas.getBoundingClientRect();
    const touch = e.touches ? e.touches[0] : e;

    return {
        x: touch.clientX - rect.left,
        y: touch.clientY - rect.top
    };
}

function startDraw(e) {
    drawing = true;
    currentStroke = [];

    const pos = getPos(e);
    ctx.beginPath();
    ctx.moveTo(pos.x, pos.y);

    currentStroke.push(pos);
}
function draw(e) {
    if (!drawing) return;
    e.preventDefault();

    const pos = getPos(e);
    ctx.lineTo(pos.x, pos.y);
    ctx.lineWidth = 4;
    ctx.lineCap = "round";
    ctx.stroke();

    currentStroke.push(pos);
}


function stopDraw() {
    if (!drawing) return;
    drawing = false;

    if (currentStroke.length > 0) {
        strokes.push(currentStroke);
    }
}
function redrawCanvas() {
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.beginPath();

    strokes.forEach(stroke => {
        ctx.moveTo(stroke[0].x, stroke[0].y);
        stroke.forEach(point => {
            ctx.lineTo(point.x, point.y);
        });
        ctx.stroke();
    });
}
const undoBtn = document.getElementById("undoCanvasBtn");

undoBtn.addEventListener("click", () => {
    strokes.pop();
    redrawCanvas();
});

const clearBtn = document.getElementById("resetCanvasBtn");
clearBtn.addEventListener("click", () => {
    strokes = [];
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.beginPath();
});

canvas.addEventListener("mousedown", startDraw);
canvas.addEventListener("mousemove", draw);
canvas.addEventListener("mouseup", stopDraw);
canvas.addEventListener("mouseleave", stopDraw);

canvas.addEventListener("touchstart", startDraw);
canvas.addEventListener("touchmove", draw);
canvas.addEventListener("touchend", stopDraw);
