using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Operations;
using NWaves.Signals;

namespace DIGITC2
{
  using GatedLexicalSignal = GenericLexicalSignal<GatedSymbol>;
  using BitsSignal         = GenericLexicalSignal<BitSymbol>;

  public class BinarizeByDuration : GatedLexicalFilter
  {
    public BinarizeByDuration( double aThreshold ) { mThreshold = aThreshold ; }

    protected override Signal Process ( GatedLexicalSignal aInput, Context aContext )
    {
       List<BitSymbol>   lBits     = new List<BitSymbol>   ();
       List<GatedSymbol> lBitViews = new List<GatedSymbol> ();
             
       double lAccDuration = 0 ;
       aInput.Symbols.ForEach( s => lAccDuration += s.Duration ) ;
       double lAvgDuration = lAccDuration / (double)aInput.Symbols.Count ;

       double lMaxDuration = 0 ;
       aInput.Symbols.ForEach( s => { if ( s.Duration < 3 * lAvgDuration ) lMaxDuration = Math.Max(s.Duration, lMaxDuration) ; } ) ;

       foreach ( GatedSymbol lGI in aInput.Symbols ) 
       {
         bool lOne = ( lGI.Duration / lMaxDuration ) > mThreshold ;

         GatedSymbol lViewSym = lGI.Clone() as GatedSymbol ;
         lViewSym.Amplitude = lOne ? - lGI.Amplitude * 0.5f : - lGI.Amplitude * 0.2f;
         lBitViews.Add( lViewSym ) ; 

         lBits.Add( new BitSymbol( lBits.Count, lOne, lViewSym )) ;

       }
   
       mResult = new BitsSignal(lBits);

       mResult.Name = "DurationBits";

       return mResult ;
    }

    public override void Render ( TextRenderer aRenderer, RenderOptions aOptions ) 
    { 
      aRenderer.Render($"", aOptions);
    }

    double mThreshold ;
  }

}
