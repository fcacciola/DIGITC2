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

    protected override void Process(LexicalSignal aInput, Packet aInputPacket, List<Packet> rOutput)
    {
      DContext.WriteLine("Converting Tokens to Words");
      DContext.Indent();

      Process( new Options() { Label = "ASCII CharSet with fallback:!"
                             , CharSet = "ascii"
                             , Fallback = "!"
                             , Validator = new TextDigitValidator() 
                             }

             , aInput, aInputPacket, rOutput) ;

      DContext.Unindent();
    }

    void Process( Options aOptions, LexicalSignal aInput, Packet aInputPacket, List<Packet> rOutput)
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
  
      DContext.WriteLine($"Words:{Environment.NewLine}{string.Join(Environment.NewLine, lWords.ConvertAll( b => b.Meaning) ) }" ) ;

      rOutput.Add( new Packet(Name, aInputPacket, new LexicalSignal(lWords), aOptions.Label) ) ;
    }


    public override string Name => this.GetType().Name ;

    class Options
    {
      internal string             Label ;
      internal string             CharSet ;
      internal string             Fallback ; 
      internal TextDigitValidator Validator ;

      public override string ToString() => Label ;
    }
  }

}
