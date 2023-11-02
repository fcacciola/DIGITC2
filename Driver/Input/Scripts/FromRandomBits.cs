using System;

namespace DIGITC2 {

public class FromRandomBits
{
  public static void Run( Args aArgs )
  {
    Context.Setup( new Session("FromRandomBits", aArgs) ) ;

    Context.WriteLine("Random binary sequence");

    int lLen = aArgs.GetOptionalInt("BitsCount") ?? 512 ;

    var lSource = BitsSource.FromRandom(lLen);

    var lResult = Processor.FromBits().Process( lSource.CreateSignal() ) ;
   
    Context.Shutdown(); 
  }
}

}
      
