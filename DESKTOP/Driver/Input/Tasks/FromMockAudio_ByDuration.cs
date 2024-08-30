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


public sealed class FromMockAudio_ByDuration : DecodingTask
{
  public override void Run( Args aArgs )
  {
    DContext.Setup( new Session("FromMockAudio_ByDuration", aArgs, BaseFolder) ) ;

    DContext.WriteLine("From MockAudio ByDuration");

    //string lSourceText = File.ReadAllText( DIGITC_Context.Session.SampleFile( DIGITC_Context.Session.Args.Get("LargeText") ) );

    string lSourceText = "H W";

    var lSource = ByDuration_MockWaveSource.FromText(lSourceText);  

    Processor.FromAudioToBits_ByPulseDuration().Then( Processor.FromBits() ).Process( lSource.CreateSignal() ).Save() ;

    DContext.Shutdown(); 
  }
}


}
      
