using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2
{
  using GatedLexicalSignal = LexicalSignal<GatedSymbol>;
  using BitsSignal         = LexicalSignal<BitSymbol>;
  using BytesSignal        = LexicalSignal<ByteSymbol>;
  using TokensSignal       = LexicalSignal<TokenSymbol>;
  using WordsSignal        = LexicalSignal<WordSymbol>;

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
      State rS = new State() ;
      UpdateState(rS) ;
      return rS ;
    }

    protected virtual void UpdateState( State rS ) {}

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

  public abstract class GatedLexicalFilter : Filter
  {
    protected GatedLexicalFilter() : base() {}

    protected override Step DoApply( Step aInput ) 
    {
      GatedLexicalSignal lLexicalSignal = aInput.Signal as GatedLexicalSignal; 
      if ( lLexicalSignal == null )
        throw new ArgumentException("Input Signal must be a gated Lexical Signal.");

      return Process(lLexicalSignal, aInput);
    }
    
    protected abstract Step Process ( GatedLexicalSignal aInput, Step aStep );  

  }

  public abstract class BitsFilter : Filter
  {
    protected BitsFilter() : base() {}

    protected override Step DoApply( Step aInput ) 
    {
      BitsSignal lBitsSignal = aInput.Signal as BitsSignal; 
      if ( lBitsSignal == null )
        throw new ArgumentException("Input Signal must be a Bits Lexical Signal.");

      return Process(lBitsSignal, aInput);
    }
    
    protected abstract Step Process ( BitsSignal aInput, Step aStep );  

  }

  public abstract class BytesFilter : Filter
  {
    protected BytesFilter() : base() {}

    protected override Step DoApply( Step aInput ) 
    {
      BytesSignal lBytesSignal = aInput.Signal as BytesSignal; 
      if ( lBytesSignal == null )
        throw new ArgumentException("Input Signal must be a Bytes Lexical Signal.");

      return Process(lBytesSignal, aInput);
    }
    
    protected abstract Step Process ( BytesSignal aInput, Step aStep );  

  }

  public abstract class TokensFilter : Filter
  {
    protected TokensFilter() : base() {}

    protected override Step DoApply( Step aInput ) 
    {
      TokensSignal lTokensSignal = aInput.Signal as TokensSignal; 
      if ( lTokensSignal == null )
        throw new ArgumentException("Input Signal must be a Tokens Lexical Signal.");

      return Process(lTokensSignal, aInput);
    }
    
    protected abstract Step Process ( TokensSignal aInput, Step aStep );  

  }

  public abstract class WordsFilter : Filter
  {
    protected WordsFilter() : base() {}

    protected override Step DoApply( Step aInput ) 
    {
      WordsSignal lTokensSignal = aInput.Signal as WordsSignal; 
      if ( lTokensSignal == null )
        throw new ArgumentException("Input Signal must be a Words Lexical Signal.");

      return Process(lTokensSignal, aInput);
    }
    
    protected abstract Step Process ( WordsSignal aInput, Step aStep );  

  }


}
