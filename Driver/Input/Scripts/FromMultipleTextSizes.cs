using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Microsoft.SqlServer.Server;

namespace DIGITC2 {


public class FromMultipleTextSizes
{
  public static void Run( Args aArgs )
  {
    var lOuterSession = new Session("FromMultipleTextSizes", aArgs) ;

    var lAllWords = File.ReadAllText( lOuterSession.SampleFile(aArgs.Get("LargeText")) ).Split('\n','\r',' ');

    var lSlices = aArgs.Get("TextSlices").Split(',').Select( s => int.Parse(s) ).ToList();

    foreach( int lSlice in lSlices ) 
    { 
      List<string> lSublist = new List<string>();

      var lRNG = new Random();

      while ( lSublist.Count < lSlice )  
      { 
        int lIdx = lRNG.Next(0, lAllWords.Length) ;
      
        string lWord = lAllWords[lIdx]; 
        if ( !string.IsNullOrEmpty(lWord) )
          lSublist.Add(lWord  );
      }

      string lSourceText = string.Join(" ", lSublist.ToArray() );

      string lSliceSessionName = "FromMultipleTextSizes_Slice_" + lSlice;

      Context.Setup( new Session(lSliceSessionName, aArgs) ) ;

      Context.WriteLine("Text slice: " + lSlice);

      var lSource = BitsSource.FromText(lSourceText);  

      var lResult = Processor.FromBits().Process( lSource.CreateSignal() ) ;

      Context.Shutdown(); 
    }

  }
}


}
      
