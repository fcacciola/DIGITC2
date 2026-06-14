import type { ConfigParam, ProcessJobResponse, ResultFileNode, ResultManifest } from "./types";

export async function getDefaultConfig(): Promise<ConfigParam[]> {
  const response = await fetchApi("/api/config", undefined, "Could not reach the local server while loading configuration.");
  const payload = await readJsonResponse<ConfigParam[]>(response, "Could not load configuration.");

  if (!response.ok) {
    throw new Error(readErrorMessage(payload, "Could not load configuration."));
  }

  return payload;
}

export async function processFile(file: File, sessionName: string, configParams: ConfigParam[]): Promise<ProcessJobResponse> {
  const form = new FormData();
  form.append("file", file);
  form.append("name", sessionName);
  form.append("config", JSON.stringify(configParams));

  const response = await fetchApi("/api/jobs", {
    method: "POST",
    body: form
  }, "Could not reach the local server while uploading the input file. Check that the server is running and that the file is under the upload limit.");

  const payload = await readJsonResponse<ProcessJobResponse>(response, "Processing failed.");
  if (!response.ok) {
    throw new Error(readErrorMessage(payload, "Processing failed."));
  }

  return payload;
}

export async function getResult(resultUrl: string): Promise<ResultManifest> {
  const response = await fetchApi(resultUrl, undefined, "Could not reach the local server while loading the result.");
  const payload = await readJsonResponse<ResultManifest>(response, "Could not load result.");

  if (!response.ok) {
    throw new Error(readErrorMessage(payload, "Could not load result."));
  }

  return payload;
}

export async function getTextFile(url: string): Promise<string> {
  const response = await fetchApi(resolveServerUrl(url), undefined, "Could not reach the local server while loading a result file.");

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

async function fetchApi(url: string, init: RequestInit | undefined, networkErrorMessage: string): Promise<Response> {
  try {
    return await fetch(url, init);
  } catch (caught) {
    if (caught instanceof TypeError) {
      throw new Error(networkErrorMessage);
    }

    throw caught;
  }
}

async function readJsonResponse<T>(response: Response, fallbackMessage: string): Promise<T> {
  const contentType = response.headers.get("content-type") ?? "";

  if (contentType.includes("application/json")) {
    return response.json();
  }

  const text = await response.text();
  if (!response.ok) {
    throw new Error(extractPlainTextError(text) || fallbackMessage);
  }

  throw new Error(fallbackMessage);
}

function readErrorMessage(payload: unknown, fallbackMessage: string): string {
  if (payload && typeof payload === "object") {
    const errorPayload = payload as { error?: unknown; detail?: unknown; title?: unknown };
    for (const candidate of [errorPayload.error, errorPayload.detail, errorPayload.title]) {
      if (typeof candidate === "string" && candidate.trim().length > 0) {
        return candidate;
      }
    }
  }

  return fallbackMessage;
}

function extractPlainTextError(text: string): string {
  const normalized = text
    .replace(/<style[\s\S]*?<\/style>/gi, " ")
    .replace(/<script[\s\S]*?<\/script>/gi, " ")
    .replace(/<[^>]+>/g, " ")
    .replace(/\s+/g, " ")
    .trim();

  if (!normalized) {
    return "";
  }

  const exceptionMatch = normalized.match(/System\.[^:]+:\s*([^]+?)(?:\s+at\s+|$)/);
  if (exceptionMatch?.[1]) {
    return exceptionMatch[1].trim();
  }

  return normalized.length > 280 ? `${normalized.slice(0, 277)}...` : normalized;
}
