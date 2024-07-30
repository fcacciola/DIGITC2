using NAudio.Wave;

using System;
using System.Collections.Generic;

namespace DIGITC2_ENGINE
{
  class AudioService : ISampleProvider
  {
    AudioFileReader? _reader;
    WaveOut _player;

    WaveInEvent    mRecorder;
    WaveFileWriter mWriter;



    const int MaxBufferSize = 64000;
    private readonly float[] _tmp = new float[MaxBufferSize];

    private WaveFormat? _waveFormat;

    //public event Action<float[]>? WaveformUpdated;
    //public event Action<List<float[]>>? VectorsComputed;

    public WaveFormat? WaveFormat => _waveFormat;

    public int MaxRecordingTime { get ; set ; } = 60 * 120 ;

    public event Action RecStopped;

    public void Load(string filename)
    {
      _player?.Stop();
      _player?.Dispose();
      _reader?.Dispose();

      _reader = new AudioFileReader(filename);

      //Channels = _reader.WaveFormat.Channels;
      _waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(_reader.WaveFormat.SampleRate, 1);

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

    public void StartRecording( string aFile, int deviceNumber = 0)
    {
      mRecorder = new WaveInEvent
      {
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(16000, 1),
        DeviceNumber = deviceNumber,
        BufferMilliseconds = 200
      };

      mRecorder.DataAvailable    += OnRecordedDataAvailable;
      mRecorder.RecordingStopped += OnRecordingStopped ;

      mWriter = new WaveFileWriter(aFile, mRecorder.WaveFormat) ;

      mRecorder?.StartRecording();
    }

    public void StopRecording()
    {
      mRecorder?.StopRecording();
    }

    void OnRecordedDataAvailable(object sender, WaveInEventArgs aWaveIn)
    {
      mWriter.Write(aWaveIn.Buffer, 0, aWaveIn.BytesRecorded);
      if ( mWriter.Position > mRecorder.WaveFormat.AverageBytesPerSecond * MaxRecordingTime)
      {
        mRecorder.StopRecording();
      }
    }

    void OnRecordingStopped(object sender, StoppedEventArgs aArgs)
    {
      mWriter?.Close();
      RecStopped?.Invoke();

      mWriter?.Dispose();
      mRecorder?.Dispose();

    }

    public void Dispose()
    {
      _player?.Dispose();
      _reader?.Dispose();
      mRecorder?.Dispose();
    }
  }
}
