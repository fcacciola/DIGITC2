using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Operations;
using NWaves.Signals;

namespace DIGITC2
{
  using Token         = SymbolString<ByteSymbol>;
  using BytesSignal   = LexicalSignal<ByteSymbol>;
  using TokensSignal  = LexicalSignal<TokenSymbol>;

  public class Tokenizer : BytesFilter
  {
    public Tokenizer( ByteSymbol aSeparator = null ) : base() 
    {
      mSeparator = aSeparator ?? BytesSource.GetTextSeparator() ; 
    }

    protected override Step Process ( BytesSignal aInput, Step aStep )
    {
      List<ByteSymbol> lCurrToken = new List<ByteSymbol>();

      List<TokenSymbol> lTokens = new List<TokenSymbol>(); 

      foreach( var lByte in aInput.String.Symbols )
      {
        if ( lByte == mSeparator )
        { 
          if ( lCurrToken.Count > 0 ) 
          {
            var lWord = new Token(lCurrToken);
            lTokens.Add( new TokenSymbol(lTokens.Count,lWord) ); 
          }

          lCurrToken.Clear(); 
        }
        else
        {
          lCurrToken.Add( lByte );  
        }
      }

      if ( lCurrToken.Count > 0 ) 
      {
        var lToken = new Token(lCurrToken);
        lTokens.Add( new TokenSymbol(lTokens.Count,lToken) ); 
      }

      mStep = aStep.Next( new TokensSignal(lTokens), "Tokens", this) ;

      return mStep ;
    }

    public override string ToString() => $"Tokenize({mSeparator.Meaning})";

    ByteSymbol mSeparator ;


  }

}
