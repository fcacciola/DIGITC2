using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Signals;

namespace DIGITC2
{
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

    public int    StepIdx  = 0 ;
    public int    SliceIdx = 0 ;
    public string Name     = "";
  }

  public class SignalArray : Signal
  {
    public SignalArray( IEnumerable<Signal> aUnits )
    {
      Units.AddRange( aUnits ) ;

      Source = aUnits.First().Source ;
    }

    public override void Render ( TextRenderer aRenderer, RenderOptions aOptions  )
    {
      Units.ForEach( u => u.Render ( aRenderer, aOptions ) );
    }

    public override Signal Copy()
    {
      SignalArray rCopy = new SignalArray( Units.Select( u => u.Copy() ) ) ;  

      return rCopy;
    }

    public override string ToString() => $"(Array of {Units.Count} signals)" ; 

    public List<Signal> Units = new List<Signal>(); 
  }

  public class WaveSignal : Signal
  {
    public WaveSignal( DiscreteSignal aRep ) : base()
    { 
      Rep = aRep ;
    }

    public DiscreteSignal Rep ;
    
    public double  Duration     => Rep.Duration ;
    public int     SamplingRate => Rep.SamplingRate ;
    public float[] Samples      => Rep.Samples ; 

    public WaveSignal CopyWith( DiscreteSignal aDS )
    {
      WaveSignal rCopy = new WaveSignal(aDS);
      rCopy.Assign(this); 
      return rCopy ;
    }

    public override Signal Copy() => CopyWith(Rep.Copy()); 
    
    public override void Render ( TextRenderer aRenderer, RenderOptions aOptions )
    {
      aRenderer.Render ( ToString(), aOptions );
    }

    public override string ToString()
    {
      return $"[{base.ToString()} Duration:{Rep.Duration:F2} seconds. SampleRate:{Rep.SamplingRate} Samples:[{Utils.ToStr(Rep.Samples)}]";
    }

  }
}
