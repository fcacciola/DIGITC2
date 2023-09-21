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
  using TokensSignal = LexicalSignal<TokenSymbol>;
  using WordSignal   = LexicalSignal<WordSymbol>;

  public class TokensToWords : ByteStringFilter
  {
    public TokensToWords( string aCharSet = "us-ascii", string aFallback = "!" ) : base() { mCharSet = aCharSet ; mFallback = aFallback; }

    protected override Signal Process ( TokensSignal aInput, Context aContext )
    {
      Encoding lEncoding = Encoding.GetEncoding( mCharSet
                                               , new EncoderReplacementFallback("(unknown)")
                                               , new DecoderReplacementFallback(mFallback));

      List<WordSymbol> lWords = new List<WordSymbol> ();

      StringBuilder lSB = new StringBuilder ();

      foreach( var lToken in aInput.String.Symbols )
      {
        lSB.Clear();

        byte[] lBuffer = new byte[1];

        foreach( var lByteSymbol in lToken.Token.Symbols )
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
  
      mResult = new WordSignal(lWords);

      mResult.Name = "Words";

      return mResult ;
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

    public override string ToString() => $"BytesToText(CharSet:{mCharSet})";

    string mCharSet ;
    string mFallback ; 
  }

}
