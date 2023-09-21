namespace DIGITC2 {

public class BitsToText_Sample0
{
  public static void Run( Context aContext, string[] aCmdLineArgs )
  {
    aContext.Log("BitsToText from a given known text");

    int lBitsPerByteParam = 8 ;

    string lSourceText = "Hello World!";

    aContext.Log("Source text: " + lSourceText );

    var lSource = BitsSource.FromText(lSourceText);  

    var lProcessor = new Processor();

    lProcessor.Add( new BinaryToBytes( lBitsPerByteParam, true))
              .Add( new Tokenizer())
              .Add( new TokensToWords()) ;

    var lResult = lProcessor.Process( lSource.CreateSignal(), aContext ) ;
  }
}

}
      
