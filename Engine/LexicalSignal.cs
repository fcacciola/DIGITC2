using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Signals;

namespace DIGITC2
{
  using Token = SymbolString<ByteSymbol>;

  public abstract class Symbol : ICloneable
  {
    public Symbol( int aIdx ) { Idx = aIdx ; }

    public abstract object Clone() ;  

    public int Idx ;

    public virtual string Meaning => ToString();

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

    public override string ToString() => $"[{Duration:F2} at {(double)Pos/(double)SamplingRate:F2})]{Environment.NewLine}" ;

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

    public override object Clone() { return new BitSymbol( Idx, One, View?.Clone()  as GatedSymbol ); }  

    public override string ToString() => One ? "1" : "0" ;

    public bool One ;

    public GatedSymbol View ;
  }

  public class ByteSymbol : Symbol
  {
    public ByteSymbol( int aIdx, byte aByte ) : base(aIdx) { Byte = aByte ; }

    public override object Clone() { return new ByteSymbol( Idx, Byte ); }  

    public override string ToString() => $"[{Byte.ToString():x}]" ;

    public byte Byte ;
  }

  public class TokenSymbol : Symbol
  {
    public TokenSymbol( int aIdx, Token aToken ) : base(aIdx) { Token = aToken ; }

    public override object Clone() { return new TokenSymbol( Idx, Token.Copy() ); }  

    public override string ToString() => Token.ToString() ;

    public Token Token ;
  }

  public class WordSymbol : Symbol
  {
    public WordSymbol( int aIdx, string aText ) : base(aIdx) { Word = aText ; }

    public override object Clone() { return new WordSymbol( Idx, Word ); }  

    public override string ToString() => $"[{Word}]{Environment.NewLine}" ;

    public string Word ;
  }

  public class Histogram<SYM> : IEnumerable< KeyValuePair<SYM, int> >  where SYM : Symbol 
  {
    public void Add( SYM aSymbol )
    {
      if ( mMap.ContainsKey(aSymbol))
           mMap[aSymbol] = mMap[aSymbol] + 1  ;
      else mMap.Add(aSymbol, 1) ;
    }

    public int Count => mMap.Count; 

    public double ShannonEntropy
    {
      get
      {
        if ( mShannonEntropy == null )
          CalculateShannonEntropy();
        return mShannonEntropy.Value;
      }
    }

    public class Bin
    {
      public Bin( SYM aS, int aF ) { Symbol = aS ; Frequency = aF; }

      public SYM Symbol ;
      public int Frequency ;
    }

    public List<Bin> Sorted
    {
      get
      {
        if ( mSorted == null )
          BuildSorted();
        return mSorted; 
      }
    }

    void BuildSorted()
    {
      mSorted = new List<Bin>();
      foreach( var lKV in mMap )
        mSorted.Add( new Bin(lKV.Key, lKV.Value) );

      mSorted.Sort( (x,y) => x.Frequency.CompareTo(y.Frequency) ) ;
    }

    void CalculateShannonEntropy()
    {
      mShannonEntropy = 0;
      foreach (var lItem in mMap)
      {
        var lF = (double)lItem.Value / Count;
        mShannonEntropy -= lF * (Math.Log(lF) / Math.Log(2));
      }
    }

    public IEnumerator<KeyValuePair<SYM, int>> GetEnumerator()
    {
        return mMap.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        //forces use of the non-generic implementation on the Values collection
        return mMap.GetEnumerator();
    }

    Dictionary<SYM,int> mMap = new Dictionary<SYM,int>() ;

    double? mShannonEntropy = null;
    List<Bin> mSorted = null ;
  }


  public class SymbolString<SYM> where SYM : Symbol
  {
    public SymbolString( IEnumerable<SYM> aSymbols )
    {
      Symbols.AddRange(aSymbols);
    }

    public override string ToString()
    {
      List<string> lAll = new List<string>();

      foreach( Symbol lSymbol in Symbols )
        lAll.Add(lSymbol.ToString() );  

      return "{" + String.Join( "", lAll ) + "}";
    }

    public SymbolString<SYM> Copy()
    {
      return new SymbolString<SYM>( Symbols.ConvertAll( s => s.Clone() ).Cast<SYM>() ) ;
    }

    public Histogram<SYM> Histogram
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
      mHistogram = new Histogram<SYM>(); 
      Symbols.ForEach( s => mHistogram.Add( s ) );
    }

    Histogram<SYM> mHistogram = null ;
  }

  public class LexicalSignal<SYM> : Signal where SYM : Symbol
  {
    public LexicalSignal( IEnumerable<SYM> aSymbols )
    {
      String = new SymbolString<SYM>(aSymbols);
    }

    public override string ToString() => $"{base.ToString()} {String.ToString()}";

    public override Signal Copy()
    {
      return new LexicalSignal<SYM>( String.Copy() ) ;
    }

    LexicalSignal( SymbolString<SYM> aString )
    {
      String = aString ;
    }

    public SymbolString<SYM> String ;

    public Histogram<SYM> Histogram => String.Histogram ;


  }


}
