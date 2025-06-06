﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Signals;

namespace DIGITC2_ENGINE
{
  public abstract class Symbol 
  {
    public Symbol( int aIdx ) { Idx = aIdx ; }

    public abstract Symbol Copy();

    public abstract string Type   { get; }
    public abstract string Meaning{ get; }
    public abstract double Value  { get; }

    public int Idx ;

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

    public virtual Sample ToSample() => new Sample( new SymbolSampleSource(this, Meaning), Value)  ;
  }

  public class PulseStep
  {
    public PulseStep( float aAmplitud, int aStart, int aEnd ) 
    {
      Amplitude = aAmplitud;
      Start     = aStart; 
      End       = aEnd;
    }


    public PulseStep Copy() {  return new PulseStep( Amplitude, Start, End ); }  

    public void DumpSamples( List<float> aSamples )
    {
      for( int i = 0; i < Length; i++ ) 
        aSamples.Add(Amplitude);
    }

    public int Length => End - Start ;

    public float Amplitude ;
    public int   Level => (int)(Amplitude * 100) ;
    public int   Start ;
    public int   End ; 
  }

  public class PulseSymbol : Symbol
  {
    public PulseSymbol( int aIdx, int aStart, int aEnd, IEnumerable<PulseStep> aSteps ) : base(aIdx)
    {
      Start = aStart;
      End   = aEnd;

      Steps.AddRange(aSteps) ; 

      MaxAmplitude = Steps.Select( p => p.Amplitude ).Max() ;
    }

    public int    Length => End - Start ;

    public double StartTime => (double)Start  / (double)SIG.SamplingRate;
    public double EndTime   => (double)End    / (double)SIG.SamplingRate;
    public double Duration  => (double)Length / (double)SIG.SamplingRate;

    public override string Type => "Pulse" ;

    public override string Meaning => $"{Duration:F2} pulse at {(double)Start/(double)SIG.SamplingRate:F2} " ;

    public override double Value => MaxAmplitude ;

    public override Symbol Copy()
    { 
      var lStepsCopy = new List<PulseStep>() ;  
      Steps.ForEach( s => lStepsCopy.Add( s.Copy() ) );  
      return new PulseSymbol( Idx, Start, End, lStepsCopy ); 
    }  

    public void DumpSamples( List<float> aSamples )
    {
      int lC = aSamples.Count ;
      for( int i = lC ; i < Start ; i++ )
        aSamples.Add(0);

      Steps.ForEach( s => s.DumpSamples( aSamples ) ); 
    }

    public override Sample ToSample() => new Sample( new SymbolSampleSource(this, $"{Duration:F2}"), Duration)  ;

    public int             MaxLevel => (int)(MaxAmplitude*100) ;

    public float           MaxAmplitude ;
    public int             Start ;
    public int             End ; 
    public List<PulseStep> Steps = new List<PulseStep>();
  }

  public class BitSymbol : Symbol
  {
    public BitSymbol( int aIdx, bool? aOne, double aLikelihood, PulseSymbol aView = null ) : base(aIdx) { One = aOne ; Likelihood = aLikelihood ; View = aView ; }

    public override string Type => "Bit" ;

    public override Symbol Copy() { return new BitSymbol( Idx, One, Likelihood, View?.Copy()  as PulseSymbol ); }  

    public override string Meaning => ( One.HasValue ? ( One.Value ? "1" : "0" ) : "?" ) ;

    public override double Value => One.GetValueOrDefault(false) ? 1.0 : 0.0 ;

    public bool? One ;

    public double Likelihood ;

    public PulseSymbol View ;
  }

  public class BitBagSymbol : Symbol
  {
    public BitBagSymbol( int aIdx, List<BitSymbol> aBits, double aLikelihood ) : base(aIdx) { Bits = aBits; Likelihood = aLikelihood ; }

    public override string Type => "BitList" ;

    public override Symbol Copy() { return new BitBagSymbol( Idx, Bits.ConvertAll( b => b.Copy() as BitSymbol), Likelihood ) ; }  

    public override string Meaning => string.Join("|", Bits.ConvertAll( s => s.Meaning ) );
    
    public override double Value => Bits.Count;

    public List<BitSymbol> Bits ;

    public double Likelihood ;
  }

  public class ByteSymbol : Symbol
  {
    public ByteSymbol( int aIdx, byte aByte, double aLikelihood = 1.0) : base(aIdx) { Byte = aByte ; Likelihood = aLikelihood ; }

    public override string Type => "Byte" ;

    public override Symbol Copy () { return new ByteSymbol( Idx, Byte, Likelihood ); }  

    public override string Meaning => $"[{Byte.ToString():x}]" ;

    public override double Value => Convert.ToDouble(Byte);

    public double Likelihood ;

    public byte Byte ;
  }

  public class ArraySymbol : Symbol
  {
    public ArraySymbol( int aIdx, List<Symbol> aSymbols ) : base(aIdx) { Symbols = aSymbols ; }

    public override string Type => "Token" ;

    public override Symbol Copy() { return new ArraySymbol( Idx, Symbols.ConvertAll( s => s.Copy() ) ); }  

    public override string Meaning => string.Join("|", Symbols.ConvertAll( s => s.Meaning ) );
    
    public int UpperBound => Math.Max(Symbols.Count,DContext.Session.Args.GetInt("MaxWordLength")) ;
    
    public override double Value => Symbols.Count ;

    public List<Symbol> Symbols ;

    public List<SYM> GetSymbols<SYM>() => Symbols.Cast<SYM>().ToList();

    public override Sample ToSample() => new Sample( new SymbolSampleSource(this,  $"{Symbols.Count}"), Symbols.Count) ;

  }

  public class WordSymbol : Symbol
  {
    public WordSymbol( int aIdx, string aWord ) : base(aIdx) { Word = aWord ; }

    public override string Type => "Word" ;

    public override Symbol Copy() { return new WordSymbol( Idx, Word ); }  

    public override string Meaning => $"[{Word}]" ;

    public int UpperBound => Math.Max(Word.Length,DContext.Session.Args.GetInt("MaxWordLength")) ;

    public override double Value => Word.Length ;

    public string Word ;

    public override Sample ToSample() => new Sample( new SymbolSampleSource(this, Word), Idx) ;
  }

  public class TextSymbol : Symbol
  {
    public TextSymbol( int aIdx, string aText ) : base(aIdx) { Text = aText ; }

    public override string Type => "Text" ;

    public override Symbol Copy() { return new WordSymbol( Idx, Text ); }  

    public override string Meaning => Text ;

    public override double Value => Text.Length ;

    public string Text ;

    public override Sample ToSample() => new Sample( new SymbolSampleSource(this, Text), Idx) ;
  }


  public static class SymbolExtensions
  {
    public static List<double> GetValues( this IEnumerable<Symbol> aSymbols )
    {
      List<double> rValues = new List<double>();
      foreach( Symbol lSymbol in aSymbols) 
        rValues.Add( lSymbol.Value ) ;
      return rValues ;
    }
  }
}
