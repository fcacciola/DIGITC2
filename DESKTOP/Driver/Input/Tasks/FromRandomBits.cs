using System;

using DIGITC2_ENGINE ;

namespace DIGITC2 {

public sealed class FromRandomBits : DecodingTask
{
  public override void Run( Args aArgs )
  {
    DIGITC_Context.Setup( new Session("FromRandomBits", aArgs) ) ;

    DIGITC_Context.WriteLine("Random binary sequence");

    int lLen = aArgs.GetOptionalInt("BitsCount") ?? 512 ;

    var lSource = BitsSource.FromRandom(lLen);

    var lResult = Processor.FromBits().Process( lSource.CreateSignal() ) ;
   
    DIGITC_Context.Shutdown(); 
  }
}

}
      
