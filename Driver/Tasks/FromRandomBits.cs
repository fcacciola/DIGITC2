using System;

using DIGITC2_ENGINE ;

namespace DIGITC2 {

public sealed class FromRandomBits : DecodingTask
{
  public override void Run( Args aArgs )
  {
    int lLen = aArgs.GetOptionalInt("BitsCount") ?? 512 ;

    var lSource = BitsSource.FromRandom("FromRandomBits",lLen);

    DContext.Setup( new Session(lSource.Name, aArgs, BaseFolder) ) ;

    DContext.WriteLine("Random binary sequence");

    var lResult = Processor.FromBits().Process( lSource.CreateSignal() ) ;
   
    DContext.Shutdown(); 
  }
}

}
      
