using System.Collections.Generic;
using System.IO;

using DIGITC2_ENGINE ;

namespace DIGITC2 {

public sealed class FromAudio_ByTapCode_DirectLetters : DecodingTask
{ 
  public override void Run( Args aArgs  )
  {
    string lArg = aArgs.Get("Audio" ) ;

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
    DContext.Setup( new Session("FromAudio_" +  Path.GetFileNameWithoutExtension(aWaveFilename), aArgs, BaseFolder) ) ;

    if ( File.Exists( aWaveFilename ) )
    {
      var lSource = new WaveFileSource(aWaveFilename) ;

      Processor.FromAudio_ByCode_ToDirectLetters().Process( lSource.CreateSignal() ).Save() ;
    }
    else
    {
      DContext.Error("Could not find audio file: [" + aWaveFilename + "]");
    }

    DContext.Shutdown(); 
  }

}
}
