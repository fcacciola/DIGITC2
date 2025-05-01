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


public sealed class FromLargeText : DecodingTask
{
  public override void Run( Args aArgs )
  {
    string lSourceText = File.ReadAllText( DContext.Session.Args.Get("LargeText") );

    var lSource = BitsSource.FromText("FromLargeText",lSourceText);  

    DContext.Setup( new Session(lSource.Name, aArgs, BaseFolder) ) ;

    DContext.WriteLine("From large text");

    Processor.FromBits().Process( lSource.CreateSignal() ).Save() ;

    DContext.Shutdown(); 
  }
}


}
      
