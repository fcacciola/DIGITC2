import type { ResultFileNode, TimelineAsset, TimelineEntry, WaveAsset } from "./types";
import { resolveServerUrl } from "./api";

const engineSampleRate = 44100;

export type PreparedAudioInput = {
  file: File;
  wave: WaveAsset;
};

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

export async function prepareAudioInput(file: File): Promise<PreparedAudioInput> {
  const arrayBuffer = await file.arrayBuffer();
  let samples: Float32Array;

  if (file.name.toLowerCase().endsWith(".wav")) {
    try {
      const decoded = decodeWavMono(arrayBuffer);
      if (decoded.sampleRate === engineSampleRate) {
        return {
          file,
          wave: makeLocalWaveAsset(file, decoded.samples)
        };
      }

      samples = await resampleMono(decoded.samples, decoded.sampleRate, engineSampleRate);
    } catch {
      samples = await decodeBrowserAudioMono(arrayBuffer);
    }
  } else {
    samples = await decodeBrowserAudioMono(arrayBuffer);
  }

  const wav = encodePcm16Wav(samples, engineSampleRate);
  const normalizedFile = new File([wav], `${file.name.replace(/\.[^.]+$/, "")}.wav`, {
    type: "audio/wav",
    lastModified: file.lastModified
  });

  return {
    file: normalizedFile,
    wave: makeLocalWaveAsset(file, samples)
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

function makeLocalWaveAsset(file: File, samples: Float32Array): WaveAsset {
  return {
    id: `local:${file.name}:${file.lastModified}`,
    title: file.name.replace(/\.[^.]+$/, ""),
    url: null,
    relativePath: file.name,
    samples,
    sampleRate: engineSampleRate,
    colorCoded: false,
    includeRuler: true
  };
}

async function decodeBrowserAudioMono(arrayBuffer: ArrayBuffer): Promise<Float32Array> {
  const context = new OfflineAudioContext(1, 1, engineSampleRate);
  let audioBuffer: AudioBuffer;

  try {
    audioBuffer = await context.decodeAudioData(arrayBuffer);
  } catch {
    throw new Error("This browser could not decode the selected audio file.");
  }

  const samples = downmixToMono(audioBuffer);
  return audioBuffer.sampleRate === engineSampleRate
    ? samples
    : resampleMono(samples, audioBuffer.sampleRate, engineSampleRate);
}

function downmixToMono(audioBuffer: AudioBuffer): Float32Array {
  const samples = new Float32Array(audioBuffer.length);

  for (let channel = 0; channel < audioBuffer.numberOfChannels; channel++) {
    const channelSamples = audioBuffer.getChannelData(channel);
    for (let i = 0; i < samples.length; i++) {
      samples[i] += channelSamples[i] / audioBuffer.numberOfChannels;
    }
  }

  return samples;
}

async function resampleMono(
  samples: Float32Array,
  sourceSampleRate: number,
  targetSampleRate: number
): Promise<Float32Array> {
  if (sourceSampleRate === targetSampleRate || samples.length === 0) {
    return samples;
  }

  const targetLength = Math.max(1, Math.round(samples.length * targetSampleRate / sourceSampleRate));
  const context = new OfflineAudioContext(1, targetLength, targetSampleRate);
  const sourceBuffer = context.createBuffer(1, samples.length, sourceSampleRate);
  sourceBuffer.getChannelData(0).set(samples);

  const source = context.createBufferSource();
  source.buffer = sourceBuffer;
  source.connect(context.destination);
  source.start();

  const rendered = await context.startRendering();
  return new Float32Array(rendered.getChannelData(0));
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
    let sample = 0;
    for (let channel = 0; channel < channelCount; channel++) {
      const sampleOffset = dataOffset + frame * blockAlign + channel * bytesPerSample;
      sample += readSample(view, sampleOffset, audioFormat, bitsPerSample, bytesPerSample);
    }

    samples[frame] = sample / channelCount;
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

function encodePcm16Wav(samples: Float32Array, sampleRate: number): Blob {
  const bytesPerSample = 2;
  const dataSize = samples.length * bytesPerSample;
  const buffer = new ArrayBuffer(44 + dataSize);
  const view = new DataView(buffer);

  writeAscii(view, 0, "RIFF");
  view.setUint32(4, 36 + dataSize, true);
  writeAscii(view, 8, "WAVE");
  writeAscii(view, 12, "fmt ");
  view.setUint32(16, 16, true);
  view.setUint16(20, 1, true);
  view.setUint16(22, 1, true);
  view.setUint32(24, sampleRate, true);
  view.setUint32(28, sampleRate * bytesPerSample, true);
  view.setUint16(32, bytesPerSample, true);
  view.setUint16(34, 16, true);
  writeAscii(view, 36, "data");
  view.setUint32(40, dataSize, true);

  let offset = 44;
  for (const sample of samples) {
    const clamped = Math.max(-1, Math.min(1, sample));
    const value = clamped < 0 ? clamped * 32768 : clamped * 32767;
    view.setInt16(offset, value, true);
    offset += bytesPerSample;
  }

  return new Blob([buffer], { type: "audio/wav" });
}

function writeAscii(view: DataView, offset: number, value: string) {
  for (let i = 0; i < value.length; i++) {
    view.setUint8(offset + i, value.charCodeAt(i));
  }
}
