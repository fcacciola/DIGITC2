export type ProcessJobResponse = {
  jobId: string;
  sessionName: string;
  status: string;
  resultUrl: string;
};

export type ResultFileNode = {
  name: string;
  relativePath: string;
  kind: "file" | "folder";
  size: number | null;
  contentType: string | null;
  url: string | null;
  children: ResultFileNode[] | null;
};

export type ResultManifest = {
  jobId: string;
  sessionName: string;
  status: string;
  createdAt: string;
  winningBranchName: string;
  branchCount: number;
  files: ResultFileNode[];
  configParams: ConfigParam[];
  messages: string[];
  errors: string[];
};

export type ConfigParam = {
  section: string;
  key: string;
  name: string;
  value: string;
  label: string;
};

export type WaveAsset = {
  id: string;
  title: string;
  url: string | null;
  relativePath: string;
  samples: Float32Array;
  sampleRate: number;
  colorCoded: boolean;
  includeRuler: boolean;
};

export type TimelineEntry = {
  pixel: number;
  label: string;
};

export type TimelineAsset = {
  title: string;
  url: string;
  entries: TimelineEntry[];
};

export type ViewControllerState = {
  minSamplesPerPixel: number;
  maxSamplesPerPixel: number;
  samplesPerPixel: number;
  panStartSample: number;
  length: number;
  viewportWidth: number;
};

export type MeasureSelection = {
  startSample: number;
  endSample: number;
};
