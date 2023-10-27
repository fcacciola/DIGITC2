using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2
{
  public abstract class Filter : IWithState
  {
    public Step Apply( Step aInput ) 
    {
      if ( mStep == null )
        mStep = DoApply ( aInput );

      return mStep;
    }

    protected abstract Step DoApply( Step aInput) ;

    public State GetState()
    {
      State rS = new State("Filter",Name) ;
      UpdateState(rS) ;
      return rS ;
    }

    protected virtual void UpdateState( State rS ) {}

    protected abstract string Name { get; } 

    public override string ToString() => Name ;

    protected Step mStep ;
  }

  public abstract class WaveFilter : Filter
  {
    protected WaveFilter() : base() {}

    protected override Step DoApply( Step aInput ) 
    {
      WaveSignal lWaveSignal = aInput.Signal as WaveSignal; 
      if ( lWaveSignal == null )
        throw new ArgumentException("Input Signal must be an Audio Signal.");

      return Process(lWaveSignal, aInput);
    }
    
    protected abstract Step Process ( WaveSignal aInput, Step aInputStep );  

  }

  public abstract class LexicalFilter : Filter
  {
    protected LexicalFilter() : base() {}

    protected override Step DoApply( Step aInput ) 
    {
      LexicalSignal lLexicalSignal = aInput.Signal as LexicalSignal; 
      if ( lLexicalSignal == null )
        throw new ArgumentException("Input Signal must be a gated Lexical Signal.");

      return Process(lLexicalSignal, aInput);
    }
    
    protected abstract Step Process ( LexicalSignal aInput, Step aStep );  

  }


}
