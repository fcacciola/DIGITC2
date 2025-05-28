using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DocumentFormat.OpenXml.Drawing;

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
    mPipelineSelection = new PipelineSelection(DContext.Session.Args.Get("BinarizeFromTapCode_Pipelines"));

    mMinBitCount = DContext.Session.Args.GetOptionalInt("BinarizeFromTapCode_MinBitCount").GetValueOrDefault(20);

    mFitnessMap = new FitnessMap(DContext.Session.Args.Get("BinarizeFromTapCode_FitnessMap"));

    mQuitThreshold = DContext.Session.Args.GetOptionalInt("BinarizeFromTapCode_QuitThreshold").GetValueOrDefault(1);
  }

  protected override void Process (LexicalSignal aInput, Packet aInputPacket, List<Packet> rOutput )
  {
    DContext.WriteLine("Binarizing Tap Codes via Binary Polybius Squares");
    DContext.Indent();

    var lSymbols = aInput.GetSymbols<TapCodeSymbol>();
    var lCodes   = lSymbols.ConvertAll( s => s.Code ) ;

    if ( mPipelineSelection.IsActive("Binary_3_1_Guarded") )
      ProcessCodes(aInputPacket, lCodes, PolybiusSquare.Binary_3_1_Guarded, rOutput ) ;

    if ( mPipelineSelection.IsActive("Binary_2_1_Guarded") )
      ProcessCodes(aInputPacket, lCodes, PolybiusSquare.Binary_2_1_Guarded, rOutput ) ;

    if ( mPipelineSelection.IsActive("Binary_3_1") )
      ProcessCodes(aInputPacket, lCodes, PolybiusSquare.Binary_3_1, rOutput ) ;

    if ( mPipelineSelection.IsActive("Binary_2_1") )
      ProcessCodes(aInputPacket, lCodes, PolybiusSquare.Binary_2_1, rOutput ) ;

    if ( mPipelineSelection.IsActive("Binary") )
      ProcessCodes(aInputPacket, lCodes, PolybiusSquare.Binary, rOutput ) ;

    DContext.Unindent();  
  }

  void ProcessCodes( Packet aInputPacket, List<TapCode> aCodes, PolybiusSquare aSquare, List<Packet> rOutput )
  {
    List<string> lRawBits = aCodes.ConvertAll( code => aSquare.Decode(code));

    DContext.WriteLine($"RAW Bits: { string.Join(",",lRawBits)}" );

    List<BitSymbol> lBits = new List<BitSymbol> ();

    double lStrength = 0 ;

    foreach( var lRawBit in lRawBits )
    {
      if ( lRawBit != "?" )
      {
        bool lOne = lRawBit[0]=='1';
        double lLikelihood = lRawBit.Length == 2 ? 1.0 : 0.8 ;
        lStrength += lLikelihood ;
        lBits.Add( new BitSymbol(lBits.Count, lOne, lLikelihood, null)) ;
      }
    }

    DContext.WriteLine($"KNOWN Bits: {string.Join(", ", lBits.ConvertAll( b => b.Meaning) ) }" ) ;

    if ( lBits.Count > mMinBitCount)
    {
      double lSNR = lStrength / (double)lRawBits.Count ;
      
      int lLikelihood = (int)Math.Ceiling(lSNR * 100) ; 

      Fitness lFitness = mFitnessMap.Map(lLikelihood) ;

      Score lScore = new Score(Name, lLikelihood, lFitness) ;

      DContext.WriteLine($"Known Bits SNR: {lSNR}");
      DContext.WriteLine($"Score: {lScore}");
      DContext.WriteLine($"Likelihood: {lLikelihood}");
      DContext.WriteLine($"Fitness: {lFitness}");

      rOutput.Add( new Packet(Name, aInputPacket, new LexicalSignal(lBits), aSquare.Name, lScore, lLikelihood < mQuitThreshold ) ) ;
    }
  }

  public override string Name => this.GetType().Name ;

  int               mMinBitCount ;
  PipelineSelection mPipelineSelection ;
  FitnessMap        mFitnessMap ;
  int               mQuitThreshold ;
}

}
