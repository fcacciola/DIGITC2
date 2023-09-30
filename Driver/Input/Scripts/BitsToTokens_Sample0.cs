namespace DIGITC2 {

public class BitsToTokens_Sample0 
{
  public static void Run( string[] aCmdLineArgs )
  {
    Context.Setup( new Session("BitsToTokens_Sample0") ) ;

    Context.WriteLine("BitsToTokens Sample 0 ");

    int lBitsPerByteParam = 8 ;
    var lSourceText = "These are separate words";

    Context.WriteLine("Source text: " + lSourceText);

    var lSource = BitsSource.FromText(lSourceText);  

    var lProcessor = new Processor();

    lProcessor.Add( new BinaryToBytes( lBitsPerByteParam, true))
              .Add( new ScoreBytesAsLanguageDigits())
              .Add( new Tokenizer())
              .Add( new ScoreTokenLengthDistribution());

    var lResult = lProcessor.Process( lSource.CreateSignal() ) ;

    Context.Shutdown(); 
  }
}

}

