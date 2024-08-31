using DIGITC2_ENGINE ;

namespace DIGITC2 {

public class BitsToTokens_Sample0 
{
  public static void Run( Args aArgs )
  {
    var lSourceText = "These are separate words";

    var lSource = BitsSource.FromText("BitsToTokens_Sample0", lSourceText);  

    DContext.Setup( new Session(lSource.Name, aArgs, Task.BaseFolder) ) ;

    DContext.WriteLine("BitsToTokens Sample 0 ");

    DContext.WriteLine("Source text: " + lSourceText);

    var lResult = Processor.FromBits().Process( lSource.CreateSignal() ) ;

    DContext.Shutdown(); 
  }
}

}

