namespace DIGITC2 {

public class BitsToTokens_Sample0 
{
  public static void Run( Args aArgs )
  {
    Context.Setup( new Session("BitsToTokens_Sample0") ) ;

    Context.WriteLine("BitsToTokens Sample 0 ");

    var lSourceText = "These are separate words";

    Context.WriteLine("Source text: " + lSourceText);

    var lSource = BitsSource.FromText(lSourceText);  

    var lProcessor = new Processor();

    var lResult = Processor.FromBits().Process( lSource.CreateSignal() ) ;

    Context.Shutdown(); 
  }
}

}

