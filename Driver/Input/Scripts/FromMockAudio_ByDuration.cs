using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Microsoft.SqlServer.Server;

namespace DIGITC2 {


public class FromMockAudio_ByDuration
{
  public static void Run( Args aArgs )
  {
    Context.Setup( new Session("FromMockAudio_ByDuration", aArgs) ) ;

    Context.WriteLine("From MockAudio ByDuration");

    //string lSourceText = File.ReadAllText( Context.Session.SampleFile( Context.Session.Args.Get("LargeText") ) );

    string lSourceText = "H W";

    var lSource = ByDuration_MockWaveSource.FromText(lSourceText);  

    Processor.FromAudioToBits_ByPulseDuration().Then( Processor.FromBits() ).Process( lSource.CreateSignal() ).Save() ;

    Context.Shutdown(); 
  }
}


}
      
