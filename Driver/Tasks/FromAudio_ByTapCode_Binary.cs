using System.Collections.Generic;
using System.IO;

using DIGITC2_ENGINE ;

namespace DIGITC2 {

public sealed class FromAudio_ByTapCode_Binary : DecodingTask
{ 
  public override void Run( Args aArgs  )
  {
    string lArg = aArgs.Get("InputAudioFileList") ;

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
    string lWaveFilename = ExpandRelativeFilePath(aWaveFilename) ;

    var lSession = new Session( this.GetType().Name, aArgs, BaseFolder);

    DContext.Setup( lSession ) ;

    if ( File.Exists( lWaveFilename ) )
    {
      var lSource = new WaveFileSource(lWaveFilename) ;

      var lSignal = lSource.CreateSignal() ;

      var lPipeline = PipelineFactory.FromAudioToBits_ByTapCode().Then( PipelineFactory.FromBits() ) ;

      var lResult = Processor.Process(lSession.Name, lPipeline, lSignal);

      lResult.Save( lSession.CurrentOutputFolder )  ;
    }
    else
    {
      DContext.Error("Could not find audio file: [" + lWaveFilename + "]");
    }

    DContext.Shutdown(); 
  }

}
}
