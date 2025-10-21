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
    mPipelineSelection = new PipelineSelection(DContext.Session.Args.Get(Name, "Pipelines"));
  }

  protected override void Process (LexicalSignal aInput, Packet aInputPacket, List<Packet> rOutput )
  {
    DContext.WriteLine("Decoding Tap Codes as Bytes from Latin-alphabet Polybius Squares");
    DContext.Indent();

    var lSymbols = aInput.GetSymbols<TapCodeSymbol>();
    var lCodes   = lSymbols.ConvertAll( s => s.Code ) ;

    if ( mPipelineSelection.IsActive("LatinAlphabet_Simple") )
      ProcessCodes(aInputPacket, lCodes, PolybiusSquare.LatinAlphabet_Simple, rOutput);

    if ( mPipelineSelection.IsActive("LatinAlphabet_Extended") )
      ProcessCodes(aInputPacket, lCodes, PolybiusSquare.LatinAlphabet_Extended, rOutput);
  }

  void ProcessCodes( Packet aInputPacket, List<TapCode> aCodes, PolybiusSquare aSquare, List<Packet> rOutput )
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
      rOutput.Add( new Packet(Name, aInputPacket, new LexicalSignal(lByteSymbols), aSquare.Name ) ) ;
  }


  public override string Name => this.GetType().Name ;

  PipelineSelection mPipelineSelection ;
}

}
