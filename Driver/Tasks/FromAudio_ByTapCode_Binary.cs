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

    DContext.Setup( new Session("FromAudio_ByTapCode_Binary_using_file_" +  Path.GetFileNameWithoutExtension(lWaveFilename), aArgs, BaseFolder) ) ;

    if ( File.Exists( lWaveFilename ) )
    {
      var lSource = new WaveFileSource(lWaveFilename) ;

      Processor.FromAudioToBits_ByTapCode().Then( Processor.FromBits() ).Process( lSource.CreateSignal() ).Save() ;
    }
    else
    {
      DContext.Error("Could not find audio file: [" + lWaveFilename + "]");
    }

    DContext.Shutdown(); 
  }

}
}
