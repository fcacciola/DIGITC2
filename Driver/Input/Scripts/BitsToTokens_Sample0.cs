namespace DIGITC2 {

public class BitsToTokens_Sample0 
{
  public static void Run( Context aContext, string[] aCmdLineArgs )
  {
    aContext.Log("BitsToTokens Sample 0 ");

    int lBitsPerByteParam = 8 ;
    var lSourceText = "This are separate words";

    aContext.Log("Source text: " + lSourceText);

    var lSource = BitsSource.FromText(lSourceText);  

    var lProcessor = new Processor();

    lProcessor.Add( new BinaryToBytes( lBitsPerByteParam, true))
              .Add( new Tokenizer()) ;

    var lResult = lProcessor.Process( lSource.CreateSignal(), aContext ) ;
  }
}

}

