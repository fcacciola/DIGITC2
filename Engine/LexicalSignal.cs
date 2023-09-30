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
  public class LexicalSignal : Signal 
  {
    public LexicalSignal( IEnumerable<Symbol> aSymbols )
    {
      Symbols.AddRange(aSymbols);
    }

    public override Signal Copy()
    {
      return new LexicalSignal( Symbols.ConvertAll( s => s.Copy() ) ) ;
    }

    protected override void UpdateState( State rS ) 
    {
      rS.Add( State.From(null,null, Symbols) );
    }

    //public Histogram Histogram
    //{
    //  get
    //  {
    //    if (mHistogram == null)
    //      BuildHistogram();
    //    return mHistogram;
    //  }
    //}

    public int Length => Symbols.Count ;

    public List<Symbol> Symbols = new List<Symbol>();

    public List<SYM> GetSymbols<SYM>() => Symbols.Cast<SYM>().ToList();

    public Symbol this[int aIdx] => Symbols[aIdx];

    public override Samples GetSamples()
    {
      return new Samples(Symbols.ConvertAll( s => s.Value )) ;
    }

    //public Histogram.Params HistogramParams => Symbols[0].HistogramParams ;

    //void BuildHistogram()
    //{
    //  mHistogram = new Histogram( GetSamples(), HistogramParams );
    //}

    //Histogram mHistogram = null;

  }


}
