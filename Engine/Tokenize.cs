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
  using Word = SymbolString<ByteSymbol>;
  using BytesSignal = LexicalSignal<ByteSymbol>;
  using WordSignal  = LexicalSignal<WordSymbol>;

  public class Tokenizer : BytesFilter
  {
    public Tokenizer( ByteSymbol aSeparator = null ) : base() 
    {
      mSeparator = aSeparator ?? BytesSource.GetTextSeparator() ; 
    }

    protected override Signal Process ( BytesSignal aInput, Context aContext )
    {
      List<ByteSymbol> lCurrStr = new List<ByteSymbol>();

      List<WordSymbol> lWords = new List<WordSymbol>(); 

      foreach( var lByte in aInput.String.Symbols )
      {
        if ( lByte == mSeparator )
        { 
          if ( lCurrStr.Count > 0 ) 
          {
            var lWord = new Word(lCurrStr);
            lWords.Add( new WordSymbol(lWords.Count,lWord) ); 
          }

          lCurrStr.Clear(); 
        }
        else
        {
          lCurrStr.Add( lByte );  
        }
      }

      if ( lCurrStr.Count > 0 ) 
      {
        var lWord = new Word(lCurrStr);
        lWords.Add( new WordSymbol(lWords.Count,lWord) ); 
      }

      mResult = new WordSignal(lWords);
      mResult.Name = "Words";

      return mResult ;
    }

    public override string ToString() => $"Tokenize({mSeparator.Meaning})";

    ByteSymbol mSeparator ;


  }

}
