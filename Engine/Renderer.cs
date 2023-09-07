using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2_Engine
{
  public abstract class SignalRenderer
  {
    public abstract void Render ( Processor aProcessor, string aTitle = null ) ;
    public abstract void Render ( Signal aSignal, string aTitle = null ) ;
  }

  public class TextSignalRenderer : SignalRenderer
  {
    public override void Render ( Processor aProcessor , string aTitle = null ) 
    {
      if ( ! string.IsNullOrEmpty(aTitle) ) 
        Render(aTitle);
      aProcessor.Render ( this ) ;
    }

    public override void Render ( Signal aSignal , string aTitle = null ) 
    {
      if ( ! string.IsNullOrEmpty(aTitle) ) 
        Render(aTitle);
      aSignal.Render ( this ) ;
    }


    public void Render ( string aText )
    {
      Trace.WriteLine( aText );
    }
  }
}
