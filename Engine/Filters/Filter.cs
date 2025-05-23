using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2_ENGINE
{
  public abstract class Filter 
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
          if ( ! lBranch.ShouldQuit )
          {
            DContext.Session.PushFolder( $"{Name}_{lBranch.Name}");

            try
            {
              DoApply(lBranch, lOutput);
            }
            catch ( Exception x )
            {
              lBranch.ShouldQuit = true ;
              DContext.Error(x);
            }

            DContext.Session.PopFolder();
          }
        }

        mStep = new Step(this,lOutput); 
      }

      return mStep;
    }

    protected abstract void DoApply( Branch aInput, List<Branch> rOutput ) ;

    public abstract string Name { get; } 

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
