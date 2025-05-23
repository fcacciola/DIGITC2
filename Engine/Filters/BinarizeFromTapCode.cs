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
    mPipelineSelection = new PipelineSelection(DContext.Session.Args.Get("BinarizeFromTapCode_Branches"));

    mMinBitCount = DContext.Session.Args.GetOptionalInt("BinarizeFromTapCode_MinBitCount").GetValueOrDefault(20);
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
    List<BitSymbol> lBits = new List<BitSymbol> ();

    foreach( var lRawBit in lRawBits )
    {
      if ( lRawBit != "?" )
      {
        bool lOne = lRawBit[0]=='1';
        double lLikelihood = lRawBit.Length == 2 ? 1.0 : 0.8 ;
        lBits.Add( new BitSymbol(lBits.Count, lOne, lLikelihood, null)) ;
      }
    }

    if ( lBits.Count > mMinBitCount)
      rOutput.Add( new Packet(aInputPacket, new LexicalSignal(lBits), aSquare.Name ) ) ;
  }

  public override string Name => this.GetType().Name ;

  int mMinBitCount ;
  PipelineSelection mPipelineSelection ;
}

}
