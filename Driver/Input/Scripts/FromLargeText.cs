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
  public static void Run( string[] aCmdLineArgs )
  {
    Context.Setup( new Session("FromLargeText") ) ;

    Context.WriteLine("From large text");

    string lData = "dracula.txt" ;

    var lHugeWordList = File.ReadAllLines( Context.Session.SampleFile(lData) );

    int lCount = lHugeWordList.Length ;

    List<string> lSublist = new List<string>();

    var lRNG = new Random();

    for( int c = 0 ; c < Math.Min(lCount,lHugeWordList.Length) ; ++ c )
    { 
      int lIdx = lRNG.Next(0, lHugeWordList.Length) ;
      
      lSublist.Add( lHugeWordList[lIdx] );
    }

    string lSourceText = string.Join(" ", lSublist.ToArray() );

    Context.WriteLine("Source text: " + lSourceText );

    var lSource = BitsSource.FromText(lSourceText);  

    var lResult = Processor.FromBits().Process( lSource.CreateSignal() ) ;

    Context.Shutdown(); 
  }
}


}
      
