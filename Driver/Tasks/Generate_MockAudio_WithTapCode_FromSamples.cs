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


public sealed class Generate_MockAudio_WithTapCode_FromSamples : GeneratorTask
{
  public override void Run( Settings aSettings, List<Config> aConfigs )
  {
    DContext.Setup( new Session("Generate_MockAudio_WithTapCode_FromSamples", aSettings, BaseFolder) ) ;

    DContext.WriteLine("Generating MockAudio With TapCode From Samples");

    string lSourceText = aSettings.Get("MockAudio_WithTapCode_Text") ;
    if ( !string.IsNullOrEmpty( lSourceText ) ) 
    { 
      var lSource = MockWaveSource_WithTapCode_FromSamples.FromText(aSettings, lSourceText);  

      var lSignal = lSource.CreateSignal() as WaveSignal;

      string lOutputFile = aSettings.GetPath("MockAudio_WithTapCode_OutputFile");

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
      
