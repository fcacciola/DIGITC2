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
  public class TokensToWords : LexicalFilter
  {
    public TokensToWords( string aCharSet = "us-ascii", string aFallback = "!" ) : base() { mCharSet = aCharSet ; mFallback = aFallback; }

    protected override Step Process ( LexicalSignal aInput, Step aStep )
    {
      Encoding lEncoding = Encoding.GetEncoding( mCharSet
                                               , new EncoderReplacementFallback("(unknown)")
                                               , new DecoderReplacementFallback(mFallback));

      List<WordSymbol> lWords = new List<WordSymbol> ();

      StringBuilder lSB = new StringBuilder ();

      foreach( var lToken in aInput.GetSymbols<ArraySymbol>() )
      {
        lSB.Clear();

        byte[] lBuffer = new byte[1];

        foreach( var lByteSymbol in lToken.GetSymbols<ByteSymbol>() )
        {
          lBuffer[0] = lByteSymbol.Byte; 
          string lDigit = lEncoding.GetString(lBuffer);

          if ( ! IsValidTextDigit( lDigit ) )
            lDigit = mFallback ;

          lSB.Append( lDigit );

        }

        string lWord = lSB.ToString();
        if ( ! string.IsNullOrEmpty( lWord ) )
          lWords.Add( new WordSymbol(lWords.Count, lWord ) );
      }
  
      mStep = aStep.Next( new LexicalSignal(lWords), "Words", this) ;

      return mStep ;
    }

    bool IsValidTextDigit ( string aText )
    {
      if ( string.IsNullOrEmpty( aText ) ) 
        return false;

      char lChar = aText[0];

      return char.IsLetterOrDigit(lChar) 
            //|| char.IsWhiteSpace(lChar) 
            //|| char.IsPunctuation(lChar) 
            //|| char.IsSeparator(lChar) 
            ;
    }

    protected override string Name => "TokenToWords" ;

    string mCharSet ;
    string mFallback ; 
  }

}
