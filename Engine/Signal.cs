using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Signals;

namespace DIGITC2
{
  using BitsSignal  = GenericLexicalSignal<BitSymbol>;
  using BytesSignal = GenericLexicalSignal<ByteSymbol>;
  using TextSignal  = GenericLexicalSignal<TextSymbol>;

  public abstract class Signal
  {
    public Source Source { get ; set ; }

    public abstract void Render ( TextRenderer aRenderer, RenderOptions aOptions );

    public List<Signal> BranchOut ( int aBranchCount )
    {
      List<Signal> rBranches = new List<Signal>();

      for( int i = 0; i < aBranchCount ; ++ i )
        rBranches.Add( Copy() ) ;

      return rBranches;
    }

    public abstract Signal Copy() ;

    public void Assign( Signal aRHS )
    {
      StepIdx  = aRHS.StepIdx  ;
      SliceIdx = aRHS.SliceIdx ; 
      Name     = aRHS.Name ;
    }

    public override string ToString()
    {
      return $"(Name:{Name}. StepIdx:{StepIdx} SliceIdx:{SliceIdx})";
    }

    public virtual List<Signal> Slice( Context aContext = null ) { return new List<Signal>{this} ;}

    public virtual Signal MergeWith( IEnumerable<Signal> aSlices, Context aContext = null ) {  return this ; }

    public int    StepIdx  = 0 ;

    public int    SliceIdx = 0 ;
    public string Name     = "";
  }

  //public class SignalArray : Signal
  //{
  //  public SignalArray( IEnumerable<Signal> aUnits )
  //  {
  //    Units.AddRange( aUnits ) ;

  //    Source = aUnits.First().Source ;
  //  }

  //  public override void Render ( TextRenderer aRenderer, RenderOptions aOptions  )
  //  {
  //    Units.ForEach( u => u.Render ( aRenderer, aOptions ) );
  //  }

  //  public override Signal Copy()
  //  {
  //    SignalArray rCopy = new SignalArray( Units.Select( u => u.Copy() ) ) ;  

  //    return rCopy;
  //  }

  //  public override string ToString() => $"(Array of {Units.Count} signals)" ; 

  //  public List<Signal> Units = new List<Signal>(); 
  //}

  }
