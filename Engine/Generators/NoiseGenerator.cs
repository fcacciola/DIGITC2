using System;
using System.Collections.Generic;
using System.IO;

using NWaves.Signals;
using NWaves.Signals.Builders;
using NWaves.Audio;

namespace DIGITC2_ENGINE {


public class NoiseLab
{
  static public DiscreteSignal GenerateNoise( int aSamples, double aLevel )
  {
    DContext.WriteLine("Generating Noise");
    
    //double lAmplitud = aLevel / 200.0 ;
    DiscreteSignal rNoise = new WhiteNoiseBuilder()
                                .SetParameter("min", - aLevel)
                                .SetParameter("max", aLevel)
                                .OfLength(aSamples)
                                .SampledAt(X.SamplingRate)
                                .Build();

    return rNoise;
  }

  static public DiscreteSignal GenerateNoise( double aDuration, double aLevel )
  {
    DContext.WriteLine("Generating Noise");
    
    return GenerateNoise((int)Math.Ceiling(X.SamplingRate * aDuration), aLevel);
  }

  static public void ModulateNoise(DiscreteSignal rCarrier, List<DiscreteSignal> aMasks )
  {
    for (int i = 0; i < rCarrier.Length; i++)
    {
      float lSample = rCarrier[i];
      foreach (var lMask in aMasks)
      {
        lSample = lSample * Math.Abs(lMask[i]);
      }

      rCarrier[i] = lSample;
    }
  }

  static public void _ModulateNoise(DiscreteSignal rCarrier, List<DiscreteSignal> aMasks, float aNoiseWeight )
  {
    float lAllMasksWeights = 1.0f - aNoiseWeight;
    float lSingleMaskWeight = lAllMasksWeights / aMasks.Count;

    for (int i = 0; i < rCarrier.Length; i++)
    {
      float lSample = aNoiseWeight * rCarrier[i];
      float lSign = lSample > 0 ? 1.0f : -1.0f;
      foreach (var lMask in aMasks)
      {
        float lMaskSample = lSign * lSingleMaskWeight * lMask[i];

        lSample += lMaskSample;
      }

      rCarrier[i] = lSample;
    }
  }

  static public void ModulateNoise(DiscreteSignal rCarrier, DiscreteSignal aMask )
  {
    ModulateNoise(rCarrier, new List<DiscreteSignal> { aMask });
  }

}

public abstract class AudioGenerator
{
  public abstract DiscreteSignal Generate(Args aArgs) ;
}


public abstract class NoiseGenerator : AudioGenerator
{

}


}
