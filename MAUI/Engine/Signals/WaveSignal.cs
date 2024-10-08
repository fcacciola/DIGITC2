﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Audio;
using NWaves.Signals;

namespace DIGITC2_ENGINE
{
  public class WaveSignal : Signal
  {
    public WaveSignal( DiscreteSignal aRep ) : base()
    { 
      Rep = aRep ;
    }

    public DiscreteSignal Rep ;
    
    public double  Duration     => Rep.Duration ;
    public int     SamplingRate => Rep.SamplingRate ;
    public float[] Samples      => Rep.Samples ; 

    public WaveSignal CopyWith( DiscreteSignal aDS )
    {
      WaveSignal rCopy = new WaveSignal(aDS);
      rCopy.Assign(this); 
      return rCopy ;
    }

    public override Signal Copy() => CopyWith(Rep.Copy());      

    public float ComputeMax() => Rep.Samples.Max();

    public override Distribution GetDistribution()
    {
      List<Sample> lSamples = new List<Sample>();
      for (int i = 0; i < Samples.Length; ++i)
        lSamples.Add( new Sample( new WaveValueSampleSource(i), Samples[i]) ) ;
      return new Distribution(lSamples);
    }

    public void SaveTo( Stream aStream ) 
    {
      var lWF = new WaveFile(Rep);
      lWF.SaveTo( aStream );  
    }

    public void SaveTo( string aFilename )  
    {
      using (var lStream = new FileStream(aFilename, FileMode.OpenOrCreate, FileAccess.Write))
        SaveTo( lStream );  
    }

    public WaveSignal Transform(Func<float, float> Transformation)
    {
      float[] lTransformedSamples = new float[Samples.Length];
      for (int i = 0; i < Samples.Length; i++)
        lTransformedSamples[i] = Transformation(Samples[i]);
      return CopyWith(new DiscreteSignal(SamplingRate, lTransformedSamples));
    }


    protected override void UpdateState( State rS ) 
    {
      rS.Add( State.With("Duration"    , Duration));
      rS.Add( State.With("SamplingRate", SamplingRate));
      rS.Add( State.With("SamplingRate", Samples));
    }
  }
}
