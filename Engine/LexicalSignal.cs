using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.TextFormatting;

using NWaves.Signals;

namespace DIGITC2
{
  public class SymbolString<SYM> where SYM : Symbol, IWithState
  {
    public SymbolString( IEnumerable<SYM> aSymbols )
    {
      Symbols.AddRange(aSymbols);
    }

    public State GetState() => State.From(null, Symbols);

    public SymbolString<SYM> Copy()
    {
      return new SymbolString<SYM>( Symbols.ConvertAll( s => s.Clone() ).Cast<SYM>() ) ;
    }

    public Histogram Histogram
    {
      get
      {
        if ( mHistogram == null )
          BuildHistogram();
        return mHistogram;  
      }
    }

    public int Length => Symbols.Count ;

    public List<SYM> Symbols = new List<SYM>();

    public SYM this[int aIdx] => Symbols[aIdx];

    void BuildHistogram()
    {
      mHistogram = new Histogram(); 
      Symbols.ForEach( s => mHistogram.Add( s ) );
    }

    Histogram mHistogram = null ;
  }

  public class LexicalSignal<SYM> : Signal where SYM : Symbol
  {
    public LexicalSignal( IEnumerable<SYM> aSymbols )
    {
      String = new SymbolString<SYM>(aSymbols);
    }

    public override Signal Copy()
    {
      return new LexicalSignal<SYM>( String.Copy() ) ;
    }

    LexicalSignal( SymbolString<SYM> aString )
    {
      String = aString ;
    }

    public SymbolString<SYM> String ;

    public Histogram Histogram => String.Histogram ;

    public override Plot CreatePlot( Plot.Options aOptions ) 
    {
      return Histogram.CreatePlot( aOptions ) ;
    }

    protected override void UpdateState( State rS ) 
    {
      rS.Add( String.GetState() );
    }

  }


}
