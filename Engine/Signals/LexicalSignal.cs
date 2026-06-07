using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Signals;

namespace ENGINE
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

    public int Length => Symbols.Count ;

    public List<Symbol> Symbols = new List<Symbol>();

    public List<SYM> GetSymbols<SYM>() => Symbols.Cast<SYM>().ToList();

    public Symbol this[int aIdx] => Symbols[aIdx];
  }

  public class FileSignal : Signal
  {
    public FileSignal( string aFilename) : base()
    {
      mFilename = aFilename;
    }

    public LexicalSignal LoadLexicalSignal<SYM>() 
    {
      if ( File.Exists(mFilename) )
      {
        List<SYM> rSymbols = new List<SYM>();
        string[] lLines = File.ReadAllLines(mFilename);
        foreach (string line in lLines) 
        {
          try
          {
            SYM lSym = (SYM)Activator.CreateInstance(typeof(SYM), new object[] { rSymbols.Count, line });
            rSymbols.Add(lSym);
          }
          catch (Exception )
          {
          } 
        }

        if (  rSymbols.Count > 0 )
          return new LexicalSignal(rSymbols.Cast<Symbol>());
      }
      return null ;
    }

    public override Signal Copy()
    {
      return new FileSignal(mFilename);
    }

    string mFilename ;
  }

}
