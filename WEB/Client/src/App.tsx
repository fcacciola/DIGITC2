import { ChangeEvent, useEffect, useMemo, useRef, useState } from "react";
import { AlertCircle, BookOpen, Download, FileAudio, Loader2, Mic, Play, Ruler, Square } from "lucide-react";
import { flattenFiles, getDefaultConfig, getResult, getTextFile, processFile } from "./api";
import { loadLocalWaveAsset, loadTimelineAsset, loadWaveAsset } from "./audio";
import { WaveView } from "./components/WaveView";
import { TimelineView } from "./components/TimelineView";
import { ConfigTable } from "./components/ConfigTable";
import { BlockNavigator } from "./components/BlockNavigator";
import { UserManual } from "./components/UserManual";
import { clamp, createInitialController, updateViewportWidth } from "./viewController";
import type { ConfigParam, MeasureSelection, ResultFileNode, ResultManifest, TimelineAsset, ViewControllerState, WaveAsset } from "./types";
import { startWavRecorder, type WavRecorderSession } from "./recorder";

type LoadState = "idle" | "processing" | "loading-results" | "ready" | "error";

export function App() {
  const recorderRef = useRef<WavRecorderSession | null>(null);
  const [inputFile, setInputFile] = useState<File | null>(null);
  const [sessionName, setSessionName] = useState("");
  const [state, setState] = useState<LoadState>("idle");
  const [error, setError] = useState<string | null>(null);
  const [manifest, setManifest] = useState<ResultManifest | null>(null);
  const [inputWave, setInputWave] = useState<WaveAsset | null>(null);
  const [resultWaves, setResultWaves] = useState<WaveAsset[]>([]);
  const [timeline, setTimeline] = useState<TimelineAsset | null>(null);
  const [controller, setController] = useState<ViewControllerState | null>(null);
  const [defaultConfigParams, setDefaultConfigParams] = useState<ConfigParam[] | null>(null);
  const [configParams, setConfigParams] = useState<ConfigParam[]>([]);
  const [overallResult, setOverallResult] = useState<string | null>(null);
  const [completeLog, setCompleteLog] = useState<string | null>(null);
  const [blockStartSamples, setBlockStartSamples] = useState<number[]>([]);
  const [blockIndex, setBlockIndex] = useState(0);
  const [hasProcessedResult, setHasProcessedResult] = useState(false);
  const [measureEnabled, setMeasureEnabled] = useState(false);
  const [measureSelection, setMeasureSelection] = useState<MeasureSelection | null>(null);
  const [isRecording, setIsRecording] = useState(false);
  const [recordedDownloadUrl, setRecordedDownloadUrl] = useState<string | null>(null);
  const [recordedFileName, setRecordedFileName] = useState<string | null>(null);
  const [manualOpen, setManualOpen] = useState(false);

  const canProcess = inputFile !== null && !isRecording && state !== "processing" && state !== "loading-results";
  const visibleFileCount = useMemo(() => (manifest ? flattenFiles(manifest.files).length : 0), [manifest]);
  const measurementSampleRate = inputWave?.sampleRate ?? resultWaves[0]?.sampleRate ?? null;
  const measurementText = formatMeasurement(measureSelection, measurementSampleRate);

  useEffect(() => {
    let cancelled = false;

    getDefaultConfig()
      .then((params) => {
        if (!cancelled) {
          setDefaultConfigParams(params);
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

  useEffect(() => {
    return () => {
      recorderRef.current?.cancel();
      if (recordedDownloadUrl) {
        URL.revokeObjectURL(recordedDownloadUrl);
      }
    };
  }, [recordedDownloadUrl]);

  useEffect(() => {
    if (!manualOpen) {
      return;
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setManualOpen(false);
      }
    };

    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [manualOpen]);

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
    setMeasureSelection(null);

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
      const blockWave = loadedResultWaves.find(isBlockWave);
      const blocks = blockWave ? loadBlocks(blockWave.samples) : [];
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
    if (recordedDownloadUrl) {
      URL.revokeObjectURL(recordedDownloadUrl);
      setRecordedDownloadUrl(null);
      setRecordedFileName(null);
    }

    await loadInputFile(file, true);
  }

  async function handleStartRecording() {
    setError(null);

    try {
      recorderRef.current = await startWavRecorder();
      setIsRecording(true);
    } catch (caught) {
      setState("error");
      setError(caught instanceof Error ? caught.message : "Could not start recording.");
    }
  }

  async function handleStopRecording() {
    const recorder = recorderRef.current;
    if (!recorder) {
      return;
    }

    setIsRecording(false);
    recorderRef.current = null;

    try {
      const file = await recorder.stop();
      if (recordedDownloadUrl) {
        URL.revokeObjectURL(recordedDownloadUrl);
      }

      setRecordedDownloadUrl(URL.createObjectURL(file));
      setRecordedFileName(file.name);
      await loadInputFile(file, true);
    } catch (caught) {
      setState("error");
      setError(caught instanceof Error ? caught.message : "Could not finish recording.");
    }
  }

  async function loadInputFile(file: File | null, updateSessionName: boolean) {
    setInputFile(file);
    if (defaultConfigParams) {
      setConfigParams(defaultConfigParams.map((param) => ({ ...param })));
    }
    setManifest(null);
    setResultWaves([]);
    setTimeline(null);
    setOverallResult(null);
    setCompleteLog(null);
    setBlockStartSamples([]);
    setBlockIndex(0);
    setHasProcessedResult(false);
    setMeasureSelection(null);
    setInputWave(null);
    setController(null);
    setError(null);
    setState("idle");

    if (file && updateSessionName) {
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
          <button
            type="button"
            className={measureEnabled ? "measure-tool is-active" : "measure-tool"}
            disabled={!controller}
            onClick={() => {
              setMeasureEnabled((current) => !current);
              if (measureEnabled) {
                setMeasureSelection(null);
              }
            }}
            title="Measure time interval"
            aria-pressed={measureEnabled}
          >
            <Ruler size={16} aria-hidden="true" />
            <span>Measure</span>
          </button>
          <div className="measure-readout" aria-live="polite">{measurementText}</div>
          <button type="button" className="manual-button" onClick={() => setManualOpen(true)} title="Open user manual">
            <BookOpen size={16} aria-hidden="true" />
            <span>Manual</span>
          </button>
          <div className="server-pill">Local server</div>
        </div>
      </header>

      <section className="upload-panel">
        <label className="file-drop">
          <input type="file" accept=".wav,.txt,audio/wav,text/plain" disabled={isRecording} onChange={handleFileChange} />
          <FileAudio size={22} aria-hidden="true" />
          <span>{inputFile ? inputFile.name : "Choose a .wav or .txt file"}</span>
        </label>

        <label className="session-field">
          <span>Session</span>
          <input value={sessionName} onChange={(event) => setSessionName(event.target.value)} placeholder="Session name" />
        </label>

        <div className="recording-tools">
          <button
            type="button"
            className={isRecording ? "record-button is-recording" : "record-button"}
            disabled={state === "processing" || state === "loading-results"}
            onClick={isRecording ? handleStopRecording : handleStartRecording}
            title={isRecording ? "Stop recording" : "Record audio"}
          >
            {isRecording ? <Square size={16} aria-hidden="true" /> : <Mic size={16} aria-hidden="true" />}
            <span>{isRecording ? "Stop" : "Record"}</span>
          </button>

          <a
            className={recordedDownloadUrl ? "download-recording" : "download-recording is-disabled"}
            href={recordedDownloadUrl ?? undefined}
            download={recordedFileName ?? undefined}
            aria-disabled={!recordedDownloadUrl}
            title="Download recorded WAV"
          >
            <Download size={16} aria-hidden="true" />
            <span>Download</span>
          </a>
        </div>

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

      {overallResult && <OverallResult result={overallResult} />}

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
            measureEnabled={measureEnabled}
            measureSelection={measureSelection}
            onMeasureSelectionChange={setMeasureSelection}
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
            <WaveView
              key={wave.id}
              wave={wave}
              controller={controller}
              onControllerChange={setController}
              measureEnabled={measureEnabled}
              measureSelection={measureSelection}
              onMeasureSelectionChange={setMeasureSelection}
            />
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

      {manualOpen && <UserManual onClose={() => setManualOpen(false)} />}
    </main>
  );
}

function loadBlocks(blockSamples: Float32Array): number[] {
  const blockBoundaries: number[] = [];
  let separatorFound = false;
  const separatorThreshold = 0.85;
  const pulseThreshold = 0.05;

  for (let i = 0; i < blockSamples.length; i++) {
    const sample = blockSamples[i];
    const isSeparator = sample >= separatorThreshold;

    if (isSeparator) {
      separatorFound = true;
      continue;
    }

    const isPulseAfterSeparator = separatorFound && Math.abs(sample) >= pulseThreshold;
    if (isPulseAfterSeparator) {
      if (blockBoundaries[blockBoundaries.length - 1] !== i) {
        blockBoundaries.push(i);
      }
      separatorFound = false;
    }
  }

  if (blockBoundaries.length === 0) {
    return [];
  }

  return [0, ...blockBoundaries, blockSamples.length];
}

function isBlockWave(wave: WaveAsset): boolean {
  return wave.relativePath.toLowerCase().includes("blocks") || wave.title.toLowerCase().includes("blocks");
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

  const decodedTextLine = lines.find((line) =>
    line.trim().toLowerCase().startsWith("decoded text message:")
  );

  const scoreLine = lines.find((line) =>
    line.trim().toLowerCase().startsWith("score:")
  );

  const decodedText = decodedTextLine
    ? decodedTextLine.substring(decodedTextLine.indexOf(":") + 1).trim()
    : "";

  const score = scoreLine
    ? scoreLine.trim()
    : "Score: Undefined";

  if (!decodedText ) {
    return "<<<< NO MESSAGE COULD BE DECODED >>>";
  }

  return [
    "Decoded Text Message:",
    decodedText,
    "",
    score
  ].join("\n");
}

function OverallResult({ result }: { result: string }) {
  const lines = result.split("\n");
  const decodedLineIndex = getDecodedMessageLineIndex(result);

  if (decodedLineIndex < 0) {
    return <pre className={isNoMessageResult(result) ? "overall-result no-message-result" : "overall-result"}>{result}</pre>;
  }

  return (
    <pre className="overall-result">
      {lines.map((line, index) => (
        <span key={`${index}-${line}`} className={index === decodedLineIndex ? "decoded-message-line" : undefined}>
          {line}
          {index < lines.length - 1 ? "\n" : ""}
        </span>
      ))}
    </pre>
  );
}

function getDecodedMessageLineIndex(result: string): number {
  const lines = result.split("\n");
  const headerIndex = lines.findIndex((line) => line.trim().toLowerCase() === "decoded text message:");
  if (headerIndex < 0) {
    return -1;
  }

  const decodedLineIndex = headerIndex + 1;
  const decodedText = lines[decodedLineIndex]?.trim() ?? "";
  if (!decodedText || result.toLowerCase().includes("no message could be decoded")) {
    return -1;
  }

  return decodedLineIndex;
}

function isNoMessageResult(result: string): boolean {
  return result.toLowerCase().includes("no message could be decoded");
}

function formatMeasurement(selection: MeasureSelection | null, sampleRate: number | null): string {
  if (!selection || !sampleRate) {
    return "No interval selected";
  }

  const sampleCount = Math.abs(selection.endSample - selection.startSample);
  const seconds = sampleCount / sampleRate;
  const milliseconds = seconds * 1000;

  if (sampleCount < 1) {
    return "0 samples";
  }

  if (seconds < 1) {
    return `${sampleCount.toFixed(0)} samples | ${milliseconds.toFixed(3)} ms`;
  }

  return `${sampleCount.toFixed(0)} samples | ${seconds.toFixed(6)} s`;
}
