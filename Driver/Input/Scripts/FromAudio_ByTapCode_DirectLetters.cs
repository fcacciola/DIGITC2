using System.Collections.Generic;
using System.IO;

namespace DIGITC2 {

public class FromAudio_ByTapCode_DirectLetters
{ 
  public static void Run( Args aArgs  )
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

  static void RunWithFile( Args aArgs, string aWaveFilename  )
  {
    Context.Setup( new Session("FromAudio_" +  Path.GetFileNameWithoutExtension(aWaveFilename), aArgs) ) ;

    if ( File.Exists( aWaveFilename ) )
    {
      var lSource = new WaveFileSource(aWaveFilename) ;

      Processor.FromAudio_ByCode_ToDirectLetters().Process( lSource.CreateSignal() ).Save() ;
    }
    else
    {
      Context.Error("Could not find audio file: [" + aWaveFilename + "]");
    }

    Context.Shutdown(); 
  }

}
}
