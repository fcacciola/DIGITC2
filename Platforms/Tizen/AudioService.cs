using System;
using System.Collections.Generic;

namespace DIGITC2_ENGINE
{
    class AudioService 
    {
        public int MaxRecordingTime { get ; set ; } = 60 * 120 ;

        public event Action RecStopped;

        public int Channels => 1

        public void Load(string filename)
        {
        }

        public int Read(float[] buffer, int offset, int count)
        {
        }

        public int ReadMono(float[] buffer, int offset, int count)
        {
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

        //private void OnRecordedDataAvailable(object? sender, WaveInEventArgs waveInArgs)
        //{
        //}

        public void Dispose()
        {
        }
    }
}
