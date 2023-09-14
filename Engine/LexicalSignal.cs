using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Signals;

namespace DIGITC2
{
  public abstract class Symbol : ICloneable
  {
    public Symbol( int aIdx ) { Idx = aIdx ; }

    public abstract object Clone() ;  

    public int Idx ;

    public virtual string Meaning => ToString();
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

  public class TextSymbol : Symbol
  {
    public TextSymbol( int aIdx, string aText ) : base(aIdx) { Text = aText ; }

    public override object Clone() { return new TextSymbol( Idx, Text ); }  

    public override string ToString() => Text ;

    public string Text ;
  }

  public abstract class LexicalSignal : Signal
  {
    public override string ToString()
    {
      List<string> lAll = new List<string>();

      foreach( Symbol lSymbol in EnumSymbols )
        lAll.Add(lSymbol.ToString() );  

      return String.Join( "", lAll );
    }

    public abstract IEnumerable<Symbol> EnumSymbols { get ; }
  }

  public class GenericLexicalSignal<SYM> : LexicalSignal where SYM : Symbol
  {
    public GenericLexicalSignal( IEnumerable<SYM> aSymbols )
    {
      Symbols.AddRange(aSymbols);
    }

    public override Signal Copy()
    {
      return new GenericLexicalSignal<SYM>( Symbols.ConvertAll( s => s.Clone() ).Cast<SYM>() ) ;
    }

    public override Signal MergeWith( IEnumerable<Signal> aSlices, Context aContext )
    { 
      if ( aSlices.Count() == 0 )
        return this ; 

      List<SYM> lAllSymbos = new List<SYM> ();

      lAllSymbos.AddRange(Symbols);

      foreach( GenericLexicalSignal<SYM> lSlice in aSlices.Cast<GenericLexicalSignal<SYM>>() ) 
        lAllSymbos.AddRange(lSlice.Symbols);

      var rS = new GenericLexicalSignal<SYM>(lAllSymbos );  

      rS.Assign( this );

      return rS;  
    }

    public override IEnumerable<Symbol> EnumSymbols => Symbols.Cast<Symbol>() ;

    public List<SYM> Symbols = new List<SYM>();

    public override void Render ( TextRenderer aRenderer, RenderOptions aOptions )
    {
      aRenderer.Render ( ToString(), aOptions );
    }
  }


}
