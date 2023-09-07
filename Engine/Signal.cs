using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2_Engine
{
  public abstract class Signal
  {
    public Source Source { get ; set ; }

    public abstract void Render ( TextSignalRenderer aRenderer );

    public List<Signal> BranchOut ( int aBranchCount )
    {
      List<Signal> rBranches = new List<Signal>();

      for( int i = 0; i < aBranchCount ; ++ i )
        rBranches.Add( Copy() ) ;

      return rBranches;
    }

    public abstract Signal Copy() ;
  }

  public class SignalArray : Signal
  {
    public SignalArray( IEnumerable<Signal> aUnits )
    {
      Units.AddRange( aUnits ) ;

      Source = aUnits.First().Source ;
    }

    public override void Render ( TextSignalRenderer aRenderer )
    {
      Units.ForEach( u => u.Render ( aRenderer ) );
    }

    public override Signal Copy()
    {
      SignalArray rCopy = new SignalArray( Units.Select( u => u.Copy() ) ) ;  

      return rCopy;
    }

    public override string ToString() => $"(Array of {Units.Count} signals)" ; 

    public List<Signal> Units = new List<Signal>(); 
  }

  public class TrivialSignal : Signal 
  { 
    public TrivialSignal( params string[] aData ) { Data.AddRange(aData) ; }  

    public TrivialSignal( List<string> aData ) { Data = aData ; }  

    public override void Render ( TextSignalRenderer aRenderer ) 
    {
      aRenderer.Render ( string.Join( " | ", Data ) ) ;
    }

    public override Signal Copy()
    {
      List<string> lDataCopy = new List<string>() ; 
      Data.ForEach( s => lDataCopy.Add( s ) ) ;

      return new TrivialSignal(lDataCopy) ;
    }

    public override string ToString() => string.Join(" | ", Data) ; 

    public List<string> Data = new List<string>() ;
  }
}
