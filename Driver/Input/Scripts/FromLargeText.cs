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

    string lData = "dracula.txt" ;

    var lHugeWordList = File.ReadAllText( Context.Session.SampleFile(lData) ).Split('\n','\r',' ');

    int lCount = aArgs.GetOptionalInt("TextSlice") ?? lHugeWordList.Length ;

    List<string> lSublist = new List<string>();

    var lRNG = new Random();

    while ( lSublist.Count < Math.Min(lCount,lHugeWordList.Length) )  
    { 
      int lIdx = lRNG.Next(0, lHugeWordList.Length) ;
      
      string lWord = lHugeWordList[lIdx]; 
      if ( !string.IsNullOrEmpty(lWord) )
        lSublist.Add(lWord  );
    }

    string lSourceText = string.Join(" ", lSublist.ToArray() );

    Context.WriteLine("Source text: " + lSourceText );

    var lSource = BitsSource.FromText(lSourceText);  

    var lResult = Processor.FromBits().Process( lSource.CreateSignal() ) ;

    Context.Shutdown(); 
  }
}


}
      
