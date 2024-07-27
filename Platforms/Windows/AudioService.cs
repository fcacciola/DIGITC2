using NAudio.Wave;

using System;
using System.Collections.Generic;

namespace DIGITC2_ENGINE
{
    class AudioService : ISampleProvider
    {
        private AudioFileReader? _reader;
        private WaveOut? _player;
        private WaveIn? _recorder;


        const int MaxBufferSize = 64000;
        private readonly float[] _tmp = new float[MaxBufferSize];

        private WaveFormat? _waveFormat;

        //public event Action<float[]>? WaveformUpdated;
        //public event Action<List<float[]>>? VectorsComputed;

        public WaveFormat? WaveFormat => _waveFormat;

        public int Channels { get; protected set; }

        public void Load(string filename)
        {
            _player?.Stop();
            _player?.Dispose();
            _reader?.Dispose();

            _reader = new AudioFileReader(filename);

            Channels = _reader.WaveFormat.Channels;
            _waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(_reader.WaveFormat.SampleRate, Channels);

            _player = new WaveOut();
            _player.Init(this);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            if (_reader is null)
            {
                return 0;
            }

            return _reader.WaveFormat.Channels switch
            {
                1 => ReadMono(buffer, offset, count),
                _ => ReadStereo(buffer, offset, count),
            };
        }

        public int ReadMono(float[] buffer, int offset, int count)
        {
            if (_reader is null)
            {
                return 0;
            }

            var samplesRead = _reader.Read(buffer, offset, count);

            return samplesRead;
        }

        public int ReadStereo(float[] buffer, int offset, int count)
        {
            if (_reader is null)
            {
                return 0;
            }

            var samplesRead = _reader.Read(buffer, offset, count);

            return samplesRead;
        }

        public void Play()
        {
            if (_player is null)
            {
                return;
            }

            if (_player.PlaybackState == PlaybackState.Stopped)
            {
                _reader?.Seek(0, System.IO.SeekOrigin.Begin);
            }

            _player.Play();
        }

        public void Pause()
        {
            _player?.Pause();
        }

        public void Stop()
        {
            _player?.Stop();
            _reader?.Seek(0, System.IO.SeekOrigin.Begin);
        }

        public void StartRecording(int deviceNumber = 0)
        {
            _recorder?.StopRecording();
            _recorder?.Dispose();

            _recorder = new WaveIn
            {
                WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(16000, 1),
                DeviceNumber = deviceNumber,
                BufferMilliseconds = 200
            };

            _recorder.DataAvailable += OnRecordedDataAvailable;
            
            _recorder?.StartRecording();
        }

        public void StopRecording()
        {
            _recorder?.StopRecording();
            _recorder?.Dispose();
        }

        private void OnRecordedDataAvailable(object? sender, WaveInEventArgs waveInArgs)
        {
            var buffer = new WaveBuffer(waveInArgs.Buffer);

            var size = waveInArgs.BytesRecorded / 4;
        }

        public void Dispose()
        {
            _player?.Dispose();
            _reader?.Dispose();
            _recorder?.Dispose();
        }
    }
}
