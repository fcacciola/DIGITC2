using System;

namespace DIGITC2 {

public class BitsToText_Sample1
{
  public static void Run( string[] aCmdLineArgs )
  {
    Context.Setup( @".\DIGITC2_Output.txt") ;

    Context.WriteLine("BitsToText from a random binary sequence");

    int lBitsPerByteParam = 8 ;
    int lLen = aCmdLineArgs.Length > 1 ? Convert.ToInt32(aCmdLineArgs[1]) : 5120 ;

    var lSource = BitsSource.FromRandom(lLen);

    var lProcessor = new Processor();

    lProcessor.Add( new BinaryToBytes( lBitsPerByteParam, true))
              .Add( new ScoreBytesAsLanguageDigits())
              .Add( new Tokenizer())
              .Add( new ScoreTokenLengthDistribution())
              .Add( new TokensToWords()) 
              .Add( new ScoreWordLengthDistribution());

    var lResult = lProcessor.Process( lSource.CreateSignal() ) ;
   
    Context.Shutdown(); 
  }
}

}
      
