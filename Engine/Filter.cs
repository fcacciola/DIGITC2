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

    public Signal Apply( Signal aInput, Context aContext ) 
    {
      if ( mResult == null )
        mResult = DoApply ( aInput, aContext );

      return mResult;
    }

    protected abstract Signal DoApply( Signal aInput, Context aContext ) ;

    protected Signal mResult ;

  }
  public abstract class WaveFilter : Filter
  {
    protected WaveFilter() : base() {}

    protected override Signal DoApply( Signal aInput, Context aContext ) 
    {
      WaveSignal lWaveSignal = aInput as WaveSignal; 
      if ( lWaveSignal == null )
        throw new ArgumentException("Input Signal must be an Audio Signal.");

      return Process(lWaveSignal, aContext);
    }
    
    protected abstract Signal Process ( WaveSignal aInput, Context aContext );  

  }

  public abstract class GatedLexicalFilter : Filter
  {
    protected GatedLexicalFilter() : base() {}

    protected override Signal DoApply( Signal aInput, Context aContext ) 
    {
      GatedLexicalSignal lLexicalSignal = aInput as GatedLexicalSignal; 
      if ( lLexicalSignal == null )
        throw new ArgumentException("Input Signal must be a gated Lexical Signal.");

      return Process(lLexicalSignal, aContext);
    }
    
    protected abstract Signal Process ( GatedLexicalSignal aInput, Context aContext );  

  }

  public abstract class BitsFilter : Filter
  {
    protected BitsFilter() : base() {}

    protected override Signal DoApply( Signal aInput, Context aContext ) 
    {
      BitsSignal lBitsSignal = aInput as BitsSignal; 
      if ( lBitsSignal == null )
        throw new ArgumentException("Input Signal must be a Bits Lexical Signal.");

      return Process(lBitsSignal, aContext);
    }
    
    protected abstract Signal Process ( BitsSignal aInput, Context aContext );  

  }

  public abstract class BytesFilter : Filter
  {
    protected BytesFilter() : base() {}

    protected override Signal DoApply( Signal aInput, Context aContext ) 
    {
      BytesSignal lBytesSignal = aInput as BytesSignal; 
      if ( lBytesSignal == null )
        throw new ArgumentException("Input Signal must be a Bytes Lexical Signal.");

      return Process(lBytesSignal, aContext);
    }
    
    protected abstract Signal Process ( BytesSignal aInput, Context aContext );  

  }

  public abstract class ByteStringFilter : Filter
  {
    protected ByteStringFilter() : base() {}

    protected override Signal DoApply( Signal aInput, Context aContext ) 
    {
      ByteStringsSignal lWordsSignal = aInput as ByteStringsSignal; 
      if ( lWordsSignal == null )
        throw new ArgumentException("Input Signal must be a Words Lexical Signal.");

      return Process(lWordsSignal, aContext);
    }
    
    protected abstract Signal Process ( ByteStringsSignal aInput, Context aContext );  

  }

}
