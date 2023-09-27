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
  using WordSignal    = LexicalSignal<WordSymbol>;

  public class ScoreTokens : TokensFilter
  {
    public ScoreTokens() : base() 
    {
    }

    protected override Step Process ( TokensSignal aInput, Step aStep )
    {
      mStep = aStep.Next( aInput, "Token Scoring", this) ;

      return mStep ;
    }

  }

}
