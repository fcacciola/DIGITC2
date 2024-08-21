using System.IO;
using DIGITC2.ViewModel;

using NWaves.Signals;
using NWaves.Signals.Builders;

using DIGITC2_ENGINE;

using Newtonsoft.Json ;

namespace DIGITC2;


public class NoiseGenerator
{
  static public void Generate( string aOutputWaveFile ) 
  {
    Args lArgs = new Args();
  
    TapCodeMaskNoiseGenerator lGenerator = new TapCodeMaskNoiseGenerator();

    var lSignal = lGenerator.Generate(lArgs); 

    lSignal.Save(aOutputWaveFile);
  }  

  
}