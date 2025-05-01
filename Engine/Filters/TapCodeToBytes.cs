using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Operations;
using NWaves.Signals;

namespace DIGITC2_ENGINE
{
public class TapCodeToBytes : LexicalFilter
{
  public TapCodeToBytes() : base() 
  { 
  }

  public override void Setup()
  { 
    mBranchSelection = new Branch.Selection(DContext.Session.Args.Get("TapCodeToBytes_Branches"));
  }

  protected override void Process (LexicalSignal aInput, Branch aInputBranch, List<Branch> rOutput )
  {
    DContext.WriteLine("Decoding Tap Codes as Bytes from Latin-alphabet Polybius Squares");
    DContext.Indent();

    var lSymbols = aInput.GetSymbols<TapCodeSymbol>();
    var lCodes   = lSymbols.ConvertAll( s => s.Code ) ;

    if ( mBranchSelection.IsActive("LatinAlphabet_Simple") )
      ProcessCodes(aInputBranch, lCodes, PolybiusSquare.LatinAlphabet_Simple, rOutput);

    if ( mBranchSelection.IsActive("LatinAlphabet_Extended") )
      ProcessCodes(aInputBranch, lCodes, PolybiusSquare.LatinAlphabet_Extended, rOutput);
  }

  void ProcessCodes( Branch aInputBranch, List<TapCode> aCodes, PolybiusSquare aSquare, List<Branch> rOutput )
  {
    List<string> lRawLetters = aCodes.ConvertAll( code => { string rLetter = aSquare.Decode(code) ; DContext.WriteLine($"{code} -> {rLetter}") ;  return rLetter ; } );

    List<ByteSymbol> lByteSymbols = new List<ByteSymbol>();

    Encoding lEncoding = Encoding.GetEncoding("us-ascii");
    foreach( var lRawLetter in lRawLetters )
    {
      if ( lRawLetter != "" )
      {
        var lBytes = lEncoding.GetBytes(lRawLetter);

        if ( lBytes.Length > 0 ) 
          lByteSymbols.Add( new ByteSymbol(lByteSymbols.Count, lBytes[0])) ;
      }
    }

    if ( lByteSymbols.Count > 0 )
      rOutput.Add( new Branch(aInputBranch, new LexicalSignal(lByteSymbols), aSquare.Name ) ) ;
  }


  protected override string Name => "TapCodeToBytes" ;

  Branch.Selection mBranchSelection ;
}

}
