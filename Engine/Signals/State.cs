namespace DIGITC2_ENGINE
{
  public class StateValue
  {
    public StateValue ( string aText, object aData ) { Text = aText ; Data = aData ; }

    public static StateValue From( string   aV )                    => new StateValue( aV                                   ,aV) ;
    public static StateValue From( bool     aV )                    => new StateValue( Textualizer.Textualize     (aV      ),aV) ;
    public static StateValue From( int      aV )                    => new StateValue( Textualizer.Textualize     (aV      ),aV) ;
    public static StateValue From( Enum     aV )                    => new StateValue( Textualizer.Textualize     (aV      ),aV) ;
    public static StateValue From( float    aV, string aFmt = "F2") => new StateValue( Textualizer.Textualize     (aV, aFmt),aV) ;
    public static StateValue From( double   aV, string aFmt = "F2") => new StateValue( Textualizer.Textualize     (aV, aFmt),aV) ;
    public static StateValue From( int[]    aV )                    => new StateValue( Textualizer.TextualizeArray(aV      ),aV) ;
    public static StateValue From( float[]  aV, string aFmt = "F2") => new StateValue( Textualizer.TextualizeArray(aV, aFmt),aV) ;
    public static StateValue From( double[] aV, string aFmt = "F2") => new StateValue( Textualizer.TextualizeArray(aV, aFmt),aV) ;

    public string Text ;
    public object Data ;

    public override string ToString() => Text;
  } 

  public class State
  {
    public State( string aType, string aName = null, StateValue aValue = null )
    { 
      Type  = aType;
      Name  = aName ; 
      Value = aValue ; 
    }

    public static State With( string aName, string   aV                    ) => new State(null, aName, StateValue.From(aV)       ) ;
    public static State With( string aName, bool     aV                    ) => new State(null, aName, StateValue.From(aV)       ) ;
    public static State With( string aName, int      aV                    ) => new State(null, aName, StateValue.From(aV)       ) ;
    public static State With( string aName, Enum     aV                    ) => new State(null, aName, StateValue.From(aV)       ) ;
    public static State With( string aName, float    aV, string aFmt = "F2") => new State(null, aName, StateValue.From(aV, aFmt) ) ;
    public static State With( string aName, double   aV, string aFmt = "F2") => new State(null, aName, StateValue.From(aV, aFmt) ) ;
    public static State With( string aName, int[]    aV                    ) => new State(null, aName, StateValue.From(aV) ) ;
    public static State With( string aName, float[]  aV, string aFmt = "F2") => new State(null, aName, StateValue.From(aV, aFmt) ) ;
    public static State With( string aName, double[] aV, string aFmt = "F2") => new State(null, aName, StateValue.From(aV, aFmt) ) ;

    public static State From( IWithState aO ) => aO.GetState() ;

    public static State From( string aType, string aName, IEnumerable<IWithState> aL )  
    {
      State rState = new State(aType,aName);

      foreach( var lO in aL )
      {
        rState.Add(lO.GetState());
      }

      rState.IsArray = true ;

      return rState;
   }

    public string      Type ;
    public string      Name ;
    public StateValue  Value ; 
    public bool        IsArray ;
    public List<State> Children = new List<State>();

    public void Add( State aChild ) { if ( aChild != null) Children.Add( aChild ) ; }

    public override string ToString() => $"({Type}|{Name}:{Value})";
  }

  public interface IWithState
  {
    State GetState();
  }


}
