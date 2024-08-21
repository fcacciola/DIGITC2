using System;
using System.Collections.Generic;
using System.IO;

using NWaves.Signals;
using NWaves.Signals.Builders;

using DIGITC2_ENGINE ;

namespace DIGITC2 {

public abstract class NoiseGeneratorTaskBase : GeneratorTask
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

public sealed class GenerateNoise : NoiseGeneratorTaskBase
{
  public override void Run( Args aArgs  )
  {
    DIGITC_Context.Setup( new Session("Generate Noise", aArgs) ) ;

    DIGITC_Context.WriteLine("Generate Noise");

    double lDuration = aArgs.GetDouble("NoiseDuration");
    double lLevel    = aArgs.GetDouble("NoiseLevel");

    var lNoise = GenerateNoise(lDuration, lLevel); 

    string lFilename = aArgs.Get("NoiseFile");

    Save(lNoise,lFilename+"_noise.wav");
  }
}

}
