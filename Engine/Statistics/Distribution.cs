using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Schema;

using MathNet.Numerics.Statistics;

using Newtonsoft.Json;

using OxyPlot;
using OxyPlot.Series;

namespace DIGITC2_ENGINE
{
  
  public class SampleSource // This is not abtract because the Json Deserializer cannot handle it
  {
    public virtual string Key => "";

    public override string ToString() => Key ;

    public virtual int HistogramCount => 1 ;
  }

  public sealed class WaveValueSampleSource : SampleSource
  {
    public WaveValueSampleSource( int aIdx ) { Idx = aIdx ; }

    public int Idx ;

    public override string Key => Idx.ToString() ;
  }

  public sealed class FakeSampleSource : SampleSource
  {
    public FakeSampleSource( string aKey, int aCount ) { mKey = aKey ; mCount = aCount ; }  

    public override string Key => mKey ;
    
    public override int HistogramCount => mCount ;

    string mKey ;
    int    mCount ;
  }

  public sealed class SymbolSampleSource : SampleSource
  {
    public SymbolSampleSource( Symbol aSymbol, string aKey ) { Symbol = aSymbol ; mKey = aKey ; }  

    public Symbol Symbol ;

    public override string Key => mKey ;
    
    string mKey ;
  }

  public class Sample
  {
    public Sample( SampleSource aSource, double aValue ) { Source = aSource ; Value = aValue ; }  

    public Sample Transformed( Func<double, double> f ) 
    {
      return new Sample( Source, f(Value ) );
    }

    public SampleSource Source ;
    public double       Value ; 

    public Sample Copy() => new Sample(Source, Value);  

    public override string ToString() => $"[{Source}|{Value}]";
  }

  [JsonObject(MemberSerialization.OptIn)]
  public class Distribution 
  {  
    public Distribution() {}

    public Distribution( IEnumerable<double> aValues )
    { 
      mSamples.AddRange( aValues.Select( v => new Sample( new FakeSampleSource($"{v}",1), v ) ) ) ;
      mValues .AddRange( aValues ) ;
    }

    public Distribution( IEnumerable<Sample> aSamples )
    { 
      mSamples.AddRange(aSamples); 
      mValues .AddRange( mSamples.Select( s => s.Value ) ) ;
    }

    public Distribution Transformed( Func<double, double> f ) 
    { 
      return new Distribution( Samples.Select( s => s.Transformed(f)));
    }

    string CreateFakeKey( double i ) => $"{i}";

    public Distribution ExtendedWithBaseline( double aLower, double aHigher, double aStep, Func<double,string> CreateKey = null )
    {
      var CK = CreateKey ?? CreateFakeKey ;

      List<Sample> lSamples = new List<Sample>();
      for( double v = aLower; v < aHigher; v += aStep )  
        lSamples.Add( new Sample( new FakeSampleSource( CK(v), 0 ) ,v));

      lSamples.AddRange(Samples);

      return new Distribution( lSamples );
    }

    public int Count => Samples.Count ;

    public Sample this [int i ]  => Samples[i] ;  

    public void Save( string aFilename ) 
    {
      File.WriteAllText( aFilename, this.ToJSON() );
    }

    static public Distribution FromFile( string aFilename ) 
    {
      string lJson = File.ReadAllText( aFilename );

      return JsonConvert.DeserializeObject<Distribution>( lJson );  
    }

    public IReadOnlyList<Sample> Samples => mSamples.AsReadOnly();
    public IReadOnlyList<double> Values  => mValues .AsReadOnly();

    [JsonProperty]
    List<Sample> mSamples = new List<Sample>() ;

    List<double> mValues  = new List<double>() ; 
  }

}
