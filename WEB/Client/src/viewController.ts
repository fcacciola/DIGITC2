import type { ViewControllerState } from "./types";

export function createInitialController(length: number, viewportWidth: number): ViewControllerState {
  const width = Math.max(1, viewportWidth);
  const maxSamplesPerPixel = Math.max(2, length / width);

  return {
    minSamplesPerPixel: 2,
    maxSamplesPerPixel,
    samplesPerPixel: maxSamplesPerPixel,
    panStartSample: 0,
    length,
    viewportWidth: width
  };
}

export function clamp(value: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, value));
}

export function updateViewportWidth(controller: ViewControllerState, width: number): ViewControllerState {
  const maxSamplesPerPixel = Math.max(controller.minSamplesPerPixel, controller.length / Math.max(1, width));
  return {
    ...controller,
    maxSamplesPerPixel,
    samplesPerPixel: clamp(controller.samplesPerPixel, controller.minSamplesPerPixel, maxSamplesPerPixel),
    panStartSample: clamp(controller.panStartSample, 0, controller.length),
    viewportWidth: Math.max(1, width)
  };
}
