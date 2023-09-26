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
  using Token = SymbolString<ByteSymbol>;

  public abstract class Symbol : ICloneable, IWithState
  {
    public Symbol( int aIdx ) { Idx = aIdx ; }

    public abstract object Clone() ;  

    public abstract string Type { get; }
    public abstract string Meaning { get; }

    public int Idx ;

    public virtual State GetState()
    {
      State rState = new State();
      rState.Add( State.With($"[{Idx}]",Meaning) ); 
      return rState;
    }

    public override bool Equals(object obj) => this.ValueEquals(obj as Symbol);

    public bool ValueEquals(Symbol aRHS)
    {
      if (this is null)
      {
        if (aRHS is null)
            return true;

        return false;
      }

      return string.Compare(Meaning, aRHS.Meaning) == 0;
    }

    public override int GetHashCode() => Meaning.GetHashCode();

    public static bool operator ==(Symbol lhs, Symbol rhs) => lhs.ValueEquals(rhs);
    public static bool operator !=(Symbol lhs, Symbol rhs) => !(lhs == rhs);

  }

  public class GatedSymbol : Symbol
  {
    public GatedSymbol( int aIdx, float aAmplitud, int aSamplingRate, int aPos, int aLength ) : base(aIdx)
    {
      Amplitude    = aAmplitud;
      SamplingRate = aSamplingRate; 
      Pos          = aPos; 
      Length       = aLength;
    }

    public double Duration => (double)Length / (double)SamplingRate;

    public override string Type => "Gated" ;

    public override string Meaning => $"{Duration:F2} at {(double)Pos/(double)SamplingRate:F2}" ;

    public override object Clone() {  return new GatedSymbol( Idx, Amplitude, SamplingRate, Pos, Length ); }  

    public bool IsGap => Amplitude == 0 ;

    public void DumpSamples( List<float> aSamples )
    {
      int lC = aSamples.Count ;
      for( int i = lC ; i < Pos ; i++ )
        aSamples.Add(0);

      for( int i = 0; i < Length; i++ ) 
        aSamples.Add(Amplitude);
    }

    public float Amplitude ;
    public int   SamplingRate ;
    public int   Pos ;
    public int   Length ; 
  }

  public class BitSymbol : Symbol
  {
    public BitSymbol( int aIdx, bool aOne, GatedSymbol aView ) : base(aIdx) { One = aOne ; View = aView ; }

    public override string Type => "Bit" ;

    public override object Clone() { return new BitSymbol( Idx, One, View?.Clone()  as GatedSymbol ); }  

    public override string Meaning => One ? "1" : "0" ;

    public bool One ;

    public GatedSymbol View ;
  }

  public class ByteSymbol : Symbol
  {
    public ByteSymbol( int aIdx, byte aByte ) : base(aIdx) { Byte = aByte ; }

    public override string Type => "Byte" ;

    public override object Clone() { return new ByteSymbol( Idx, Byte ); }  

    public override string Meaning => $"{Byte.ToString():x}" ;

    public byte Byte ;
  }

  public class TokenSymbol : Symbol
  {
    public TokenSymbol( int aIdx, Token aToken ) : base(aIdx) { Token = aToken ; }

    public override string Type => "Token" ;

    public override object Clone() { return new TokenSymbol( Idx, Token.Copy() ); }  

    public override string Meaning => Token.ToString() ;

    public Token Token ;
  }

  public class WordSymbol : Symbol
  {
    public WordSymbol( int aIdx, string aText ) : base(aIdx) { Word = aText ; }

    public override string Type => "Word" ;

    public override object Clone() { return new WordSymbol( Idx, Word ); }  

    public override string Meaning => Word ;

    public string Word ;
  }

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
