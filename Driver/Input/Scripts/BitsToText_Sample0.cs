using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DIGITC2 {

public class BitsToText_Sample0
{
  public static void Run( Context aContext, string[] aCmdLineArgs )
  {
    aContext.Log("BitsToText from a given known text");

    int lBitsPerByteParam = 8 ;

    var lHugeWordList = File.ReadAllLines("./Input/Samples/words.txt");

    int lCount = 1000 ;

    List<string> lSublist = new List<string>();

    var lRNG = new Random();

    for( int c = 0 ; c < lCount ; ++ c )
    { 
      int lIdx = lRNG.Next(0, lHugeWordList.Length) ;
      
      lSublist.Add( lHugeWordList[lIdx] );
    }


    string lSourceText = string.Join(" ", lSublist.ToArray() );

    aContext.Log("Source text: " + lSourceText );

    var lSource = BitsSource.FromText(lSourceText);  

    var lProcessor = new Processor();

    lProcessor.Add( new BinaryToBytes( lBitsPerByteParam, true))
              .Add( new ScoreLexicalSignal())
              .Add( new Tokenizer())
              .Add( new ScoreLexicalSignal())
              .Add( new TokensToWords()) ;

    var lResult = lProcessor.Process( lSource.CreateSignal(), aContext ) ;
  }
}

}
      
