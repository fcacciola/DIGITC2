import type { ConfigParam, ProcessJobResponse, ResultFileNode, ResultManifest } from "./types";

export async function getDefaultConfig(): Promise<ConfigParam[]> {
  const response = await fetch("/api/config");
  const payload = await response.json();

  if (!response.ok) {
    throw new Error(payload.error ?? "Could not load configuration.");
  }

  return payload;
}

export async function processFile(file: File, sessionName: string, configParams: ConfigParam[]): Promise<ProcessJobResponse> {
  const form = new FormData();
  form.append("file", file);
  form.append("name", sessionName);
  form.append("config", JSON.stringify(configParams));

  const response = await fetch("/api/jobs", {
    method: "POST",
    body: form
  });

  const payload = await response.json();
  if (!response.ok) {
    throw new Error(payload.error ?? "Processing failed.");
  }

  return payload;
}

export async function getResult(resultUrl: string): Promise<ResultManifest> {
  const response = await fetch(resultUrl);
  const payload = await response.json();

  if (!response.ok) {
    throw new Error(payload.error ?? "Could not load result.");
  }

  return payload;
}

export async function getTextFile(url: string): Promise<string> {
  const response = await fetch(resolveServerUrl(url));

  if (!response.ok) {
    throw new Error("Could not load text file.");
  }

  return response.text();
}

export function flattenFiles(nodes: ResultFileNode[]): ResultFileNode[] {
  const files: ResultFileNode[] = [];

  for (const node of nodes) {
    if (node.kind === "file") {
      files.push(node);
    }

    if (node.children) {
      files.push(...flattenFiles(node.children));
    }
  }

  return files;
}

export function resolveServerUrl(url: string): string {
  if (!url.startsWith("http://") && !url.startsWith("https://")) {
    return url;
  }

  const parsed = new URL(url);
  if (parsed.pathname.startsWith("/api/")) {
    return `${parsed.pathname}${parsed.search}${parsed.hash}`;
  }

  return url;
}
