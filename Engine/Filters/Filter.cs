﻿using System;
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

    public List<Packet> Apply( Packet aInput ) 
    {
      List<Packet> rOutput = new List<Packet>();

      try
      {
        DoApply(aInput, rOutput);
      }
      catch ( Exception x )
      {
        if ( rOutput.Count > 0 ) 
          rOutput.First().ShouldQuit = true ;

        DContext.Error(x);
      }

      return rOutput; 
    }

    protected abstract void DoApply( Packet aInput, List<Packet> rOutput ) ;

    public abstract string Name { get; } 

    public override string ToString() => Name ;
  }

  public abstract class WaveFilter : Filter
  {
    protected WaveFilter() : base() {}

    protected override void DoApply( Packet aInput, List<Packet> rOuput )
    {
      WaveSignal lWaveSignal = aInput.Signal as WaveSignal; 
      if ( lWaveSignal == null )
        throw new ArgumentException("Input Signal must be an Audio Signal.");

      Process(lWaveSignal, aInput, rOuput);
    }
    
    protected abstract void Process ( WaveSignal aInput, Packet aInputPacket, List<Packet> rOuput );  

  }

  public abstract class LexicalFilter : Filter
  {
    protected LexicalFilter() : base() {}

    protected override void DoApply( Packet aInput, List<Packet> rOuput )
    {
      LexicalSignal lLexicalSignal = aInput.Signal as LexicalSignal; 
      if ( lLexicalSignal == null )
        throw new ArgumentException("Input Signal must be a gated Lexical Signal.");

      Process(lLexicalSignal, aInput, rOuput );
    }
    
    protected abstract void Process(LexicalSignal aInput, Packet aInputPacket, List<Packet> rOuput);  

  }


}
