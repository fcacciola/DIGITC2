using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Operations;
using NWaves.Signals;

namespace DIGITC2
{
  using BytesSignal = GenericLexicalSignal<ByteSymbol>;
  using TextSignal  = GenericLexicalSignal<TextSymbol>;

  public class BytesToText : BytesFilter
  {
    public BytesToText( string aCharSet ) : base() { mCharSet = aCharSet ; }

    protected override Signal Process ( BytesSignal aInput, Context aContext )
    {
      Encoding lEncoding = Encoding.GetEncoding(mCharSet);

      List<TextSymbol> lTextSymbols = new List<TextSymbol> ();

      byte[] lBuffer = new byte[1];

      foreach( var lByteSymbol in aInput.Symbols )
      {
        lBuffer[0] = lByteSymbol.Byte; 
        string lText = lEncoding.GetString(lBuffer);
        lTextSymbols.Add( new TextSymbol(lTextSymbols.Count, lText ) );
      }
  
      mResult = new TextSignal(lTextSymbols);

      mResult.Name = "Text";

      return mResult ;
    }

    public override void Render ( TextRenderer aRenderer, RenderOptions aOptions ) 
    { 
      aRenderer.Render($"", aOptions);
    }

    string mCharSet ;
  }

}
