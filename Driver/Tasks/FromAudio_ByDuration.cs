using System.Collections.Generic;
using System.IO;

using DIGITC2_ENGINE ;

namespace DIGITC2 {

public sealed class FromAudio_ByPulseDuration : DecodingTask
{
  public override void Run( Args aArgs  )
  {
    string lArg = aArgs.Get("InputAudioFile" ) ;

    List<string> lFiles = new List<string>() ;

    if ( lArg.Contains(",") )
    {
      lFiles.AddRange( lArg.Split(','));
    }
    else
    {
      lFiles.Add(lArg) ;
    }

    lFiles.ForEach( f => RunWithFile(aArgs,f) );  
  }

  void RunWithFile( Args aArgs, string aWaveFilename  )
  {
    var lSession = new Session("FromAudio_" +  Path.GetFileNameWithoutExtension(aWaveFilename), aArgs, Task.BaseFolder) ;

    DContext.Setup(lSession) ;

    if ( File.Exists( aWaveFilename ) )
    {
      var lSource = new WaveFileSource(aWaveFilename) ;

      var lSignal = lSource.CreateSignal() ;

      var lPipeline = PipelineFactory.FromAudioToBits_ByPulseDuration().Then( PipelineFactory.FromBits() ) ;

      var lResult = Processor.Process(lSession.Name, lPipeline, lSignal);

      lResult.Save() ;
    }
    else
    {
      DContext.Error("Could not find audio file: [" + aWaveFilename + "]");
    }

    DContext.Shutdown(); 
  }

}
}
