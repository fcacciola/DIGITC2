using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2_Engine
{
  public abstract class SignalRenderer
  {
    public abstract void Render ( Signal aSignal ) ;
  }

  public class TextSignalRenderer : SignalRenderer
  {
    public override void Render ( Signal aSignal ) 
    {
      aSignal.Render ( this ) ;
    }


    public void Render ( string aText )
    {
      Console.WriteLine( aText );
    }
  }
}
