﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.TextFormatting;

using NWaves.Signals;

namespace DIGITC2
{
  public abstract class Symbol : IWithState
  {
    public Symbol( int aIdx ) { Idx = aIdx ; }

    public abstract Symbol Copy();

    public abstract string Type            { get; }
    public abstract string Meaning         { get; }
    public abstract bool   UseCompactState { get; }
    public abstract double Value           { get; }

    public int Idx ;

    public virtual State GetState()
    {
      return State.With($"[{Idx}]",Meaning,UseCompactState) ; 
    }

    public static bool Equals( Symbol aLHS, Symbol aRHS)
    {
      if (aLHS is null)
      {
        if (aRHS is null)
            return true;

        return false;
      }
      else
      {
        if (aRHS is null)
             return false;
        else return aLHS.Meaning == aRHS.Meaning ; 
      }
    }

    public override int GetHashCode() => Meaning.GetHashCode();

    public override bool Equals( object aRHS) => Equals( this, aRHS as Symbol ); 

    public static bool operator ==(Symbol lhs, Symbol rhs) => Equals(lhs,rhs) ;
    public static bool operator !=(Symbol lhs, Symbol rhs) => !(lhs == rhs);

    public override string ToString() => Meaning ;
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

    public override double Value => Amplitude ;

    public override Symbol Copy() {  return new GatedSymbol( Idx, Amplitude, SamplingRate, Pos, Length ); }  

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

    public override Symbol Copy() { return new BitSymbol( Idx, One, View?.Copy()  as GatedSymbol ); }  

    public override string Meaning => One ? "1" : "0" ;

    public override bool UseCompactState => true ;

    public override double Value => One ? 1.0 : 0.0 ;

    public bool One ;

    public GatedSymbol View ;
  }

  public class ByteSymbol : Symbol
  {
    public ByteSymbol( int aIdx, byte aByte ) : base(aIdx) { Byte = aByte ; }

    public override string Type => "Byte" ;

    public override Symbol Copy () { return new ByteSymbol( Idx, Byte ); }  

    public override string Meaning => $"[{Byte.ToString():x}]" ;

    public override bool UseCompactState => true ;

    public override double Value => Convert.ToDouble(Byte);

    public byte Byte ;
  }

  public class ArraySymbol : Symbol
  {
    public ArraySymbol( int aIdx, List<Symbol> aSymbols ) : base(aIdx) { Symbols = aSymbols ; }

    public override string Type => "Token" ;

    public override Symbol Copy() { return new ArraySymbol( Idx, Symbols.ConvertAll( s => s.Copy() ) ); }  

    public override string Meaning => GetState().ToString() ;

    public override bool UseCompactState => false ;

    public int UpperBound => Math.Max(Symbols.Count,Context.MaxWordLength) ;

    public override double Value => Symbols.Count ;

    public override State GetState()
    {
      State rState = new State($"[{Idx}]");
      rState.Add( State.From("Tokens", Symbols) );
      return rState;
    }

    public List<Symbol> Symbols ;

    public List<SYM> GetSymbols<SYM>() => Symbols.Cast<SYM>().ToList();

  }

  public class WordSymbol : Symbol
  {
    public WordSymbol( int aIdx, string aText ) : base(aIdx) { Word = aText ; }

    public override string Type => "Word" ;

    public override Symbol Copy() { return new WordSymbol( Idx, Word ); }  

    public override string Meaning => $"[{Word}]" ;

    public override bool UseCompactState => false ;

    public int UpperBound => Math.Max(Word.Length,Context.MaxWordLength) ;

    public override double Value => Word.Length ;

    public string Word ;
  }


}