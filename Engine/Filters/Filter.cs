using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2_ENGINE
{
  public abstract class Filter 
  {
    public virtual void Setup  ( Config aConfig ) {}
    public virtual void Cleanup( Config aConfig ) {}

    public (Packet,List<Config>) Apply( Config aConfig, Packet aInput ) 
    {
      Packet rPacket = null ;

      List<Config> rBranches = new List<Config>();

      try
      {
        rPacket = DoApply(aConfig, aInput, rBranches);
      }
      catch ( Exception x )
      {
        DContext.Error(x);
      }

      return (rPacket,rBranches); 
    }

    protected abstract Packet DoApply( Config aConfig, Packet aInput, List<Config> rBranches ) ;

    public abstract string Name { get; } 

    public override string ToString() => Name ;
  }

  public abstract class WaveFilter : Filter
  {
    protected WaveFilter() : base() {}

    protected override Packet DoApply( Config aConfig, Packet aInput, List<Config> rBranches )
    {
      WaveSignal lWaveSignal = aInput.Signal as WaveSignal; 
      if ( lWaveSignal == null )
        throw new ArgumentException("Input Signal must be an Audio Signal.");

      return Process(lWaveSignal, aConfig, aInput, rBranches);
    }
    
    protected abstract Packet Process ( WaveSignal aInput, Config aConfig, Packet aInputPacket, List<Config> rBranches  );  

  }

  public abstract class LexicalFilter : Filter
  {
    protected LexicalFilter() : base() {}

    protected override Packet DoApply( Config aConfig, Packet aInput, List<Config> rBranches )
    {
      LexicalSignal lLexicalSignal = aInput.Signal as LexicalSignal; 
      if ( lLexicalSignal == null )
        throw new ArgumentException("Input Signal must be a gated Lexical Signal.");

      return Process(lLexicalSignal, aConfig, aInput, rBranches );
    }
    
    protected abstract Packet Process ( LexicalSignal aInput, Config aConfig, Packet aInputPacket, List<Config> rBranches  );  

  }


}
