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

  protected override void OnSetup()
  { 
    switch ( Params.Get("PSquare") ) 
    {
      case "LatinAlphabet_Extended": mPSquare = PolybiusSquare.LatinAlphabet_Extended; break;
      case "LatinAlphabet_Simple":
      default: mPSquare = PolybiusSquare.LatinAlphabet_Simple; break ;
    }
  }

  protected override Packet Process()
  {
    var lSymbols = LexicalInput.GetSymbols<TapCodeSymbol>();
    var lCodes   = lSymbols.ConvertAll( s => s.Code ) ;

    List<string> lRawLetters = lCodes.ConvertAll( code => { string rLetter = mPSquare.Decode(code) ; WriteLine($"{code} -> {rLetter}") ;  return rLetter ; } );

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
         return CreateOutput( new LexicalSignal(lByteSymbols), mPSquare.Name ) ;
    else return CreateQuitOutput();

  }

  public override string Name => this.GetType().Name ;

  PolybiusSquare mPSquare ;
}

}
