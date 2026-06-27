import { useCallback, useEffect, useRef, useState, type MutableRefObject } from "react";
import type { MeasureSelection, ViewControllerState, WaveAsset } from "../types";
import { clamp } from "../viewController";
import { getRulerTicks } from "../ruler";

type WaveViewProps = {
  wave: WaveAsset;
  controller: ViewControllerState;
  onControllerChange: (controller: ViewControllerState) => void;
  onViewportWidthChange?: (width: number) => void;
  measureEnabled?: boolean;
  measureSelection?: MeasureSelection | null;
  onMeasureSelectionChange?: (selection: MeasureSelection | null) => void;
};

const titleHeight = 0;
const marginSize = 2;
const rulerHeight = 24;

export function WaveView({
  wave,
  controller,
  onControllerChange,
  onViewportWidthChange,
  measureEnabled = false,
  measureSelection = null,
  onMeasureSelectionChange
}: WaveViewProps) {
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const cacheRef = useRef<HTMLCanvasElement | null>(null);
  const controllerRef = useRef(controller);
  const dragRef = useRef<{ x: number; panning: boolean } | null>(null);
  const measureDragRef = useRef<{ startSample: number } | null>(null);
  const [size, setSize] = useState({ width: 1, height: 76 });

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
        height: Math.max(64, Math.floor(rect.height))
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
    renderWave(canvasRef.current, cacheRef, wave, controller, size.width, size.height, measureSelection);
  }, [wave, controller, size, measureSelection]);

  return (
    <section className="controlled-view" aria-label={wave.title}>
      <div className="view-title">{wave.title}</div>
      <canvas
        ref={canvasRef}
        className={measureEnabled ? "wave-canvas measure-enabled" : "wave-canvas"}
        onPointerDown={(event) => {
          if (measureEnabled) {
            const startSample = getSampleFromPointer(event.currentTarget, event.clientX, controllerRef.current);
            measureDragRef.current = { startSample };
            onMeasureSelectionChange?.({ startSample, endSample: startSample });
            event.currentTarget.setPointerCapture(event.pointerId);
            event.preventDefault();
            return;
          }

          dragRef.current = { x: event.clientX, panning: true };
          event.currentTarget.setPointerCapture(event.pointerId);
        }}
        onPointerMove={(event) => {
          if (measureDragRef.current) {
            const endSample = getSampleFromPointer(event.currentTarget, event.clientX, controllerRef.current);
            onMeasureSelectionChange?.({
              startSample: measureDragRef.current.startSample,
              endSample
            });
            event.preventDefault();
            return;
          }

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
          measureDragRef.current = null;
          dragRef.current = null;
          event.currentTarget.releasePointerCapture(event.pointerId);
        }}
        onPointerCancel={() => {
          measureDragRef.current = null;
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
  cssHeight: number,
  measureSelection: MeasureSelection | null
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
  ctx.fillStyle = "#d6d3ce";
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
  visible.setTransform(dpr, 0, 0, dpr, 0, 0);
  drawMeasureSelection(visible, measureSelection, controller, cssWidth, cssHeight);
  visible.setTransform(1, 0, 0, 1, 0, 0);
}

function getSampleFromPointer(canvas: HTMLCanvasElement, clientX: number, controller: ViewControllerState): number {
  const rect = canvas.getBoundingClientRect();
  const x = clamp(clientX - rect.left, 0, rect.width);
  return clamp(
    controller.panStartSample + x * controller.samplesPerPixel - controller.samplesPerPixel / 2,
    0,
    controller.length
  );
}

function drawMeasureSelection(
  ctx: CanvasRenderingContext2D,
  selection: MeasureSelection | null,
  controller: ViewControllerState,
  width: number,
  height: number
) {
  if (!selection) {
    return;
  }

  const start = Math.min(selection.startSample, selection.endSample);
  const end = Math.max(selection.startSample, selection.endSample);
  const visibleStart = controller.panStartSample;
  const visibleEnd = controller.panStartSample + width * controller.samplesPerPixel;
  if (end < visibleStart || start > visibleEnd) {
    return;
  }

  const x1 = clamp((start - controller.panStartSample) / controller.samplesPerPixel, 0, width);
  const x2 = clamp((end - controller.panStartSample) / controller.samplesPerPixel, 0, width);

  if (Math.abs(x2 - x1) < 0.5) {
    ctx.strokeStyle = "#0f766e";
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.moveTo(Math.round(x1) + 0.5, 0);
    ctx.lineTo(Math.round(x1) + 0.5, height);
    ctx.stroke();
    return;
  }

  ctx.fillStyle = "rgba(20, 184, 166, 0.18)";
  ctx.fillRect(x1, 0, x2 - x1, height);
  ctx.strokeStyle = "#0f766e";
  ctx.lineWidth = 1;
  ctx.beginPath();
  ctx.moveTo(Math.round(x1) + 0.5, 0);
  ctx.lineTo(Math.round(x1) + 0.5, height);
  ctx.moveTo(Math.round(x2) + 0.5, 0);
  ctx.lineTo(Math.round(x2) + 0.5, height);
  ctx.stroke();
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
  else drawSamples_Normal    (ctx, wave, controller, width, centerY, waveHalfH);
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
  const LV_BLACK   = 0.9;
  const LV_BLUE    = 0.6;
  const LV_RED     = -0.6;
  const LV_GREEN   = 0.4;
  const LV_YELLOW  = - 0.4;

  const cy  = Math.round(centerY);
  const yOf = (v: number) => Math.round(centerY - v * waveHalfH);

  const blackY  = yOf(-LV_BLACK);
  const blueY   = yOf(-LV_BLUE);
  const redY    = yOf(-LV_RED);
  const greenY  = yOf(-LV_GREEN);
  const yellowY = yOf(-LV_YELLOW);

  const blue   = new Path2D();
  const red    = new Path2D();
  const yellow = new Path2D();
  const green  = new Path2D();
  const black  = new Path2D();

  for (let px = 0; px < width; px++) 
  {
    const sampleStart = startSample + Math.floor(px * samplesPerPixel);
    if (sampleStart > endSample || sampleStart >= samples.length)
      break;

    let sampleEnd = startSample + Math.ceil((px + 1) * samplesPerPixel) - 1;
    sampleEnd = Math.min(sampleEnd, endSample);
    if (sampleEnd < sampleStart) {
      sampleEnd = sampleStart;
    }

    let min = Infinity;
    let max = -Infinity;
    for (let i = sampleStart; i <= sampleEnd; i++)
    {
      const v = samples[i];
      if (v < min) min = v;
      if (v > max) max = v;
    }

    let path : Path2D  | null = null ;
    let y0 : number = 0 ;
    let y1 : number = 0 ;

    switch (Math.ceil(max * 10))
    {
      case 6: // Blue
          path = blue ;
          y0 = yOf(LV_BLUE) ;
          y1 = cy - y0 ;
          break;
      case -5: // Red
          path = red ;
          y0 = cy ;
          y1 =  yOf(LV_RED) - cy ;
          break;
      case 2: // Green
          path = green ;
          y0 = yOf(LV_GREEN) ;
          y1 = cy - y0 ;
          break;
      case -1: // Yellow
          path = yellow ;
          y0 = cy ;
          y1 =  yOf(LV_YELLOW) - cy ;
          break;
      case 9: // Black
          path = black ;
          y0 = yOf(LV_BLACK) ;
          y1 = yOf(-LV_BLACK) - y0 ;
          break;
    }
    
    if ( path !== null )
      path.rect(px, y0, 1, y1);
  }

  fillPath(ctx, yellow, "#f1f908");
  fillPath(ctx, green,  "#3aee08");
  fillPath(ctx, blue,   "#2563eb");
  fillPath(ctx, red,    "#dc2626");
  fillPath(ctx, black,  "#111827");
}

function fillPath(ctx: CanvasRenderingContext2D, path: Path2D, color: string) {
  ctx.fillStyle = color;
  ctx.fill(path);
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

  drawPath(ctx, path, "#2563eb" ,  started);
}

function addPolylinePoint(path: Path2D, started: boolean, x: number, y: number): boolean {
  if (started) {
    path.lineTo(x, y);
  } else {
    path.moveTo(x, y);
  }

  return true;
}

function drawPath(ctx: CanvasRenderingContext2D, path: Path2D, color: string, used: boolean) {
  if (!used) {
    return;
  }

  ctx.strokeStyle = color;
  ctx.lineWidth = 0;
  ctx.lineCap = "butt";
  ctx.lineJoin = "bevel";
  ctx.stroke(path);
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

  ctx.font = "10px Consolas, monospace";
  ctx.textAlign = "center";
  ctx.textBaseline = "top";
  ctx.fillStyle = "#111827";
  ctx.strokeStyle = "#111827";

  for (const tick of ticks) {
    const x = Math.round(tick.x);
    ctx.beginPath();
    ctx.moveTo(x, rulerTop + rulerH - 1);
    ctx.lineTo(x, rulerTop + rulerH - 8);
    ctx.stroke();
    ctx.fillText(tick.label, x, rulerTop + 3);
  }
}
