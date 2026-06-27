import type { ResultFileNode, TimelineAsset, TimelineEntry, WaveAsset } from "./types";
import { resolveServerUrl } from "./api";

export async function loadWaveAsset(file: ResultFileNode, includeRuler: boolean): Promise<WaveAsset> {
  if (!file.url) {
    throw new Error(`Wave file has no URL: ${file.relativePath}`);
  }

  const response = await fetch(resolveServerUrl(file.url));
  if (!response.ok) {
    throw new Error(`Could not download ${file.name}.`);
  }

  const decoded = decodeWavMono(await response.arrayBuffer());

  return {
    id: file.relativePath,
    title: makeWaveTitle(file),
    url: file.url,
    relativePath: file.relativePath,
    samples: decoded.samples,
    sampleRate: decoded.sampleRate,
    colorCoded: isColorCodedWaveFile(file),
    includeRuler
  };
}

export async function loadLocalWaveAsset(file: File): Promise<WaveAsset> {
  const decoded = decodeWavMono(await file.arrayBuffer());

  return {
    id: `local:${file.name}:${file.lastModified}`,
    title: file.name.replace(/\.wav$/i, ""),
    url: null,
    relativePath: file.name,
    samples: decoded.samples,
    sampleRate: decoded.sampleRate,
    colorCoded: false,
    includeRuler: true
  };
}

export async function loadTimelineAsset(file: ResultFileNode): Promise<TimelineAsset> {
  if (!file.url) {
    throw new Error(`Timeline file has no URL: ${file.relativePath}`);
  }

  const response = await fetch(resolveServerUrl(file.url));
  if (!response.ok) {
    throw new Error("Could not download Timeline.json.");
  }

  const json = await response.json();
  const rawEntries: Array<{ Pixel?: number; Label?: string; pixel?: number; label?: string }> =
    json.Entries ?? json.entries ?? [];

  const entries: TimelineEntry[] = rawEntries
    .map((entry) => ({
      pixel: entry.Pixel ?? entry.pixel ?? 0,
      label: entry.Label ?? entry.label ?? ""
    }))
    .filter((entry) => entry.label.length > 0);

  return {
    title: "Timeline",
    url: file.url,
    entries
  };
}

function makeWaveTitle(file: ResultFileNode): string {
  const baseName = file.name.replace(/\.wav$/i, "");
  return baseName.includes("_") ? baseName.split("_").slice(1).join("_") : baseName;
}

function isColorCodedWaveFile(file: ResultFileNode): boolean {
  const path = `${file.relativePath}/${file.name}`.toLowerCase();
  return path.includes("colorcoded") || path.includes("blocks");
}

function decodeWavMono(arrayBuffer: ArrayBuffer): { samples: Float32Array; sampleRate: number } {
  const view = new DataView(arrayBuffer);

  if (readAscii(view, 0, 4) !== "RIFF" || readAscii(view, 8, 4) !== "WAVE") {
    throw new Error("Unsupported WAV file.");
  }

  let offset = 12;
  let audioFormat = 0;
  let channelCount = 0;
  let sampleRate = 0;
  let bitsPerSample = 0;
  let blockAlign = 0;
  let dataOffset = -1;
  let dataSize = 0;

  while (offset + 8 <= view.byteLength) {
    const id = readAscii(view, offset, 4);
    const size = view.getUint32(offset + 4, true);
    const chunkDataOffset = offset + 8;

    if (id === "fmt ") {
      audioFormat = view.getUint16(chunkDataOffset, true);
      channelCount = view.getUint16(chunkDataOffset + 2, true);
      sampleRate = view.getUint32(chunkDataOffset + 4, true);
      blockAlign = view.getUint16(chunkDataOffset + 12, true);
      bitsPerSample = view.getUint16(chunkDataOffset + 14, true);
    } else if (id === "data") {
      dataOffset = chunkDataOffset;
      dataSize = size;
    }

    offset = chunkDataOffset + size + (size % 2);
  }

  if (dataOffset < 0 || channelCount <= 0 || sampleRate <= 0 || bitsPerSample <= 0 || blockAlign <= 0) {
    throw new Error("Incomplete WAV file.");
  }

  const frameCount = Math.floor(dataSize / blockAlign);
  const samples = new Float32Array(frameCount);
  const bytesPerSample = bitsPerSample / 8;

  for (let frame = 0; frame < frameCount; frame++) {
    const sampleOffset = dataOffset + frame * blockAlign;
    samples[frame] = readSample(view, sampleOffset, audioFormat, bitsPerSample, bytesPerSample);
  }

  return { samples, sampleRate };
}

function readSample(
  view: DataView,
  offset: number,
  audioFormat: number,
  bitsPerSample: number,
  bytesPerSample: number
): number {
  if (audioFormat === 3) {
    if (bitsPerSample === 32) {
      return view.getFloat32(offset, true);
    }

    if (bitsPerSample === 64) {
      return view.getFloat64(offset, true);
    }
  }

  if (audioFormat !== 1) {
    throw new Error(`Unsupported WAV audio format: ${audioFormat}`);
  }

  switch (bitsPerSample) {
    case 8:
      return (view.getUint8(offset) - 128) / 128;
    case 16:
      return view.getInt16(offset, true) / 32768;
    case 24: {
      const value = view.getUint8(offset) | (view.getUint8(offset + 1) << 8) | (view.getUint8(offset + 2) << 16);
      const signed = value & 0x800000 ? value | ~0xffffff : value;
      return signed / 8388608;
    }
    case 32:
      return view.getInt32(offset, true) / 2147483648;
    default:
      throw new Error(`Unsupported WAV bit depth: ${bitsPerSample} (${bytesPerSample} bytes)`);
  }
}

function readAscii(view: DataView, offset: number, length: number): string {
  let result = "";
  for (let i = 0; i < length; i++) {
    result += String.fromCharCode(view.getUint8(offset + i));
  }

  return result;
}
