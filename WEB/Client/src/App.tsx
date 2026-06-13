import { ChangeEvent, useEffect, useMemo, useState } from "react";
import { AlertCircle, FileAudio, Loader2, Play } from "lucide-react";
import { flattenFiles, getDefaultConfig, getResult, getTextFile, processFile } from "./api";
import { loadLocalWaveAsset, loadTimelineAsset, loadWaveAsset } from "./audio";
import { WaveView } from "./components/WaveView";
import { TimelineView } from "./components/TimelineView";
import { ConfigTable } from "./components/ConfigTable";
import { BlockNavigator } from "./components/BlockNavigator";
import { clamp, createInitialController, updateViewportWidth } from "./viewController";
import type { ConfigParam, ResultFileNode, ResultManifest, TimelineAsset, ViewControllerState, WaveAsset } from "./types";

type LoadState = "idle" | "processing" | "loading-results" | "ready" | "error";

export function App() {
  const [inputFile, setInputFile] = useState<File | null>(null);
  const [sessionName, setSessionName] = useState("");
  const [state, setState] = useState<LoadState>("idle");
  const [error, setError] = useState<string | null>(null);
  const [manifest, setManifest] = useState<ResultManifest | null>(null);
  const [inputWave, setInputWave] = useState<WaveAsset | null>(null);
  const [resultWaves, setResultWaves] = useState<WaveAsset[]>([]);
  const [timeline, setTimeline] = useState<TimelineAsset | null>(null);
  const [controller, setController] = useState<ViewControllerState | null>(null);
  const [configParams, setConfigParams] = useState<ConfigParam[]>([]);
  const [overallResult, setOverallResult] = useState<string | null>(null);
  const [completeLog, setCompleteLog] = useState<string | null>(null);
  const [blockStartSamples, setBlockStartSamples] = useState<number[]>([]);
  const [blockIndex, setBlockIndex] = useState(0);
  const [hasProcessedResult, setHasProcessedResult] = useState(false);

  const canProcess = inputFile !== null && state !== "processing" && state !== "loading-results";
  const visibleFileCount = useMemo(() => (manifest ? flattenFiles(manifest.files).length : 0), [manifest]);

  useEffect(() => {
    let cancelled = false;

    getDefaultConfig()
      .then((params) => {
        if (!cancelled) {
          setConfigParams(params);
        }
      })
      .catch((caught) => {
        if (!cancelled) {
          setError(caught instanceof Error ? caught.message : "Could not load configuration.");
        }
      });

    return () => {
      cancelled = true;
    };
  }, []);

  async function handleProcess() {
    if (!inputFile) {
      return;
    }

    setState("processing");
    setError(null);
    setManifest(null);
    setResultWaves([]);
    setTimeline(null);
    setOverallResult(null);
    setCompleteLog(null);
    setBlockStartSamples([]);
    setBlockIndex(0);
    setHasProcessedResult(false);

    try {
      const session = sessionName.trim() || inputFile.name.replace(/\.[^.]+$/, "");
      const job = await processFile(inputFile, session, configParams);
      setState("loading-results");

      const result = await getResult(job.resultUrl);
      const files = flattenFiles(result.files);
      const waveFiles = files
        .filter((file) => file.name.toLowerCase().endsWith(".wav"))
        .sort(compareProcessingFiles);
      const timelineFile = files.find((file) => file.name.toLowerCase() === "timeline.json");
      const resultFile = pickDeepestFile(files, "result.txt");
      const logFile = pickDeepestFile(files, "combined log file.txt");

      const loadedResultWaves = await Promise.all(waveFiles.map((file) => loadWaveAsset(file, false)));
      const primaryWave = inputWave ?? loadedResultWaves[0];
      const colorCodedWave = loadedResultWaves.find((wave) => wave.colorCoded);
      const blocks = colorCodedWave ? loadBlocks(colorCodedWave.samples) : [];
      const resultText = resultFile?.url ? await getTextFile(resultFile.url) : "";
      const logText = logFile?.url ? await getTextFile(logFile.url) : result.messages.join("\n");

      setManifest(result);
      setOverallResult(formatOverallResult(resultText));
      setCompleteLog(logText);
      if (result.configParams.length > 0) {
        setConfigParams(result.configParams);
      }
      setResultWaves(loadedResultWaves);
      setBlockStartSamples(blocks);
      setBlockIndex(0);
      setHasProcessedResult(true);
      setTimeline(timelineFile ? await loadTimelineAsset(timelineFile) : null);
      if (!controller && primaryWave) {
        setController(createInitialController(primaryWave.samples.length, 1200));
      }
      setState("ready");
    } catch (caught) {
      setState("error");
      setError(caught instanceof Error ? caught.message : "Something went wrong.");
    }
  }

  async function handleFileChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0] ?? null;
    setInputFile(file);
    setManifest(null);
    setResultWaves([]);
    setTimeline(null);
    setOverallResult(null);
    setCompleteLog(null);
    setBlockStartSamples([]);
    setBlockIndex(0);
    setHasProcessedResult(false);
    setInputWave(null);
    setController(null);
    setError(null);
    setState("idle");

    if (file && !sessionName) {
      setSessionName(file.name.replace(/\.[^.]+$/, ""));
    }

    if (!file || !file.name.toLowerCase().endsWith(".wav")) {
      return;
    }

    try {
      const loadedInputWave = await loadLocalWaveAsset(file);
      setInputWave(loadedInputWave);
      setController(createInitialController(loadedInputWave.samples.length, 1200));
    } catch (caught) {
      setState("error");
      setError(caught instanceof Error ? caught.message : "Could not load the selected WAV file.");
    }
  }

  function handleViewportWidthChange(width: number) {
    setController((current) => current ? updateViewportWidth(current, width) : current);
  }

  function gotoBlock(index: number) {
    if (!controller || blockStartSamples.length < 2) {
      return;
    }

    const nextIndex = clamp(index, 0, blockStartSamples.length - 2);
    const startSample = blockStartSamples[nextIndex];
    const blockSize = Math.max(1, blockStartSamples[nextIndex + 1] - startSample);
    const availableWidth = Math.max(1, controller.viewportWidth);

    setBlockIndex(nextIndex);
    setController({
      ...controller,
      samplesPerPixel: blockSize / availableWidth,
      panStartSample: startSample
    });
  }

  return (
    <main className="app-shell">
      <header className="top-bar">
        <div>
          <h1>Transgraphier 2.4.1</h1>
          <p>Digital Instrumental Trans-Communication (ITC) workbench</p>
        </div>
        <div className="top-actions">
          <BlockNavigator
            blockCount={Math.max(0, blockStartSamples.length - 1)}
            currentBlock={blockIndex}
            disabled={!controller}
            showEmpty={hasProcessedResult}
            onFirst={() => gotoBlock(0)}
            onPrevious={() => gotoBlock(blockIndex - 1)}
            onNext={() => gotoBlock(blockIndex + 1)}
          />
          <div className="server-pill">Local server</div>
        </div>
      </header>

      <section className="upload-panel">
        <label className="file-drop">
          <input type="file" accept=".wav,.txt,audio/wav,text/plain" onChange={handleFileChange} />
          <FileAudio size={22} aria-hidden="true" />
          <span>{inputFile ? inputFile.name : "Choose a .wav or .txt file"}</span>
        </label>

        <label className="session-field">
          <span>Session</span>
          <input value={sessionName} onChange={(event) => setSessionName(event.target.value)} placeholder="Session name" />
        </label>

        <button className="primary-button" disabled={!canProcess} onClick={handleProcess}>
          {state === "processing" || state === "loading-results" ? <Loader2 className="spin" size={18} /> : <Play size={18} />}
          <span>{state === "processing" ? "Processing" : state === "loading-results" ? "Loading" : "Process"}</span>
        </button>
      </section>

      {error && (
        <section className="status-panel error-panel">
          <AlertCircle size={18} aria-hidden="true" />
          <span>{error}</span>
        </section>
      )}

      {overallResult && <pre className="overall-result">{overallResult}</pre>}

      <section className="workspace">
        {!inputWave && resultWaves.length === 0 && state === "idle" && (
          <div className="empty-state">Select a file to inspect it, then run the processor when you are ready.</div>
        )}

        {inputWave && controller && (
          <WaveView
            key={inputWave.id}
            wave={inputWave}
            controller={controller}
            onControllerChange={setController}
            onViewportWidthChange={handleViewportWidthChange}
          />
        )}

        <ConfigTable
          params={configParams}
          disabled={state === "processing" || state === "loading-results"}
          onChange={setConfigParams}
        />

        {resultWaves.length === 0 && state === "ready" && (
          <div className="empty-state">No visible waveform files were returned for this result.</div>
        )}

        {controller &&
          resultWaves.map((wave) => (
            <WaveView key={wave.id} wave={wave} controller={controller} onControllerChange={setController} />
          ))}

        {controller && timeline && (
          <TimelineView timeline={timeline} controller={controller} onControllerChange={setController} />
        )}
      </section>

      {manifest && (
        <section className="result-summary">
          <div>
            <span className="summary-label">Job</span>
            <strong>{manifest.jobId}</strong>
          </div>
          <div>
            <span className="summary-label">Session</span>
            <strong>{manifest.sessionName}</strong>
          </div>
          <div>
            <span className="summary-label">Visible files</span>
            <strong>{visibleFileCount}</strong>
          </div>
        </section>
      )}

      {completeLog && (
        <section className="complete-log">
          <div className="log-title">Complete Log</div>
          <pre>{completeLog}</pre>
        </section>
      )}
    </main>
  );
}

function loadBlocks(colorCodedSamples: Float32Array): number[] {
  const blockStarts: number[] = [];
  let separatorFound = false;
  const zeroThreshold = 0.0001;

  for (let i = 0; i < colorCodedSamples.length; i++) {
    const sample = colorCodedSamples[i];
    const absSample = Math.abs(sample);
    if (absSample <= zeroThreshold) {
      continue;
    }

    if (absSample > 0.9) {
      separatorFound = true;
    } else {
      if (separatorFound) {
        if (blockStarts[blockStarts.length - 1] !== i) {
          blockStarts.push(i);
        }
      }
      separatorFound = false;
    }
  }

  if (blockStarts.length === 0) {
    return [];
  }

  return [0, ...blockStarts, colorCodedSamples.length];
}

function compareProcessingFiles(a: ResultFileNode, b: ResultFileNode): number {
  const aPath = a.relativePath.split("/");
  const bPath = b.relativePath.split("/");
  const aDepth = aPath.length;
  const bDepth = bPath.length;

  if (aDepth !== bDepth) {
    return aDepth - bDepth;
  }

  const aFolder = aPath.slice(0, -1).join("/");
  const bFolder = bPath.slice(0, -1).join("/");
  if (aFolder !== bFolder) {
    return aFolder.localeCompare(bFolder);
  }

  return compareOrderedNames(a.name, b.name);
}

function compareOrderedNames(a: string, b: string): number {
  const aOrder = readOrderPrefix(a);
  const bOrder = readOrderPrefix(b);

  if (aOrder !== bOrder) {
    return aOrder - bOrder;
  }

  return a.localeCompare(b);
}

function readOrderPrefix(name: string): number {
  const match = name.match(/^(\d+)_/);
  return match ? Number(match[1]) : Number.MAX_SAFE_INTEGER;
}

function pickDeepestFile(files: ResultFileNode[], name: string): ResultFileNode | undefined {
  return files
    .filter((file) => file.name.toLowerCase() === name)
    .sort((a, b) => b.relativePath.split("/").length - a.relativePath.split("/").length)
    [0];
}

function formatOverallResult(resultText: string): string {
  const lines = resultText.split(/\r?\n/);
  const decodedIndex = lines.findIndex((line) => line.trim().toLowerCase() === "decoded text message:");
  const fitnessLine = lines.find((line) => line.trim().toLowerCase().startsWith("overall fitness:"));
  const decodedText = decodedIndex >= 0 ? lines[decodedIndex + 1]?.trim() ?? "" : "";

  if (!decodedText || decodedText.includes("SORRY") || decodedText.includes("NO MESSAGE")) {
    return "<<<< NO MESSAGE COULD BE DECODED >>>";
  }

  return [
    "Decoded Text Message:",
    decodedText,
    "",
    fitnessLine?.trim() ?? "Overall Fitness: Undefined"
  ].join("\n");
}
