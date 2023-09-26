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
  public class StateValue
  {
    public StateValue ( string aText, object aData ) { Text = aText ; Data = aData ; }

    public static StateValue From( string   aV )                    => new StateValue( aV                              ,aV) ;
    public static StateValue From( bool     aV )                    => new StateValue( Textualizer.Textualize(aV)      ,aV) ;
    public static StateValue From( int      aV )                    => new StateValue( Textualizer.Textualize(aV)      ,aV) ;
    public static StateValue From( float    aV, string aFmt = "F2") => new StateValue( Textualizer.Textualize(aV, aFmt),aV) ;
    public static StateValue From( double   aV, string aFmt = "F2") => new StateValue( Textualizer.Textualize(aV, aFmt),aV) ;
    public static StateValue From( float[]  aV, string aFmt = "F2") => new StateValue( Textualizer.Textualize(aV, aFmt),aV) ;
    public static StateValue From( double[] aV, string aFmt = "F2") => new StateValue( Textualizer.Textualize(aV, aFmt),aV) ;

    public string Text ;
    public object Data ;

    public override string ToString() => Text;
  } 

  public class State
  {
    public State( string aName = null, StateValue aValue = null ) {  Name = aName ; Value = aValue ; }

    public static State With( string aName, string   aV )                    => new State( aName, StateValue.From(aV) ) ;
    public static State With( string aName, bool     aV )                    => new State( aName, StateValue.From(aV) ) ;
    public static State With( string aName, int      aV )                    => new State( aName, StateValue.From(aV) ) ;
    public static State With( string aName, float    aV, string aFmt = "F2") => new State( aName, StateValue.From(aV, aFmt) ) ;
    public static State With( string aName, double   aV, string aFmt = "F2") => new State( aName, StateValue.From(aV, aFmt) ) ;
    public static State With( string aName, float[]  aV, string aFmt = "F2") => new State( aName, StateValue.From(aV, aFmt) ) ;
    public static State With( string aName, double[] aV, string aFmt = "F2") => new State( aName, StateValue.From(aV, aFmt) ) ;

    public static State From( IWithState aO ) => aO.GetState() ;

    public static State From( string aName, IEnumerable<IWithState> aL )  
    {
      State rState = new State(aName);

      foreach( var lO in aL )
        rState.Add( lO.GetState() );

      return rState;

    }

    public string Name ;
    public StateValue Value ; 
    public List<State> Children = new List<State>();

    public void Add( State aChild ) { if ( aChild != null) Children.Add( aChild ) ; }

    public override string ToString() => $"({Name}:{Value})";
  }

  public abstract class StateMonitor
  {
    public abstract void Watch ( string aName, StateValue aV ) ;

    public abstract void Watch ( State aO ) ;

    public void Watch ( IWithState aO ) => Watch( aO?.GetState() ) ;
  }

  public class TraceStateMonitor : StateMonitor
  {
    public override void Watch ( string aName, StateValue aV ) 
    {
      if ( aV != null )
           Trace.WriteLine( $"{aName}:{aV.Text}");
      else Trace.WriteLine(aName);
    }

    public override void Watch ( State aO ) 
    {
      if ( aO.Name != null )
      {
        Watch(aO.Name,aO.Value) ;
        Trace.Indent();
      }
      aO.Children.ForEach( x => Watch(x));

      if ( aO.Name != null )
        Trace.Unindent();
    }
  }

  public interface IWithState
  {
    State GetState();
  }


}
