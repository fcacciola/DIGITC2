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
) {
  const { samples } = wave;
  const samplesPerPixel = controller.samplesPerPixel;
  const startSample = Math.floor(controller.panStartSample);
  const visibleSamples = Math.ceil(width * samplesPerPixel);
  const endSample = Math.min(Math.min(samples.length, controller.length) - 1, startSample + visibleSamples);

  // if (shouldRenderAsStepWave(wave)) {
  //   drawStepSamples(ctx, samples, samplesPerPixel, startSample, endSample, width, centerY, waveHalfH);
  //   return;
  // }

  // if (wave.colorCoded) {
  //   drawColorCodedSamples(ctx, samples, samplesPerPixel, startSample, endSample, width, centerY, waveHalfH);
  //   return;
  // }

  const paths = {
    red: new Path2D(),
    blue: new Path2D(),
    black: new Path2D(),
    gray: new Path2D()
  };

  const started = {
    red: false,
    blue: false,
    black: false,
    gray: false
  };

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

    let ly = Math.ceil(min * waveHalfH);
    let hy = Math.ceil(max * waveHalfH);
    
    if ( wave.colorCoded )
    {
      let path_key    : keyof typeof paths   = ( min != max ) ? "gray" : ( max > 0.8 ? "black" : ( max > 0.5 ?"blue" : "red" ) )  ;
      let started_key : keyof typeof started = ( min != max ) ? "gray" : ( max > 0.8 ? "black" : ( max > 0.5 ?"blue" : "red" ) )  ;

      started[started_key] = addPolylinePoint(paths[path_key], started[started_key], px, centerY - ly);
      started[started_key] = addPolylinePoint(paths[path_key], started[started_key], px, centerY - hy);
    }
    else
    {
      started.blue = addPolylinePoint(paths.blue, started.blue, px, centerY - ly);
      started.blue = addPolylinePoint(paths.blue, started.blue, px, centerY - hy);
    }
  }

  if ( wave.colorCoded )
  {
    drawPath(ctx, paths.red, "#dc2626", started.red);
    drawPath(ctx, paths.black, "#111827", started.black);
    drawPath(ctx, paths.gray, "#6b7280", started.gray);
  }
  drawPath(ctx, paths.blue, "#2563eb", started.blue);
}

function drawColorCodedSamples(
  ctx: CanvasRenderingContext2D,
  samples: Float32Array,
  samplesPerPixel: number,
  startSample: number,
  endSample: number,
  width: number,
  centerY: number,
  waveHalfH: number
) {
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

    const ly = centerY - Math.ceil(min * waveHalfH);
    const hy = centerY - Math.ceil(max * waveHalfH);
    
    ctx.fillStyle = getColorCodedFill(min, max);
    ctx.fillRect(px, ly, 1, hy-ly);
  }
}

function getColorCodedFill(min: number, max: number): string {
  if (min !== max) {
    return "#6b7280";
  }

  if (max > 0.8) {
    return "#111827";
  }

  if (max > 0.5) {
    return "#2563eb";
  }

  return "#dc2626";
}

function shouldRenderAsStepWave(wave: WaveAsset): boolean {
  const name = `${wave.title} ${wave.relativePath}`.toLowerCase();
  return !wave.colorCoded && (name.includes("pulses") || name.includes("discretized"));
}

function drawStepSamples(
  ctx: CanvasRenderingContext2D,
  samples: Float32Array,
  samplesPerPixel: number,
  startSample: number,
  endSample: number,
  width: number,
  centerY: number,
  waveHalfH: number
) {
  const path = new Path2D();
  let started = false;
  let previousY = 0;

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

    const value = samples[sampleEnd];
    const y = centerY - Math.ceil(value * waveHalfH);

    if (!started) {
      path.moveTo(px, y);
      previousY = y;
      started = true;
      continue;
    }

    if (y !== previousY) {
      path.lineTo(px, previousY);
      path.lineTo(px, y);
    } else {
      path.lineTo(px, y);
    }

    previousY = y;
  }

  drawPath(ctx, path, "#2563eb", started);
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
