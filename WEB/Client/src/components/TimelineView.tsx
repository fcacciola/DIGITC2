import { useEffect, useRef, useState, type MutableRefObject } from "react";
import type { TimelineAsset, ViewControllerState } from "../types";

type TimelineViewProps = {
  timeline: TimelineAsset;
  controller: ViewControllerState;
  onControllerChange: (controller: ViewControllerState) => void;
};

const titleHeight = 30;
const marginSize = 2;

export function TimelineView({ timeline, controller, onControllerChange }: TimelineViewProps) {
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const cacheRef = useRef<HTMLCanvasElement | null>(null);
  const [size, setSize] = useState({ width: 1, height: 140 });

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) {
      return;
    }

    const resizeObserver = new ResizeObserver((entries) => {
      const rect = entries[0].contentRect;
      setSize({
        width: Math.max(1, Math.floor(rect.width)),
        height: Math.max(80, Math.floor(rect.height))
      });
    });

    resizeObserver.observe(canvas);
    return () => resizeObserver.disconnect();
  }, []);

  useEffect(() => {
    renderTimeline(canvasRef.current, cacheRef, timeline, controller, size.width, size.height);
  }, [timeline, controller, size]);

  return (
    <section className="controlled-view timeline-view" aria-label={timeline.title}>
      <div className="view-title">{timeline.title}</div>
      <canvas ref={canvasRef} className="timeline-canvas" />
    </section>
  );
}

function renderTimeline(
  canvas: HTMLCanvasElement | null,
  cacheRef: MutableRefObject<HTMLCanvasElement | null>,
  timeline: TimelineAsset,
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
  ctx.clearRect(0, 0, cssWidth, cssHeight);
  ctx.fillStyle = "#dff8fb";
  ctx.fillRect(0, 0, cssWidth, cssHeight);

  const viewH = cssHeight - marginSize * 2 - titleHeight;
  const centerY = cssHeight - marginSize - viewH / 2;
  const labels = collectLabelsForPixels(timeline, controller, cssWidth);

  ctx.font = "700 20px Consolas, monospace";
  ctx.textAlign = "center";
  ctx.textBaseline = "middle";
  ctx.fillStyle = "#111827";

  for (const label of labels) {
    ctx.fillText(label.label, label.pixel, centerY);
  }

  visible.clearRect(0, 0, canvas.width, canvas.height);
  visible.drawImage(cache, 0, 0);
}

function collectLabelsForPixels(timeline: TimelineAsset, controller: ViewControllerState, width: number) {
  const labels: Array<{ pixel: number; label: string }> = [];
  const samplesPerPixel = controller.samplesPerPixel;
  const startSample = Math.floor(controller.panStartSample);
  const visibleSamples = Math.ceil(width * samplesPerPixel);
  const endSample = Math.min(controller.length + 1, startSample + visibleSamples);

  if (timeline.entries.length === 0) {
    return labels;
  }

  for (let px = 0; px < width; px++) {
    const sampleStart = startSample + Math.floor(px * samplesPerPixel);
    if (sampleStart > endSample) {
      break;
    }

    let sampleEnd = startSample + Math.ceil((px + 1) * samplesPerPixel) - 1;
    sampleEnd = Math.min(sampleEnd, endSample);
    if (sampleEnd < sampleStart) {
      sampleEnd = sampleStart;
    }

    const entry = timeline.entries.find((candidate) => candidate.pixel >= sampleStart && candidate.pixel <= sampleEnd);
    if (entry) {
      labels.push({ pixel: px, label: entry.label });
    }
  }

  return labels;
}
