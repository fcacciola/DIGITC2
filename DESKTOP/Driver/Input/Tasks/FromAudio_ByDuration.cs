using System.Collections.Generic;
using System.IO;

using DIGITC2_ENGINE ;

namespace DIGITC2 {

public sealed class FromAudio_ByPulseDuration : DecodingTask
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
    DIGITC_Context.Setup( new Session("FromAudio_" +  Path.GetFileNameWithoutExtension(aWaveFilename), aArgs) ) ;

    if ( File.Exists( aWaveFilename ) )
    {
      var lSource = new WaveFileSource(aWaveFilename) ;

      Processor.FromAudioToBits_ByPulseDuration().Then( Processor.FromBits() ).Process( lSource.CreateSignal() ).Save() ;
    }
    else
    {
      DIGITC_Context.Error("Could not find audio file: [" + aWaveFilename + "]");
    }

    DIGITC_Context.Shutdown(); 
  }

}
}
