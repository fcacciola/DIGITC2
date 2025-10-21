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

  public override void Setup()
  {
    mPipelineSelection = new PipelineSelection(DContext.Session.Args.Get(Name,"Pipelines"));

    mMinCount = DContext.Session.Args.GetOptionalInt(Name,"MinCount").GetValueOrDefault(20);

    mFitnessMap = new FitnessMap(DContext.Session.Args.Get(Name,"FitnessMap"));

    mQuitThreshold = DContext.Session.Args.GetOptionalInt(Name,"QuitThreshold").GetValueOrDefault(1);
  }

  protected override void Process (LexicalSignal aInput, Packet aInputPacket, List<Packet> rOutput )
  {
    DContext.WriteLine("Binarizing Tap Codes via Binary Polybius Squares");
    DContext.Indent();

    var lSymbols = aInput.GetSymbols<TapCodeSymbol>();
    var lCodes   = lSymbols.ConvertAll( s => s.Code ) ;

    if ( mPipelineSelection.IsActive("Binary") )
      ProcessCodes(aInputPacket, lCodes, PolybiusSquare.Binary, rOutput ) ;

    if ( mPipelineSelection.IsActive("Binary_3_1_Guarded") )
      ProcessCodes(aInputPacket, lCodes, PolybiusSquare.Binary_3_1_Guarded, rOutput ) ;

    if ( mPipelineSelection.IsActive("Binary_2_1_Guarded") )
      ProcessCodes(aInputPacket, lCodes, PolybiusSquare.Binary_2_1_Guarded, rOutput ) ;

    if ( mPipelineSelection.IsActive("Binary_3_1") )
      ProcessCodes(aInputPacket, lCodes, PolybiusSquare.Binary_3_1, rOutput ) ;

    if ( mPipelineSelection.IsActive("Binary_2_1") )
      ProcessCodes(aInputPacket, lCodes, PolybiusSquare.Binary_2_1, rOutput ) ;


    DContext.Unindent();  
  }

  void ProcessCodes( Packet aInputPacket, List<TapCode> aCodes, PolybiusSquare aSquare, List<Packet> rOutput )
  {
    List<BitBagSymbol> lBags = new List<BitBagSymbol> ();

    List< List<string> > lRawBags = new List<List<string>> ();  

    List<string> lCurrRawBag = new List<string>();  
    lRawBags.Add(lCurrRawBag);

    foreach( var lCode in aCodes )
    {
      bool lIsSeparator = ( lCode.Row == 0 && lCode.Col == 0 ) || lCurrRawBag.Count >= 8 ;
      if ( lIsSeparator )
      {
        lCurrRawBag = new List<string>();  
        lRawBags.Add(lCurrRawBag);
      }
      else
      {
        lCurrRawBag.Add(aSquare.Decode(lCode));
      }
    }

    DContext.WriteLine($"RAW Bags count: {lRawBags.Count}" );

    foreach ( var lRawBag in lRawBags )
    {
      if ( lRawBag.Count == 0 ) 
       continue ;

      DContext.WriteLine($"RAW Bag: { string.Join(",",lRawBag)}" );

      List<BitSymbol> lBits = new List<BitSymbol> ();

      double lStrength = 0 ;

      foreach( var lRawBit in lRawBag )
      {
        if ( lRawBit != "?" )
        {
          bool lOne = lRawBit[0]=='1';
          double lBitLikelihood = aSquare.HasExtendedBitSymbols ? ( lRawBit.Length == 2 ? 1.0 : 0.8 ) : 1.0 ;
          lStrength += lBitLikelihood ;
          lBits.Add( new BitSymbol(lBits.Count, lOne, lBitLikelihood)) ;
        }
        else lBits.Add( new BitSymbol(lBits.Count, null, 0)) ;
      }

      double lSNR = lStrength / (double)lRawBag.Count ;
      
      int lBagLikelihood = (int)Math.Ceiling(lSNR * 100) ; 

      DContext.WriteLine($"Known Bits SNR: {lSNR}");
      DContext.WriteLine($"Bag Likelihood: {lBagLikelihood}");

      BitBagSymbol lBag = new BitBagSymbol(lBags.Count, lBits, lBagLikelihood);

      lBags.Add(lBag); 
    }

    if ( lBags.Count > mMinCount)
    {
      rOutput.Add( new Packet(Name, aInputPacket, new LexicalSignal(lBags), aSquare.Name ) ) ;
    }
  }

  public override string Name => this.GetType().Name ;

  int               mMinCount ;
  PipelineSelection mPipelineSelection ;
  FitnessMap        mFitnessMap ;
  int               mQuitThreshold ;
}

}
