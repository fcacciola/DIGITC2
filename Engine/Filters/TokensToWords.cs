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
            //|| char.IsWhiteSpace(lChar) 
            //|| char.IsSeparator(lChar) 
            ;
    }
  }

  public class TokensToWords : LexicalFilter
  {
    public TokensToWords() : base() 
    { 
    }

    protected override void Process(LexicalSignal aInput, Branch aInputBranch, List<Branch> rOutput)
    {
      Process( new Options() { Label = "ascii CharSet", CharSet = "ascii", Fallback = "!", Validator = new TextDigitValidator() }
             , aInput, aInputBranch, rOutput) ;
    }

    void Process( Options aOptions, LexicalSignal aInput, Branch aInputBranch, List<Branch> rOutput)
    {
      Encoding lEncoding = Encoding.GetEncoding( aOptions.CharSet
                                               , new EncoderReplacementFallback("(unknown)")
                                               , new DecoderReplacementFallback( aOptions.Fallback));

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

          if ( !  aOptions.Validator.IsValid( lDigit ) )
            lDigit = aOptions.Fallback ;

          lSB.Append( lDigit );

        }

        string lWord = lSB.ToString();
        if ( ! string.IsNullOrEmpty( lWord ) )
          lWords.Add( new WordSymbol(lWords.Count, lWord ) );
      }
  
      rOutput.Add( new Branch( new LexicalSignal(lWords), aOptions.Label) ) ;
    }


    protected override string Name => "TokenToWords" ;

    class Options
    {
      internal string             Label ;
      internal string             CharSet ;
      internal string             Fallback ; 
      internal TextDigitValidator Validator ;
    }
  }

}
