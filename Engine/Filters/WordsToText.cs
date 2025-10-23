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
  public class TextMessage : PacketData
  {
    public TextMessage( string aText ) { Text = aText ; }  

    public override string ToString() { return Text ; } 

    public string Text {  get; private set; }
  }

  public class WordsToText : LexicalFilter
  {
    public WordsToText() : base() 
    { 
    }

    protected override Packet Process ()
    {
      List<string> lWords = new List<string>();  

      foreach( var lWS in LexicalInput.GetSymbols<WordSymbol>() )
      {
        lWords.Add( lWS.Word);
      }

      string lText = string.Join(" ",lWords);

      WriteLine($"FINAl TEXT:{Environment.NewLine}{lText}");  

      List<TextSymbol> lTextSymbols = new List<TextSymbol> ();
      lTextSymbols.Add( new TextSymbol(0,lText) );  

      return CreateOutput( new LexicalSignal(lTextSymbols), "Text", null, false, new TextMessage(lText)) ;
    }

    public override string Name => this.GetType().Name ;

  }

}
