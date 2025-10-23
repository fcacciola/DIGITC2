using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using NWaves.Operations;
using NWaves.Signals;

namespace DIGITC2_ENGINE
{
  public class TextDigitValidator
  {
    public TextDigitValidator()
    {
    }

    public bool IsValid ( string aText )
    {
      if ( string.IsNullOrEmpty( aText ) ) 
        return false;

      char lChar = aText[0];

      return    char.IsLetterOrDigit(lChar) 
             || char.IsPunctuation(lChar) 
            ;
    }
  }

  public class TokensToWords : LexicalFilter
  {
    public TokensToWords() : base() 
    { 
    }

    protected override Packet Process ()
    {
      Options lOptions = new () { CharSet = "ascii"
                                , Fallback = "!"
                                , Validator = new TextDigitValidator() 
                                } ;

      Encoding lEncoding = Encoding.GetEncoding( lOptions.CharSet
                                               , new EncoderReplacementFallback("(unknown)")
                                               , new DecoderReplacementFallback( lOptions.Fallback));

      List<WordSymbol> lWords = new List<WordSymbol> ();

      StringBuilder lSB = new StringBuilder ();

      foreach( var lToken in LexicalInput.GetSymbols<ArraySymbol>() )
      {
        lSB.Clear();

        byte[] lBuffer = new byte[1];

        foreach( var lByteSymbol in lToken.GetSymbols<ByteSymbol>() )
        {
          lBuffer[0] = lByteSymbol.Byte; 
          string lDigit = lEncoding.GetString(lBuffer);

          if ( ! lOptions.Validator.IsValid( lDigit ) )
            lDigit = lOptions.Fallback ;

          lSB.Append( lDigit );

        }

        string lWord = lSB.ToString();
        if ( ! string.IsNullOrEmpty( lWord ) )
          lWords.Add( new WordSymbol(lWords.Count, lWord ) );
      }
  
      WriteLine($"Words:{Environment.NewLine}{string.Join(Environment.NewLine, lWords.ConvertAll( b => b.Meaning) ) }" ) ;

      return CreateOutput( new LexicalSignal(lWords), Name) ;
    }

    public override string Name => this.GetType().Name ;

    class Options
    {
      internal string             CharSet ;
      internal string             Fallback ; 
      internal TextDigitValidator Validator ;
    }
  }

}
