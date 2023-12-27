using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Operations;
using NWaves.Signals;

namespace DIGITC2
{

  public class BinarizeByDuration : LexicalFilter
  {
    public BinarizeByDuration() 
    { 
    }

    protected override Step Process (LexicalSignal aInput, Step aStep )
    {
       List<BitSymbol>   lBits     = new List<BitSymbol>   ();
       List<PulseSymbol> lBitViews = new List<PulseSymbol> ();

       var lSymbols = aInput.GetSymbols<PulseSymbol>() ;
       
       double lAccDuration = 0 ;
       lSymbols.ForEach( s => lAccDuration += s.Duration ) ;
       double lAvgDuration = lAccDuration / (double)lSymbols.Count ;

       double lMaxDuration = 0 ;
       lSymbols.ForEach( s => { if ( s.Duration < 3 * lAvgDuration ) lMaxDuration = Math.Max(s.Duration, lMaxDuration) ; } ) ;

       foreach ( PulseSymbol lGI in lSymbols ) 
       {
         bool lOne = ( lGI.Duration / lMaxDuration ) > mThreshold ;

         PulseSymbol lViewSym = lGI.Copy() as PulseSymbol ;
         lViewSym.MaxAmplitude = lOne ? - lGI.MaxAmplitude * 0.5f : - lGI.MaxAmplitude * 0.2f;
         lBitViews.Add( lViewSym ) ; 

         lBits.Add( new BitSymbol( lBits.Count, lOne, lViewSym )) ;

       }
   
       mStep = aStep.Next( new LexicalSignal(lBits), "Duration-based Bits", this) ;

       return mStep ;
    }

    protected override string Name => "BinarizeByDuration" ;

  }

}
