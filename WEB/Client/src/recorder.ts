export type WavRecorderSession = {
  stop: () => Promise<File>;
  cancel: () => Promise<void>;
};

const engineSampleRate = 44100;

export async function startWavRecorder(): Promise<WavRecorderSession> {
  if (!navigator.mediaDevices?.getUserMedia) {
    throw new Error("Microphone recording is not available in this browser.");
  }

  const stream = await navigator.mediaDevices.getUserMedia({
    audio: {
      channelCount: 1,
      echoCancellation: false,
      noiseSuppression: false,
      autoGainControl: false
    }
  });

  const AudioContextCtor = window.AudioContext ?? window.webkitAudioContext;
  if (!AudioContextCtor) {
    stopStream(stream);
    throw new Error("Audio recording is not available in this browser.");
  }

  const audioContext = new AudioContextCtor();
  const source = audioContext.createMediaStreamSource(stream);
  const processor = audioContext.createScriptProcessor(4096, 1, 1);
  const chunks: Float32Array[] = [];

  processor.onaudioprocess = (event) => {
    const input = event.inputBuffer.getChannelData(0);
    chunks.push(new Float32Array(input));

    const output = event.outputBuffer.getChannelData(0);
    output.fill(0);
  };

  source.connect(processor);
  processor.connect(audioContext.destination);

  async function cleanup() {
    processor.disconnect();
    source.disconnect();
    stopStream(stream);
    await audioContext.close();
  }

  return {
    stop: async () => {
      await cleanup();
      const samples = concatenateChunks(chunks);
      const engineSamples = resampleLinear(samples, audioContext.sampleRate, engineSampleRate);
      const wav = encodePcm16Wav(engineSamples, engineSampleRate);
      const timestamp = new Date().toISOString().replace(/[:.]/g, "-");
      return new File([wav], `Recording_${timestamp}.wav`, {
        type: "audio/wav",
        lastModified: Date.now()
      });
    },
    cancel: cleanup
  };
}

declare global {
  interface Window {
    webkitAudioContext?: typeof AudioContext;
  }
}

function stopStream(stream: MediaStream) {
  for (const track of stream.getTracks()) {
    track.stop();
  }
}

function concatenateChunks(chunks: Float32Array[]): Float32Array {
  const length = chunks.reduce((sum, chunk) => sum + chunk.length, 0);
  const result = new Float32Array(length);
  let offset = 0;

  for (const chunk of chunks) {
    result.set(chunk, offset);
    offset += chunk.length;
  }

  return result;
}

function resampleLinear(samples: Float32Array, sourceSampleRate: number, targetSampleRate: number): Float32Array {
  if (sourceSampleRate === targetSampleRate) {
    return samples;
  }

  if (samples.length === 0) {
    return samples;
  }

  const targetLength = Math.max(1, Math.round(samples.length * targetSampleRate / sourceSampleRate));
  const result = new Float32Array(targetLength);
  const ratio = sourceSampleRate / targetSampleRate;

  for (let i = 0; i < targetLength; i++) {
    const sourcePosition = i * ratio;
    const leftIndex = Math.floor(sourcePosition);
    const rightIndex = Math.min(samples.length - 1, leftIndex + 1);
    const fraction = sourcePosition - leftIndex;
    result[i] = samples[leftIndex] + (samples[rightIndex] - samples[leftIndex]) * fraction;
  }

  return result;
}

function encodePcm16Wav(samples: Float32Array, sampleRate: number): Blob {
  const channelCount = 1;
  const bytesPerSample = 2;
  const blockAlign = channelCount * bytesPerSample;
  const byteRate = sampleRate * blockAlign;
  const dataSize = samples.length * bytesPerSample;
  const buffer = new ArrayBuffer(44 + dataSize);
  const view = new DataView(buffer);

  writeAscii(view, 0, "RIFF");
  view.setUint32(4, 36 + dataSize, true);
  writeAscii(view, 8, "WAVE");
  writeAscii(view, 12, "fmt ");
  view.setUint32(16, 16, true);
  view.setUint16(20, 1, true);
  view.setUint16(22, channelCount, true);
  view.setUint32(24, sampleRate, true);
  view.setUint32(28, byteRate, true);
  view.setUint16(32, blockAlign, true);
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
