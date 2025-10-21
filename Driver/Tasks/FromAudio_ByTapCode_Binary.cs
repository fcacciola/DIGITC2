using System.Collections.Generic;
using System.IO;

using DIGITC2_ENGINE ;

namespace DIGITC2 {

public sealed class FromAudio_ByTapCode_Binary : DecodingTask
{ 
  public override void Run(Settings aSettings, List<Config> aConfigs)
  {
    var lFile = aSettings.GetPath("InputAudioFile") ;

    RunWithFile(aSettings, aConfigs, lFile)   
  }

  void RunWithFile( Settings aSettings, List<Config> aConfigs, string aWaveFilename  )
  {
    var lSession = new Session( this.GetType().Name, aSettings, BaseFolder);

    DContext.Setup( lSession ) ;

    if ( File.Exists( aWaveFilename ) )
    {
      var lSource = new WaveFileSource(aWaveFilename) ;

      var lSignal = lSource.CreateSignal() ;

      var lPipeline = PipelineFactory.FromAudioToBits_ByTapCode().Then( PipelineFactory.FromBits() ) ;

      var lResult = Processor.Process(lSession.Name, aConfigs, lPipeline, lSignal);

      lResult.Save( lSession.CurrentOutputFolder )  ;
    }
    else
    {
      DContext.Error("Could not find audio file: [" + aWaveFilename + "]");
    }

    DContext.Shutdown(); 
  }

}
}
