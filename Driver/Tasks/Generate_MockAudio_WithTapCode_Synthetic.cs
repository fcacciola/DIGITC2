using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Microsoft.SqlServer.Server;

using DIGITC2_ENGINE ;

namespace DIGITC2 {


public sealed class Generate_MockAudio_WithTapCode_Synthetic : GeneratorTask
{
  public override void Run( Args aArgs )
  {
    DContext.Setup( new Session("Generate_MockAudio_WithTapCode_Synthetic", aArgs, BaseFolder) ) ;

    DContext.WriteLine("Generating MockAudio With TapCode Syntethically");

    string lSourceText = aArgs.Get("MockAudio_WithTapCode_Text") ;
    if ( !string.IsNullOrEmpty( lSourceText ) ) 
    { 
      var lSource = MockWaveSource_WithTapCode_Synthetic.FromText(aArgs, lSourceText);  

      var lSignal = lSource.CreateSignal() as WaveSignal;

      string lOutputFile_ = aArgs.Get("MockAudio_WithTapCode_OutputFile");

      string lOutputFile = ExpandRelativeFilePath(lOutputFile_);

      if ( !string.IsNullOrEmpty( lSourceText ) ) 
      {
        Utils.SetupFolderInFullPath(lOutputFile);

        Save(lSignal,lOutputFile);
      }
      else
      {
        DContext.Error("No output file specified in the configuration variable: MockAudio_WithTapCode_OutputFile");
      }

    }
    else
    {
      DContext.Error("No test specified in the configuration variable: MockAudio_WithTapCode_Text");
    }

    DContext.Shutdown(); 
  }
}


}
      
