using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2_Engine
{
  public abstract class Signal
  {
    public abstract void Render ( TextSignalRenderer renderer );
  }

  public class TrivialSignal : Signal 
  { 
    public TrivialSignal( string aD ) { Data = aD ; }  

    public override void Render ( TextSignalRenderer aRenderer ) 
    {
      aRenderer.Render ( Data );
    }

    public string Data = "" ;
  }
}
