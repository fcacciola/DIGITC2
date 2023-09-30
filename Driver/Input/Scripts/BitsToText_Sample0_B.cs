using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DIGITC2 {

public class BitsToText_Sample0_B
{
  public static void Run( string[] aCmdLineArgs )
  {
    Context.Setup( new Session("BitsToText_Sample0_B") ) ;

    Context.WriteLine("BitsToText from a given known text");

    int lBitsPerByteParam = 8 ;

    int lBaseSize = 10000 ;
    int lSize     = lBaseSize ;

    int lR = 2 ;

    List<string> lAll = new List<string>() ;
    do
    { 
       string lWord = $"{lSize}";      
       for ( int c = 0 ; c < lSize ; ++ c )  
       {
         lAll.Add( lWord ) ;
       }

       lSize = lBaseSize / lR ;
       lR ++ ;
    }
    while ( lSize > 1 ) ;
    
    string lSourceText = string.Join(" ", lAll.ToArray() );

    Context.WriteLine("Source text: " + lSourceText );

    var lSource = BitsSource.FromText(lSourceText);  

    var lProcessor = new Processor();

    lProcessor.Add( new BinaryToBytes( lBitsPerByteParam, true))
              .Add( new ScoreBytesAsLanguageDigits())
              .Add( new Tokenizer())
              .Add( new ScoreTokenLengthDistribution())
              .Add( new TokensToWords()) 
              .Add( new ScoreWordFrequencyDistribution());

    var lResult = lProcessor.Process( lSource.CreateSignal() ) ;

    Context.Shutdown(); 
  }
}
}
      
