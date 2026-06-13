export type RulerTick = {
  time: number;
  x: number;
  label: string;
};

const timeUnits = [0.01, 0.05, 0.1, 0.5, 1.0, 10.0, 30.0, 60.0];

export function formatTickLabel(timeSeconds: number, unit: number): string {
  const totalSeconds = Math.max(0, timeSeconds);
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = Math.floor(totalSeconds % 60);

  if (unit >= 60) {
    return `${minutes}m`;
  }

  if (unit >= 30) {
    return `${minutes}:${seconds.toString().padStart(2, "0")}m`;
  }

  const decimals = unit >= 0.5 ? 1 : 2;
  const fraction = totalSeconds - Math.floor(totalSeconds);
  const fractionText = fraction > 0
    ? decimals === 1
      ? `.${Math.floor(fraction * 10)}`
      : `.${Math.floor(fraction * 100).toString().padStart(2, "0")}`
    : "";

  const prefix = minutes > 0 ? `${minutes}:` : "";
  const suffix = minutes > 0 ? "m" : "s";
  return `${prefix}${seconds.toString().padStart(2, "0")}${fractionText}${suffix}`;
}

export function getRulerTicks(
  startTime: number,
  viewportWidthPx: number,
  pixelsPerSecond: number,
  minSpacingPx = 80
): RulerTick[] {
  const unit = pickTimeUnit(pixelsPerSecond, minSpacingPx);
  const endTime = startTime + viewportWidthPx / pixelsPerSecond;
  let time = firstTickTime(startTime, unit);
  const ticks: RulerTick[] = [];

  while (time <= endTime) {
    ticks.push({
      time,
      x: (time - startTime) * pixelsPerSecond,
      label: formatTickLabel(time, unit)
    });
    time = Math.round((time + unit) * 10_000_000_000) / 10_000_000_000;
  }

  return ticks;
}

function pickTimeUnit(pixelsPerSecond: number, minSpacingPx: number): number {
  for (const unit of timeUnits) {
    if (unit * pixelsPerSecond >= minSpacingPx) {
      return unit;
    }
  }

  return timeUnits[timeUnits.length - 1];
}

function firstTickTime(startTime: number, unit: number): number {
  return Math.ceil(startTime / unit) * unit;
}
