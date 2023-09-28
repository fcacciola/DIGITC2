using System;

namespace DIGITC2 {

public class BitsToText_Sample1
{
  public static void Run( Context aContext, string[] aCmdLineArgs )
  {
    aContext.Log("BitsToText from a random binary sequence");

    int lBitsPerByteParam = 8 ;
    int lLen = aCmdLineArgs.Length > 1 ? Convert.ToInt32(aCmdLineArgs[1]) : 1024 ;

    var lSource = BitsSource.FromRandom(lLen);

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
      
