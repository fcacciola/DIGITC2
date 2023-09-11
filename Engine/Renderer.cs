using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2
{
  public class RenderOptions
  {

  }

  public abstract class Renderer
  {
    public abstract void Render ( Processor aProcessor, RenderOptions aOptions, string aTitle = null ) ;
    public abstract void Render ( Source    aSource   , RenderOptions aOptions, string aTitle = null ) ;
    public abstract void Render ( Filter    aFilter   , RenderOptions aOptions, string aTitle = null ) ;
    public abstract void Render ( Signal    aSignal   , RenderOptions aOptions, string aTitle = null ) ;
    public abstract void Render ( string    aText     , RenderOptions aOptions ) ;
  }

  public class TextRenderer : Renderer
  {
    public override void Render ( Processor aProcessor, RenderOptions aOptions, string aTitle = null ) 
    {
      if ( ! string.IsNullOrEmpty(aTitle) ) 
        Trace.WriteLine( $"{aTitle}: ");
      Trace.Indent();
      aProcessor.Render ( this, aOptions  ) ;
      Trace.Unindent();
    }

    public override void Render ( Source aSource, RenderOptions aOptions, string aTitle = null ) 
    {
      if ( ! string.IsNullOrEmpty(aTitle) ) 
        Trace.WriteLine( $"{aTitle}: ");
      Trace.Indent();
      aSource.Render ( this, aOptions  ) ;
      Trace.Unindent();
    }

    public override void Render ( Filter aFilter, RenderOptions aOptions, string aTitle = null ) 
    {
      if ( ! string.IsNullOrEmpty(aTitle) ) 
        Trace.WriteLine( $"{aTitle}: ");
      Trace.Indent();
      aFilter.Render ( this, aOptions  ) ;
      Trace.Unindent();
    }

    public override void Render ( Signal aSignal, RenderOptions aOptions, string aTitle = null ) 
    {
      if ( ! string.IsNullOrEmpty(aTitle) ) 
        Trace.WriteLine( $"{aTitle}: ");
      Trace.Indent();
      aSignal.Render ( this, aOptions ) ;
      Trace.Unindent();
    }

    public override void Render ( string aText, RenderOptions aOptions ) 
    {
      Trace.WriteLine( aText );
    }
  }
}
