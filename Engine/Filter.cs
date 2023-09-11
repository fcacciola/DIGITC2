using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2
{
  using BitsSignal  = GenericLexicalSignal<BitSymbol>;
  using BytesSignal = GenericLexicalSignal<ByteSymbol>;

  public abstract class Filter
  {
    public string ID { get ; set ; }

    public Signal Apply( Signal aInput, Context aContext ) 
    {
      if ( mResult == null )
        mResult = DoApply ( aInput, aContext );

      return mResult;
    }

    public virtual void Render ( TextRenderer aRenderer, RenderOptions aOptions ) { }

    protected abstract Signal DoApply( Signal aInput, Context aContext ) ;

    protected Signal mResult ;

  }

  public class ParallelFilter : Filter
  {
    public ParallelFilter( IEnumerable<Filter> aFilters ) 
    { 
      mFilters.AddRange( aFilters );
    }

    protected override Signal DoApply( Signal aInput, Context aContext ) 
    {
      int lC = mFilters.Count ;

      List<Signal> lBranches = aInput.BranchOut(lC) ;  
      
      List<Signal> lResults = new List<Signal> (lC) ;

      for ( int i = 0 ; i < lC ; ++ i )
      {
        lResults.Add( mFilters[i].Apply( lBranches[i], aContext ) ) ; 
      }

      var rResultArray = new SignalArray(lResults) ;

      return rResultArray ; 
    }

    public override void Render ( TextRenderer aRenderer, RenderOptions aOptions ) 
    { 
    }

    public override string ToString()
    {
      StringBuilder sb = new StringBuilder() ;
      mFilters.ForEach( n => sb.Append($"{n.ID}"));
      return sb.ToString() ;
    }

    List<Filter> mFilters = new List<Filter>(); 
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

  public abstract class LexicalFilter : Filter
  {
    protected LexicalFilter() : base() {}

    protected override Signal DoApply( Signal aInput, Context aContext ) 
    {
      LexicalSignal lLexicalSignal = aInput as LexicalSignal; 
      if ( lLexicalSignal == null )
        throw new ArgumentException("Input Signal must be a Lexical Signal.");

      return Process(lLexicalSignal, aContext);
    }
    
    protected abstract Signal Process ( LexicalSignal aInput, Context aContext );  

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
}
