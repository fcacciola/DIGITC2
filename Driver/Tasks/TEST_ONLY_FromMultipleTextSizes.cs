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


public sealed class TEST_ONLY_FromMultipleTextSizes : DecodingTask
{
  public override void Run( Args aArgs )
  {
    var lAllWords = File.ReadAllText( aArgs.Get("LargeText") ).Split('\n','\r',' ');

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

      var lSource = BitsSource.FromText(lSliceSessionName, lSourceText);  

      DContext.Setup( new Session(lSliceSessionName, aArgs, BaseFolder) ) ;

      DContext.WriteLine("Text slice: " + lSlice);

      Processor.FromBits().Process( lSource.CreateSignal() ).Save() ;

      DContext.Shutdown(); 
    }

  }
}


}
      
