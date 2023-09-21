using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using NWaves.Operations;
using NWaves.Signals;

namespace DIGITC2
{
  using BytesSignal = LexicalSignal<ByteSymbol>;
  using TextSignal  = LexicalSignal<TextSymbol>;
  using Word = SymbolString<ByteSymbol>;
  using WordSignal  = LexicalSignal<WordSymbol>;

  public class WordsToText : WordsFilter
  {
    public WordsToText( string aCharSet = "us-ascii", string aFallback = "" ) : base() { mCharSet = aCharSet ; mFallback = aFallback; }

    protected override Signal Process ( WordSignal aInput, Context aContext )
    {
      Encoding lEncoding = Encoding.GetEncoding( mCharSet
                                               , new EncoderReplacementFallback("(unknown)")
                                               , new DecoderReplacementFallback(mFallback));

      List<TextSymbol> lTextSymbols = new List<TextSymbol> ();

      foreach( var lWS in aInput.String.Symbols )
      {
        byte[] lBuffer = new byte[1];

        foreach( var lByteSymbol in lWS.Word.Symbols )
        {
          lBuffer[0] = lByteSymbol.Byte; 
          string lText = lEncoding.GetString(lBuffer);
          if ( ! string.IsNullOrEmpty( lText ) ) 
          {
            char lChar = lText[0];

            if (    char.IsLetterOrDigit(lChar) 
                 || char.IsWhiteSpace(lChar) 
                 || char.IsPunctuation(lChar) 
                 || char.IsSeparator(lChar) 
               ) 
              lTextSymbols.Add( new TextSymbol(lTextSymbols.Count, lText ) );
          }
        }

        if ( lWS.Idx < aInput.String.Symbols.Count - 1 ) 
          lTextSymbols.Add( new TextSymbol(lTextSymbols.Count, " " ) );
      }
  
      mResult = new TextSignal(lTextSymbols);

      mResult.Name = "Text";

      return mResult ;
    }

    public override string ToString() => $"BytesToText(CharSet:{mCharSet})";

    string mCharSet ;
    string mFallback ; 
  }

}
