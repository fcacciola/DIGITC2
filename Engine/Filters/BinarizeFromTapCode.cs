using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using MathNet.Numerics.Integration;
using MathNet.Numerics.Statistics;

using Newtonsoft.Json.Serialization;

using NWaves.Operations;
using NWaves.Signals;

namespace DIGITC2_ENGINE
{
public class BinarizeFromTapCode : LexicalFilter
{
  public BinarizeFromTapCode() 
  { 
  }

  protected override void OnSetup()
  {
    switch( Params.Get("PSquare") )
    {
      case "Binary_3_1_Guarded": mPolybiusSquare = PolybiusSquare.Binary_3_1_Guarded ; break ;
      case "Binary_2_1_Guarded": mPolybiusSquare = PolybiusSquare.Binary_2_1_Guarded; break ;
      case "Binary_3_1":         mPolybiusSquare = PolybiusSquare.Binary_3_1; break ;
      case "Binary_2_1":         mPolybiusSquare = PolybiusSquare.Binary_2_1; break ;
      case "Binary":
      default:                   mPolybiusSquare = PolybiusSquare.Binary ; break ;

    }

    mFitnessMap    = new FitnessMap(Params.Get("FitnessMap"));
    mMinCount      = Params.GetInt("MinCount");
    mQuitThreshold = Params.GetInt("QuitThreshold");
  }

  protected override Packet Process()
  {
    var lSymbols = LexicalInput.GetSymbols<TapCodeSymbol>();

    var lCodes   = lSymbols.ConvertAll( s => s.Code ) ;

    List<BitBagSymbol> lBags = new List<BitBagSymbol> ();

    List< List<string> > lRawBags = new List<List<string>> ();  

    List<string> lCurrRawBag = new List<string>();  
    lRawBags.Add(lCurrRawBag);

    foreach( var lCode in lCodes )
    {
      bool lIsSeparator = ( lCode.Row == 0 && lCode.Col == 0 ) || lCurrRawBag.Count >= 8 ;
      if ( lIsSeparator )
      {
        lCurrRawBag = new List<string>();  
        lRawBags.Add(lCurrRawBag);
      }
      else
      {
        lCurrRawBag.Add(mPolybiusSquare.Decode(lCode));
      }
    }

    WriteLine($"RAW Bags count: {lRawBags.Count}" );

    foreach ( var lRawBag in lRawBags )
    {
      if ( lRawBag.Count == 0 ) 
       continue ;

      WriteLine($"RAW Bag: { string.Join(",",lRawBag)}" );

      List<BitSymbol> lBits = new List<BitSymbol> ();

      double lStrength = 0 ;

      foreach( var lRawBit in lRawBag )
      {
        if ( lRawBit != "?" )
        {
          bool lOne = lRawBit[0]=='1';
          double lBitLikelihood = mPolybiusSquare.HasExtendedBitSymbols ? ( lRawBit.Length == 2 ? 1.0 : 0.8 ) : 1.0 ;
          lStrength += lBitLikelihood ;
          lBits.Add( new BitSymbol(lBits.Count, lOne, lBitLikelihood)) ;
        }
        else lBits.Add( new BitSymbol(lBits.Count, null, 0)) ;
      }

      double lSNR = lStrength / (double)lRawBag.Count ;
      
      int lBagLikelihood = (int)Math.Ceiling(lSNR * 100) ; 

      WriteLine($"Known Bits SNR: {lSNR}");
      WriteLine($"Bag Likelihood: {lBagLikelihood}");

      BitBagSymbol lBag = new BitBagSymbol(lBags.Count, lBits, lBagLikelihood);

      lBags.Add(lBag); 
    }

    if ( lBags.Count > mMinCount)
    {
      return CreateOutput( new LexicalSignal(lBags), mPolybiusSquare.Name ) ;
    }
    else
    {
      return CreateQuitOutput();

    }
  }

  public override string Name => this.GetType().Name ;

  int            mMinCount ;
  PolybiusSquare mPolybiusSquare ;
  FitnessMap     mFitnessMap ;
  int            mQuitThreshold ;
}

}
