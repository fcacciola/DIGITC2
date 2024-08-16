using NAudio.Wave;

using System;
using System.Collections.Generic;

using DIGITC2 ;

namespace DIGITC2_ENGINE
{
  public class AudioService : ISampleProvider
  {
    AudioFileReader mReader;
    WaveOut mPlayer;

    WaveInEvent    mRecorder;
    WaveFileWriter mWriter;



    const int MaxBufferSize = 64000;
    private readonly float[] _tmp = new float[MaxBufferSize];

    private WaveFormat mWaveFormat;

    //public event Action<float[]>? WaveformUpdated;
    //public event Action<List<float[]>>? VectorsComputed;

    public WaveFormat WaveFormat => mWaveFormat;

    public int MaxRecordingTime { get ; set ; } = 60 * 120 ;

    public event Action RecStopped;

    public void Load(string aFilename)
    {
      mPlayer?.Stop();
      mPlayer?.Dispose();
      mReader?.Dispose();

      mReader = new AudioFileReader(aFilename);

      //Channels = _reader.WaveFormat.Channels;
      mWaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(mReader.WaveFormat.SampleRate, 1);

      mPlayer = new WaveOut();
      mPlayer.Init(this);
    }

    public List<AudioDevice> EnumInputDevices()
    {
      List<AudioDevice> rDevices = new List<AudioDevice>();  
      for (int n = -1; n < WaveIn.DeviceCount; n++)
      {
        var lCaps = WaveIn.GetCapabilities(n);
        rDevices.Add( new AudioDevice{ Name = lCaps.ProductName, Number = n } );
      }

      return rDevices;
    }

    public List<AudioDevice> EnumOutputDevices()
    {
      List<AudioDevice> rDevices = new List<AudioDevice>();  
      for (int n = -1; n < WaveOut.DeviceCount; n++)
      {
        var lCaps = WaveOut.GetCapabilities(n);
        rDevices.Add( new AudioDevice{ Name = lCaps.ProductName, Number = n } );
      }

      return rDevices;
    }
    
    public int Read(float[] buffer, int offset, int count)
    {
      if (mReader is null)
      {
        return 0;
      }

      return mReader.WaveFormat.Channels switch
      {
        1 => ReadMono(buffer, offset, count),
        _ => ReadStereo(buffer, offset, count),
      };
    }

    public int ReadMono(float[] buffer, int offset, int count)
    {
      if (mReader is null)
      {
        return 0;
      }

      var rSamplesRead = mReader.Read(buffer, offset, count);

      return rSamplesRead;
    }

    public int ReadStereo(float[] buffer, int offset, int count)
    {
      if (mReader is null)
      {
        return 0;
      }

      var samplesRead = mReader.Read(buffer, offset, count);

      return samplesRead;
    }

    public void Play()
    {
      if (mPlayer is null)
      {
        return;
      }

      if (mPlayer.PlaybackState == PlaybackState.Stopped)
      {
        mReader?.Seek(0, System.IO.SeekOrigin.Begin);
      }

      mPlayer.Play();
    }

    public void Pause()
    {
      mPlayer?.Pause();
    }

    public void Stop()
    {
      mPlayer?.Stop();
      mReader?.Seek(0, System.IO.SeekOrigin.Begin);
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
      mPlayer?.Dispose();
      mReader?.Dispose();
      mRecorder?.Dispose();
    }
  }
}
