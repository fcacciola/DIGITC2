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

  const audioContext = new AudioContext();
  const buffer = await audioContext.decodeAudioData(await response.arrayBuffer());
  const samples = new Float32Array(buffer.getChannelData(0));
  await audioContext.close();

  return {
    id: file.relativePath,
    title: makeWaveTitle(file),
    url: file.url,
    relativePath: file.relativePath,
    samples,
    sampleRate: buffer.sampleRate,
    colorCoded: file.name.toLowerCase().includes("colorcoded"),
    includeRuler
  };
}

export async function loadLocalWaveAsset(file: File): Promise<WaveAsset> {
  const audioContext = new AudioContext();
  const buffer = await audioContext.decodeAudioData(await file.arrayBuffer());
  const samples = new Float32Array(buffer.getChannelData(0));
  await audioContext.close();

  return {
    id: `local:${file.name}:${file.lastModified}`,
    title: file.name.replace(/\.wav$/i, ""),
    url: null,
    relativePath: file.name,
    samples,
    sampleRate: buffer.sampleRate,
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
