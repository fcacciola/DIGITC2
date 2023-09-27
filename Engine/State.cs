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
    public State( string aName = null, StateValue aValue = null, bool aIsCompact = false ) {  Name = aName ; Value = aValue ; IsCompact = aIsCompact ; }

    public static State With( string aName, string   aV                    , bool aUseCompactState = false ) => new State( aName, StateValue.From(aV)       , aUseCompactState ) ;
    public static State With( string aName, bool     aV                    , bool aUseCompactState = false ) => new State( aName, StateValue.From(aV)       , aUseCompactState ) ;
    public static State With( string aName, int      aV                    , bool aUseCompactState = false ) => new State( aName, StateValue.From(aV)       , aUseCompactState ) ;
    public static State With( string aName, float    aV, string aFmt = "F2", bool aUseCompactState = false ) => new State( aName, StateValue.From(aV, aFmt) , aUseCompactState ) ;
    public static State With( string aName, double   aV, string aFmt = "F2", bool aUseCompactState = false ) => new State( aName, StateValue.From(aV, aFmt) , aUseCompactState ) ;
    public static State With( string aName, float[]  aV, string aFmt = "F2", bool aUseCompactState = false ) => new State( aName, StateValue.From(aV, aFmt) , aUseCompactState ) ;
    public static State With( string aName, double[] aV, string aFmt = "F2", bool aUseCompactState = false ) => new State( aName, StateValue.From(aV, aFmt) , aUseCompactState ) ;

    public static State From( IWithState aO ) => aO.GetState() ;

    public static State From( string aName, IEnumerable<IWithState> aL, bool aUseCompactState = false )  
    {
      State rState = new State(aName);

      foreach( var lO in aL )
      {
        rState.Add(lO.GetState());
      }

      rState.IsCompact = aUseCompactState;  

      return rState;

    }

    public string Name ;
    public StateValue Value ; 
    public List<State> Children = new List<State>();
    public bool IsCompact ;

    public void Add( State aChild ) { if ( aChild != null) Children.Add( aChild ) ; }

    public override string ToString() => $"({Name}:{Value})";
  }

  public interface IWithState
  {
    State GetState();
  }


}
