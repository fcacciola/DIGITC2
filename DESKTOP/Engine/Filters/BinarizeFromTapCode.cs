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

namespace ENGINE
{
public class BinarizeFromTapCode : FileLexicalFilter
{
  public BinarizeFromTapCode() 
  { 
  }

  protected override void OnSetup()
  {
    switch( Params.GetValue("PSquare") )
    {
      case "Binary_3_1_Guarded": mPolybiusSquare = PolybiusSquare.Binary_3_1_Guarded ; break ;
      case "Binary_2_1_Guarded": mPolybiusSquare = PolybiusSquare.Binary_2_1_Guarded; break ;
      case "Binary_3_1":         mPolybiusSquare = PolybiusSquare.Binary_3_1; break ;
      case "Binary_2_1":         mPolybiusSquare = PolybiusSquare.Binary_2_1; break ;
      case "Binary":
      default:                   mPolybiusSquare = PolybiusSquare.Binary ; break ;

    }

    mBitSize       = Params.GetInt("BitSize");
    mMinCount      = Params.GetInt("MinCount");
    mQuitThreshold = Params.GetDouble("QuitThreshold");
  }

  public class DecodedTapCode
  {
    public DecodedTapCode (TapCodeSymbol aSymbol, PolybiusSquare aPolybiusSquare )
    {
      Symbol = aSymbol;
      if ( IsSeparator)
           DecodedValue = " ";
      else DecodedValue = aPolybiusSquare.Decode(aSymbol.Code);
    }

    public bool IsSeparator => Symbol.Code.IsSeparator;

    public TapCodeSymbol Symbol { get; }

    public string DecodedValue { get; }

    public override string ToString() => $"[{Symbol.Code}]=>{DecodedValue}";
  }

  protected override Packet Process()
  {
    WriteLine2GUI("Convertting Tap Code to Bits...");
    Indent();

    var lTapCodeSignal = FileInput.LoadLexicalSignal<TapCodeSymbol>();
    
    var lSymbols = lTapCodeSignal.GetSymbols<TapCodeSymbol>();

    var lDecodedList = lSymbols.ConvertAll( s => new DecodedTapCode(s, mPolybiusSquare) );

    List<BitBagSymbol> lBitBags = new List<BitBagSymbol> ();

    List< List<DecodedTapCode> > lRawDecodedBags = new List<List<DecodedTapCode>> ();  

    List<DecodedTapCode> lCurrRawDecodedBag = new List<DecodedTapCode>();  

    lRawDecodedBags.Add(lCurrRawDecodedBag);

    foreach( var lDecoded in lDecodedList)
    {
      bool lIsSeparator = lDecoded.IsSeparator || lCurrRawDecodedBag.Count >= 8 ;
      if ( lIsSeparator )
      {
        if ( ! lDecoded.IsSeparator )
          lCurrRawDecodedBag.Add(lDecoded);

        lCurrRawDecodedBag = new List<DecodedTapCode>();  
        lRawDecodedBags.Add(lCurrRawDecodedBag);
      }
      else
      {
        lCurrRawDecodedBag.Add(lDecoded);
      }
    }

    WriteDetailLine($"RAW Decoded Bags count: {lRawDecodedBags.Count}" );

    List<double> lBitBagCoverages = new List<double>();

    foreach ( var lRawDecodedBag in lRawDecodedBags )
    {
      if ( lRawDecodedBag.Count == 0 ) 
       continue ;

      WriteDetailLine($"RAW Bag: { string.Join(",",lRawDecodedBag)}" );

      List<BitSymbol> lBits = new List<BitSymbol> ();

      double lStrength = 0 ;

      foreach( var lRawDecodedTapCode in lRawDecodedBag )
      {
        if ( lRawDecodedTapCode.DecodedValue != "?" )
        {
          bool lOne = lRawDecodedTapCode.DecodedValue[0]=='1';
          double lBitCoverage = mPolybiusSquare.HasExtendedBitSymbols ? ( lRawDecodedTapCode.DecodedValue.Length == 2 ? 1.0 : 0.8 ) : 1.0 ;
          lStrength += lBitCoverage ;
          lBits.Add( new BitSymbol(lBits.Count, lOne, lBitCoverage, lRawDecodedTapCode.Symbol.SamplePos)) ;
        }
        else lBits.Add( new BitSymbol(lBits.Count, null, 0, lRawDecodedTapCode.Symbol.SamplePos)) ;
      }

      double lBitBagCoverage = lStrength / (double)mBitSize ;
      lBitBagCoverages.Add(lBitBagCoverage);

      BitBagSymbol lBag = new BitBagSymbol(lBitBags.Count, lBits, lBitBagCoverage);

      WriteLine($"Bits: {lBag}");
      WriteDetailLine($"BitBag Coverage: {lBitBagCoverage}");

      lBitBags.Add(lBag); 
    }

    Unindent();

    double lAverageBitBagCoverage = lBitBagCoverages.Count > 0 ? lBitBagCoverages.Average() : 0.0;

    if (  Session.QuitDisabled || ( lBitBags.Count > mMinCount && lAverageBitBagCoverage >= mQuitThreshold ) )
    {
      return CreateOutput( new LexicalSignal(lBitBags), Name, new Score(Name,lAverageBitBagCoverage,Score.TypeE.Coverage) ) ;
    }
    else
    {
      return CreateQuitOutput();
    }
  }

  public override string Name => this.GetType().Name ;

  int            mBitSize ;
  int            mMinCount ;
  PolybiusSquare mPolybiusSquare ;
  double         mQuitThreshold ;
}

}
