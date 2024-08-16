using System;
using System.Collections.Generic;

using DIGITC2 ;

namespace DIGITC2_ENGINE
{
    public class AudioService 
    {
        public int MaxRecordingTime { get ; set ; } = 60 * 120 ;

        public event Action RecStopped;

        public int Channels => 1 ;

        public List<AudioDevice> EnumInputDevices()
        {
          return new List<AudioDevice>();
        }

        public List<AudioDevice> EnumOutputDevices()
        {
          return new List<AudioDevice>();
        }

        public void Load(string filename)
        {
        }

        public int Read(float[] buffer, int offset, int count)
        {
          return 0 ;
        }

        public int ReadMono(float[] buffer, int offset, int count)
        {
          return 0 ;
        }

        public void Play()
        {
        }

        public void Pause()
        {
        }

        public void Stop()
        {
        }

        public void StartRecording( string aFile, int deviceNumber = 0)
        {
        }

        public void StopRecording()
        {
        }

        //private void OnRecordedDataAvailable(object? sender, WaveInEventArgs waveInArgs)
        //{
        //}

    }
}
