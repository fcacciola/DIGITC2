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

    public int Length => Symbols.Count ;

    public List<Symbol> Symbols = new List<Symbol>();

    public List<SYM> GetSymbols<SYM>() => Symbols.Cast<SYM>().ToList();

    public Symbol this[int aIdx] => Symbols[aIdx];

    public override Distribution GetDistribution() => new Distribution( Symbols.ConvertAll( s => s.ToSample() ) ) ;

    public Distribution GetDistribution( Func<Symbol, Sample> ToSampleF ) => new Distribution( Symbols.ConvertAll( s => ToSampleF(s) ) ) ;
  }


}
