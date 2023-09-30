using System;

namespace DIGITC2 {

public class FromRandomBits
{
  public static void Run( string[] aCmdLineArgs )
  {
    Context.Setup( new Session("FromRandomBits") ) ;

    Context.WriteLine("Random binary sequence");

    int lLen = aCmdLineArgs.Length > 1 ? Convert.ToInt32(aCmdLineArgs[1]) : 2560 ;

    var lSource = BitsSource.FromRandom(lLen);

    var lResult = Processor.FromBits().Process( lSource.CreateSignal() ) ;
   
    Context.Shutdown(); 
  }
}

}
      
