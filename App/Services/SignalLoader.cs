using NAudio.Wave;

using NWaves.Signals;

using System;

namespace DIGITC2_App.Services
{
  /// <summary>
  /// Helper class for loading audio signals from WAV files.
  /// </summary>
  public static class SignalLoader
  {
    /// <summary>
    /// Loads a WAV file and returns a DiscreteSignal (uses first channel if stereo).
    /// </summary>
    public static DiscreteSignal LoadSignal(string path)
    {
      using var rdr = new WaveFileReader(path);
      ISampleProvider sp = rdr.ToSampleProvider();
      var sampleRate = rdr.WaveFormat.SampleRate;
      var sampleCount = (int)rdr.SampleCount;
      var channels = rdr.WaveFormat.Channels;

      var buffer = new float[sampleCount];
      int read;
      int offset = 0;
      var temp = new float[1024 * channels];
      while ((read = sp.Read(temp, 0, temp.Length)) > 0)
      {
        if (channels == 1)
        {
          Array.Copy(temp, 0, buffer, offset, read);
        }
        else
        {
          int outIdx = offset;
          for (int i = 0; i < read; i += channels)
          {
            buffer[outIdx++] = temp[i];
          }
        }
        offset += (channels == 1) ? read : (read / channels);
      }

      if (offset != sampleCount)
      {
        Array.Resize(ref buffer, offset);
      }

      return new DiscreteSignal(sampleRate, buffer);
    }
  }
}
