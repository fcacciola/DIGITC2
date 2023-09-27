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
    public abstract bool   UseCompactState { get; }

    public int Idx ;

    public virtual State GetState()
    {
      return State.With($"[{Idx}]",Meaning,UseCompactState) ; 
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

    public override bool UseCompactState => false ;

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

    public override bool UseCompactState => true ;

    public bool One ;

    public GatedSymbol View ;
  }

  public class ByteSymbol : Symbol
  {
    public ByteSymbol( int aIdx, byte aByte ) : base(aIdx) { Byte = aByte ; }

    public override string Type => "Byte" ;

    public override object Clone() { return new ByteSymbol( Idx, Byte ); }  

    public override string Meaning => $"[{Byte.ToString():x}]" ;

    public override bool UseCompactState => true ;

    public byte Byte ;
  }

  public class TokenSymbol : Symbol
  {
    public TokenSymbol( int aIdx, Token aToken ) : base(aIdx) { Token = aToken ; }

    public override string Type => "Token" ;

    public override object Clone() { return new TokenSymbol( Idx, Token.Copy() ); }  

    public override string Meaning => GetState().ToString() ;

    public override bool UseCompactState => false ;

    public override State GetState()
    {
      State rState = new State($"[{Idx}]");
      rState.Add( Token.GetState() );
      return rState;
    }

    public Token Token ;
  }

  public class WordSymbol : Symbol
  {
    public WordSymbol( int aIdx, string aText ) : base(aIdx) { Word = aText ; }

    public override string Type => "Word" ;

    public override object Clone() { return new WordSymbol( Idx, Word ); }  

    public override string Meaning => $"[{Word}]" ;

    public override bool UseCompactState => false ;

    public string Word ;
  }


}
