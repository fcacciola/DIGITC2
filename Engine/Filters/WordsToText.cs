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
  public class TextMessage : IWithState
  {
    public TextMessage( string aText ) { Text = aText ; }  

    public override string ToString() { return Text ; } 

    public State GetState()
    {
      return new State("TextMessage", "Text Message:", StateValue.From(Text));
    }

    public string Text {  get; private set; }
  }

  public class WordsToText : LexicalFilter
  {
    public WordsToText() : base() 
    { 
    }

    protected override Step Process ( LexicalSignal aInput, Step aStep )
    {
      List<string> lWords = new List<string>();  

      foreach( var lWS in aInput.GetSymbols<WordSymbol>() )
      {
        lWords.Add( lWS.Word);
      }

      string lText = string.Join(" ",lWords);

      List<TextSymbol> lTextSymbols = new List<TextSymbol> ();
      lTextSymbols.Add( new TextSymbol(0,lText) );  

      mStep = aStep.Next( new LexicalSignal(lTextSymbols), "Text", this, new TextMessage(lText)) ;

      return mStep ;
    }

    protected override string Name => "WordsToText" ;

  }

}
