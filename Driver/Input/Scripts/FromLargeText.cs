using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Microsoft.SqlServer.Server;

namespace DIGITC2 {


public class FromLargeText
{
  public static void Run( Args aArgs )
  {
    Context.Setup( new Session("FromLargeText", aArgs) ) ;

    Context.WriteLine("From large text");

    string lSourceText = File.ReadAllText( Context.Session.SampleFile( Context.Session.Args.Get("LargeText") ) );

    var lSource = BitsSource.FromText(lSourceText);  

    Processor.FromBits().Process( lSource.CreateSignal() ).Save() ;

    Context.Shutdown(); 
  }
}


}
      
