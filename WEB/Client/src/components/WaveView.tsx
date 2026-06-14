import { useCallback, useEffect, useRef, useState, type MutableRefObject } from "react";
import type { ViewControllerState, WaveAsset } from "../types";
import { clamp } from "../viewController";
import { getRulerTicks } from "../ruler";

type WaveViewProps = {
  wave: WaveAsset;
  controller: ViewControllerState;
  onControllerChange: (controller: ViewControllerState) => void;
  onViewportWidthChange?: (width: number) => void;
};

const titleHeight = 30;
const marginSize = 2;
const rulerHeight = 40;

export function WaveView({ wave, controller, onControllerChange, onViewportWidthChange }: WaveViewProps) {
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const cacheRef = useRef<HTMLCanvasElement | null>(null);
  const controllerRef = useRef(controller);
  const dragRef = useRef<{ x: number; panning: boolean } | null>(null);
  const [size, setSize] = useState({ width: 1, height: 150 });

  useEffect(() => {
    controllerRef.current = controller;
  }, [controller]);

  const handleWheel = useCallback((event: WheelEvent) => {
    event.preventDefault();
    event.stopPropagation();

    const current = controllerRef.current;
    const oldSamplesPerPixel = current.samplesPerPixel;
    const zoomFactor = Math.pow(1.0015, event.deltaY * (event.ctrlKey ? 2.5 : 1));
    const newSamplesPerPixel = clamp(
      oldSamplesPerPixel * zoomFactor,
      current.minSamplesPerPixel,
      current.maxSamplesPerPixel
    );

    const canvas = canvasRef.current;
    if (!canvas) {
      return;
    }

    const rect = canvas.getBoundingClientRect();
    const x = event.clientX - rect.left;
    const initialSampleUnderCursor = current.panStartSample + x * oldSamplesPerPixel - oldSamplesPerPixel / 2;
    const newSampleUnderCursor = x * newSamplesPerPixel - newSamplesPerPixel / 2;
    const panStartSample = clamp(initialSampleUnderCursor - newSampleUnderCursor, 0, current.length);

    onControllerChange({
      ...current,
      samplesPerPixel: newSamplesPerPixel,
      panStartSample
    });
  }, [onControllerChange]);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) {
      return;
    }

    const resizeObserver = new ResizeObserver((entries) => {
      const rect = entries[0].contentRect;
      const width = Math.max(1, Math.floor(rect.width));
      setSize({
        width,
        height: Math.max(80, Math.floor(rect.height))
      });
      onViewportWidthChange?.(width);
    });

    resizeObserver.observe(canvas);
    canvas.addEventListener("wheel", handleWheel, { passive: false });

    return () => {
      resizeObserver.disconnect();
      canvas.removeEventListener("wheel", handleWheel);
    };
  }, [handleWheel, onViewportWidthChange]);

  useEffect(() => {
    renderWave(canvasRef.current, cacheRef, wave, controller, size.width, size.height);
  }, [wave, controller, size]);

  return (
    <section className="controlled-view" aria-label={wave.title}>
      <div className="view-title">{wave.title}</div>
      <canvas
        ref={canvasRef}
        className="wave-canvas"
        onPointerDown={(event) => {
          dragRef.current = { x: event.clientX, panning: true };
          event.currentTarget.setPointerCapture(event.pointerId);
        }}
        onPointerMove={(event) => {
          if (!dragRef.current?.panning) {
            return;
          }

          const current = controllerRef.current;
          const dx = event.clientX - dragRef.current.x;
          const sampleDelta = -dx * current.samplesPerPixel;
          dragRef.current.x = event.clientX;

          onControllerChange({
            ...current,
            panStartSample: clamp(current.panStartSample + sampleDelta, 0, current.length)
          });
        }}
        onPointerUp={(event) => {
          dragRef.current = null;
          event.currentTarget.releasePointerCapture(event.pointerId);
        }}
        onPointerCancel={() => {
          dragRef.current = null;
        }}
      />
    </section>
  );
}

function renderWave(
  canvas: HTMLCanvasElement | null,
  cacheRef: MutableRefObject<HTMLCanvasElement | null>,
  wave: WaveAsset,
  controller: ViewControllerState,
  cssWidth: number,
  cssHeight: number
) {
  if (!canvas) {
    return;
  }

  const dpr = window.devicePixelRatio || 1;
  canvas.width = Math.floor(cssWidth * dpr);
  canvas.height = Math.floor(cssHeight * dpr);
  canvas.style.height = `${cssHeight}px`;

  const cache = cacheRef.current ?? document.createElement("canvas");
  cacheRef.current = cache;
  cache.width = canvas.width;
  cache.height = canvas.height;

  const ctx = cache.getContext("2d");
  const visible = canvas.getContext("2d");
  if (!ctx || !visible) {
    return;
  }

  ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
  visible.setTransform(1, 0, 0, 1, 0, 0);

  const rulerH = wave.includeRuler ? rulerHeight : 0;
  const waveH = cssHeight - rulerH - marginSize * 2 - titleHeight;
  const waveHalfH = Math.max(1, waveH / 2);
  const bottomY = cssHeight - marginSize - rulerH;
  const centerY = bottomY - waveHalfH;

  ctx.clearRect(0, 0, cssWidth, cssHeight);
  ctx.fillStyle = "#f7ead5";
  ctx.fillRect(0, 0, cssWidth, cssHeight);
  ctx.strokeStyle = "#111827";
  ctx.lineWidth = 1;
  ctx.beginPath();
  ctx.moveTo(0, centerY);
  ctx.lineTo(cssWidth, centerY);
  ctx.stroke();

  drawSamples(ctx, wave, controller, cssWidth, centerY, waveHalfH);

  if (wave.includeRuler) {
    drawRuler(ctx, controller, wave.sampleRate, cssWidth, cssHeight, rulerH);
  }

  visible.clearRect(0, 0, canvas.width, canvas.height);
  visible.drawImage(cache, 0, 0);
}


function drawSamples(
  ctx: CanvasRenderingContext2D,
  wave: WaveAsset,
  controller: ViewControllerState,
  width: number,
  centerY: number,
  waveHalfH: number
)
{
  if ( wave.colorCoded )
       drawSamples_ColorCoded(ctx, wave, controller, width, centerY, waveHalfH);
  else drawSamplesO           (ctx, wave, controller, width, centerY, waveHalfH);
}

function drawSamples_Normal(
  ctx: CanvasRenderingContext2D,
  wave: WaveAsset,
  controller: ViewControllerState,
  width: number,
  centerY: number,
  waveHalfH: number
) {
  const { samples } = wave;
  const samplesPerPixel = controller.samplesPerPixel;
  const startSample = Math.floor(controller.panStartSample);
  const visibleSamples = Math.ceil(width * samplesPerPixel);
  const endSample = Math.min(Math.min(samples.length, controller.length) - 1, startSample + visibleSamples);

  // Pass 1: reduce each pixel column to its (min, max) -> (hy = top, ly = bottom).
  const cols: { px: number; ly: number; hy: number }[] = [];

  for (let px = 0; px < width; px++) {
    const sampleStart = startSample + Math.floor(px * samplesPerPixel);
    if (sampleStart > endSample || sampleStart >= samples.length) {
      break;
    }

    let sampleEnd = startSample + Math.ceil((px + 1) * samplesPerPixel) - 1;
    sampleEnd = Math.min(sampleEnd, endSample);
    if (sampleEnd < sampleStart) {
      sampleEnd = sampleStart;
    }

    let min = samples[sampleStart];
    let max = samples[sampleStart];
    for (let i = sampleStart + 1; i <= sampleEnd; i++) {
      const v = samples[i];
      if (v < min) min = v;
      if (v > max) max = v;
    }

    cols.push({
      px,
      ly: centerY - Math.ceil(min * waveHalfH),
      hy: centerY - Math.ceil(max * waveHalfH)
    });
  }

  if (cols.length === 0) {
    return;
  }

  // Pass 2: top envelope left->right, then back along the centerline. No center dives.
  const path = new Path2D();
  path.moveTo(cols[0].px, centerY);
  for (let i = 0; i < cols.length; i++) {
    path.lineTo(cols[i].px, cols[i].hy);
  }
  path.lineTo(cols[cols.length - 1].px, centerY);
  path.closePath();

  drawPath(ctx, path, "#2563eb", false, true);
}

function drawSamples_ColorCoded(
  ctx: CanvasRenderingContext2D,
  wave: WaveAsset,
  controller: ViewControllerState,
  width: number,
  centerY: number,
  waveHalfH: number
) {
  const { samples } = wave;
  const samplesPerPixel = controller.samplesPerPixel;
  const startSample = Math.floor(controller.panStartSample);
  const visibleSamples = Math.ceil(width * samplesPerPixel);
  const endSample = Math.min(Math.min(samples.length, controller.length) - 1, startSample + visibleSamples);

  // Levels: blue/red are DC runs at +/-0.6; black is a +/-0.9 OSCILLATION.
  // Black is identified by amplitude (only it reaches 0.9 on either rail).
  const LV_BLACK = 0.9;
  const LV_BLUE  = 0.6;
  const LV_RED   = -0.6;

  const BLACK_MIN = 0.75;   // |extreme| above this -> part of the +/-0.9 oscillation
  const POS_MIN   = 0.3;    // max above this -> a +0.6 run is present
  const NEG_MIN   = -0.3;   // min below this -> a -0.6 run is present

  const cy  = Math.round(centerY);
  const yOf = (v: number) => Math.round(centerY - v * waveHalfH);

  const blue  = new Path2D();
  const red   = new Path2D();
  const black = new Path2D();
  const gray  = new Path2D();

  for (let px = 0; px < width; px++) {
    const sampleStart = startSample + Math.floor(px * samplesPerPixel);
    if (sampleStart > endSample || sampleStart >= samples.length) {
      break;
    }

    let sampleEnd = startSample + Math.ceil((px + 1) * samplesPerPixel) - 1;
    sampleEnd = Math.min(sampleEnd, endSample);
    if (sampleEnd < sampleStart) {
      sampleEnd = sampleStart;
    }

    let min = Infinity;
    let max = -Infinity;
    for (let i = sampleStart; i <= sampleEnd; i++) {
      const v = samples[i];
      if (v < min) min = v;
      if (v > max) max = v;
    }

    const reachesBlack = max > BLACK_MIN || min < -BLACK_MIN;   // the +/-0.9 oscillation
    const hasBlue = max > POS_MIN;
    const hasRed  = min < NEG_MIN;

    if (reachesBlack) {
      // Full symmetric block, regardless of which rail(s) this pixel caught.
      const top = yOf(LV_BLACK);
      black.rect(px, top, 1, yOf(-LV_BLACK) - top);
    } else if (hasBlue && hasRed) {
      // Genuine blue + red mix -> gray, centre out to both.
      const top    = yOf(LV_BLUE);
      const bottom = yOf(LV_RED);
      gray.rect(px, top, 1, bottom - top);
    } else if (hasBlue) {
      const top = yOf(LV_BLUE);     // +0.6 -> centre up
      blue.rect(px, top, 1, cy - top);
    } else if (hasRed) {
      const bottom = yOf(LV_RED);   // -0.6 -> centre down
      red.rect(px, cy, 1, bottom - cy);
    }
    // else: silence, nothing to draw
  }

  fillPath(ctx, gray,  "#6b7280");
  fillPath(ctx, blue,  "#2563eb");
  fillPath(ctx, red,   "#dc2626");
  fillPath(ctx, black, "#111827");
}

function fillPath(ctx: CanvasRenderingContext2D, path: Path2D, color: string) {
  ctx.fillStyle = color;
  ctx.fill(path);
}


function drawSamplesO(
  ctx: CanvasRenderingContext2D,
  wave: WaveAsset,
  controller: ViewControllerState,
  width: number,
  centerY: number,
  waveHalfH: number
) {
  const { samples } = wave;
  const samplesPerPixel = controller.samplesPerPixel;
  const startSample = Math.floor(controller.panStartSample);
  const visibleSamples = Math.ceil(width * samplesPerPixel);
  const endSample = Math.min(Math.min(samples.length, controller.length) - 1, startSample + visibleSamples);

  let path = new Path2D();
  
  let started = false ;

  for (let px = 0; px < width; px++) {
    const sampleStart = startSample + Math.floor(px * samplesPerPixel);
    if (sampleStart > endSample || sampleStart >= samples.length) {
      break;
    }

    let sampleEnd = startSample + Math.ceil((px + 1) * samplesPerPixel) - 1;
    sampleEnd = Math.min(sampleEnd, endSample);
    if (sampleEnd < sampleStart) {
      sampleEnd = sampleStart;
    }

    let min = samples[sampleStart];
    let max = samples[sampleStart];
    for (let sampleIndex = sampleStart + 1; sampleIndex <= sampleEnd; sampleIndex++) {
      const value = samples[sampleIndex];
      if (value < min) {
        min = value;
      }
      if (value > max) {
        max = value;
      }
    }

    let ly = centerY - Math.ceil(min * waveHalfH);
    let hy = centerY - Math.ceil(max * waveHalfH);
    
    started = addPolylinePoint(path, started, px, ly);
    started = addPolylinePoint(path, started, px, hy);

  }

  drawPath(ctx, path, "#2563eb" , false, started);
}

function addPolylinePoint(path: Path2D, started: boolean, x: number, y: number): boolean {
  if (started) {
    path.lineTo(x, y);
  } else {
    path.moveTo(x, y);
  }

  return true;
}

function drawPath(ctx: CanvasRenderingContext2D, path: Path2D, color: string, fill: boolean, used: boolean) {
  if (!used) {
    return;
  }

  ctx.strokeStyle = color;
  ctx.lineWidth = 0;
  ctx.lineCap = "butt";
  ctx.lineJoin = "bevel";
  ctx.stroke(path);

  if ( fill )
  {
     ctx.fillStyle = color;
     //ctx.fill(path);

  }
}

function drawRuler(
  ctx: CanvasRenderingContext2D,
  controller: ViewControllerState,
  sampleRate: number,
  width: number,
  height: number,
  rulerH: number
) {
  const rulerTop = height - rulerH;
  const startTime = controller.panStartSample / sampleRate;
  const pixelsPerSecond = sampleRate / controller.samplesPerPixel;
  const ticks = getRulerTicks(startTime, width, pixelsPerSecond, 80);

  ctx.fillStyle = "#ffffff";
  ctx.fillRect(0, rulerTop, width, rulerH);
  ctx.strokeStyle = "#111827";
  ctx.beginPath();
  ctx.moveTo(0, rulerTop);
  ctx.lineTo(width, rulerTop);
  ctx.stroke();

  ctx.font = "12px Consolas, monospace";
  ctx.textAlign = "center";
  ctx.textBaseline = "top";
  ctx.fillStyle = "#111827";
  ctx.strokeStyle = "#111827";

  for (const tick of ticks) {
    const x = Math.round(tick.x);
    ctx.beginPath();
    ctx.moveTo(x, rulerTop + rulerH - 1);
    ctx.lineTo(x, rulerTop + rulerH - 11);
    ctx.stroke();
    ctx.fillText(tick.label, x, rulerTop + 4);
  }
}
