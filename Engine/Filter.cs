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
  using ByteStringsSignal  = LexicalSignal<TokenSymbol>;

  public abstract class Filter
  {
    public string ID { get ; set ; }

    public Step Apply( Step aInput ) 
    {
      if ( mStep == null )
        mStep = DoApply ( aInput );

      return mStep;
    }

    protected abstract Step DoApply( Step aInput) ;

    public override string ToString() => "Filter";

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

  public abstract class ByteStringFilter : Filter
  {
    protected ByteStringFilter() : base() {}

    protected override Step DoApply( Step aInput ) 
    {
      ByteStringsSignal lWordsSignal = aInput.Signal as ByteStringsSignal; 
      if ( lWordsSignal == null )
        throw new ArgumentException("Input Signal must be a Words Lexical Signal.");

      return Process(lWordsSignal, aInput);
    }
    
    protected abstract Step Process ( ByteStringsSignal aInput, Step aStep );  

  }

}
