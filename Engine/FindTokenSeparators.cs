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

  public class FindTokenSeparators : BytesFilter
  {
    public FindTokenSeparators() : base() 
    {
    }

    protected override Step Process ( BytesSignal aInput, Step aStep )
    {
      mStep = aStep.Next( aInput, "Token Separatos", this) ;

      return mStep ;
    }

    public override string ToString() => $"FindTokenSeparators()";



  }

}
