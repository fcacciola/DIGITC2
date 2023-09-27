using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Office2010.CustomUI;
using DocumentFormat.OpenXml.Vml.Spreadsheet;

namespace DIGITC2
{
  public abstract class StateMonitor
  {
    public abstract void Watch ( string aName, StateValue aV, bool aCompact ) ;

    public abstract void Watch ( State aO ) ;

    public void Watch ( IWithState aO ) => Watch( aO?.GetState() ) ;
  }

  public class TraceStateMonitor : StateMonitor
  {
    public override void Watch ( string aName, StateValue aV, bool aCompact ) 
    {
      if ( aCompact )
      {
        Trace.Write( aV.Text ?? aName);
      }
      else
      {
        if ( aV != null )
             Trace.WriteLine( $"{aName}:{aV.Text}");
        else Trace.WriteLine(aName);
      }
    }

    public override void Watch ( State aO )
    {
      if ( aO.Name != null )
      {
        Watch(aO.Name,aO.Value,aO.IsCompact) ;
        Trace.Indent();
      }

      aO.Children.ForEach( x => Watch(x) );

      if ( !aO.IsCompact && aO.Children.Count > 0 && aO.Children.Last().IsCompact )
        Trace.WriteLine("");

      if ( aO.Name != null )
        Trace.Unindent();
    }
  }


}
