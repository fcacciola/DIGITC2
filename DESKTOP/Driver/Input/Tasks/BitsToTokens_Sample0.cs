using DIGITC2_ENGINE ;

namespace DIGITC2 {

public class BitsToTokens_Sample0 
{
  public static void Run( Args aArgs )
  {
    DIGITC_Context.Setup( new Session("BitsToTokens_Sample0", aArgs) ) ;

    DIGITC_Context.WriteLine("BitsToTokens Sample 0 ");

    var lSourceText = "These are separate words";

    DIGITC_Context.WriteLine("Source text: " + lSourceText);

    var lSource = BitsSource.FromText(lSourceText);  

    var lResult = Processor.FromBits().Process( lSource.CreateSignal() ) ;

    DIGITC_Context.Shutdown(); 
  }
}

}

