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
    mBranchSelection = new Branch.Selection(DIGITC_Context.Session.Args.Get("BinarizeFromTapCode_Branches"));

    mMinBitCount = DIGITC_Context.Session.Args.GetOptionalInt("BinarizeFromTapCode_MinBitCount").GetValueOrDefault(20);
  }


  protected override void Process (LexicalSignal aInput, Branch aInputBranch, List<Branch> rOutput )
  {
    DIGITC_Context.WriteLine("Binarizing Tap Codes via Binary Polybius Squares");
    DIGITC_Context.Indent();

    var lSymbols = aInput.GetSymbols<TapCodeSymbol>();
    var lCodes   = lSymbols.ConvertAll( s => s.Code ) ;

    if ( mBranchSelection.IsActive("Binary_3_1_Guarded") )
      ProcessCodes(aInputBranch, lCodes, PolybiusSquare.Binary_3_1_Guarded, rOutput ) ;

    if ( mBranchSelection.IsActive("Binary_2_1_Guarded") )
      ProcessCodes(aInputBranch, lCodes, PolybiusSquare.Binary_2_1_Guarded, rOutput ) ;

    if ( mBranchSelection.IsActive("Binary_3_1") )
      ProcessCodes(aInputBranch, lCodes, PolybiusSquare.Binary_3_1, rOutput ) ;

    if ( mBranchSelection.IsActive("Binary_2_1") )
      ProcessCodes(aInputBranch, lCodes, PolybiusSquare.Binary_2_1, rOutput ) ;

    if ( mBranchSelection.IsActive("Binary") )
      ProcessCodes(aInputBranch, lCodes, PolybiusSquare.Binary, rOutput ) ;

    DIGITC_Context.Unindent();  
  }

  void ProcessCodes( Branch aInputBranch, List<TapCode> aCodes, PolybiusSquare aSquare, List<Branch> rOutput )
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
      rOutput.Add( new Branch(aInputBranch, new LexicalSignal(lBits), aSquare.Name ) ) ;
  }

  protected override string Name => "BinarizeFromTapCode" ;

  int mMinBitCount ;
  Branch.Selection mBranchSelection ;
}

}
