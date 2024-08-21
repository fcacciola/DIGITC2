using System;
using System.Collections.Generic;
using System.IO;

using NWaves.Signals;
using NWaves.Signals.Builders;
using NWaves.Audio;

namespace DIGITC2_ENGINE {

public abstract class AudioGenerator
{
  public abstract DiscreteSignal Generate(Args aArgs) ;
}


public abstract class NoiseGenerator : AudioGenerator
{
  protected DiscreteSignal GenerateNoise( double aDuration, double aLevel )
  {
    int lRate = 44100 ;
    int lSamples = (int)Math.Ceiling(lRate * aDuration);

    double lAmplitud = aLevel / 200.0 ;
    DiscreteSignal rNoise = new WhiteNoiseBuilder()
                                .SetParameter("min", - lAmplitud)
                                .SetParameter("max", lAmplitud)
                                .OfLength(lSamples)
                                .SampledAt(lRate)
                                .Build();

    return rNoise;
  }
}



}
