using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2_ENGINE
{
  public abstract class Filter : IWithState
  {
    public virtual void Setup() {}
    public virtual void Cleanup() {}

    public Step Apply( Step aInput ) 
    {
      if ( mStep == null )
      {
        List<Branch> lOutput = new List<Branch>();

        foreach ( Branch lBranch in aInput.Branches ) 
        {
          if ( ! lBranch.Quit )
          {
            try
            {
              DoApply(lBranch, lOutput);
            }
            catch ( Exception x )
            {
              lBranch.Quit = true ;
              DContext.Error(x);
            }
          }
        }

        mStep = new Step(this,lOutput); 
      }

      return mStep;
    }

    protected abstract void DoApply( Branch aInput, List<Branch> rOutput ) ;

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

    protected override void DoApply( Branch aInput, List<Branch> rOuput )
    {
      WaveSignal lWaveSignal = aInput.Signal as WaveSignal; 
      if ( lWaveSignal == null )
        throw new ArgumentException("Input Signal must be an Audio Signal.");

      Process(lWaveSignal, aInput, rOuput);
    }
    
    protected abstract void Process ( WaveSignal aInput, Branch aInputBranch, List<Branch> rOuput );  

  }

  public abstract class LexicalFilter : Filter
  {
    protected LexicalFilter() : base() {}

    protected override void DoApply( Branch aInput, List<Branch> rOuput )
    {
      LexicalSignal lLexicalSignal = aInput.Signal as LexicalSignal; 
      if ( lLexicalSignal == null )
        throw new ArgumentException("Input Signal must be a gated Lexical Signal.");

      Process(lLexicalSignal, aInput, rOuput );
    }
    
    protected abstract void Process(LexicalSignal aInput, Branch aInputBranch, List<Branch> rOuput);  

  }


}
