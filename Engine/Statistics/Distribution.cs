using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Media.Converters;
using System.Xml.Schema;

using MathNet.Numerics.Statistics;

using Newtonsoft.Json;

using OxyPlot;
using OxyPlot.Series;

namespace DIGITC2
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
    public FakeSampleSource( string aKey ) { mKey = aKey ; }  

    public override string Key => mKey ;
    
    public override int HistogramCount => 0 ;

    string mKey ;
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

    public Distribution( IEnumerable<Sample> aSamples )
    { 
      mSamples.AddRange(aSamples); 
      mValues .AddRange( mSamples.Select( s => s.Value ) ) ;
    }

    public Distribution Transformed( Func<double, double> f ) 
    { 
      return new Distribution( Samples.Select( s => s.Transformed(f)));
    }

    //public void FillBaseStats()
    //{
    //  var lDescriptiveStatistics = new DescriptiveStatistics(this);

    //  Stats.Kurtosis          = lDescriptiveStatistics.Kurtosis;
    //  Stats.Maximum           = lDescriptiveStatistics.Maximum;
    //  Stats.Minimum           = lDescriptiveStatistics.Minimum;
    //  Stats.Mean              = lDescriptiveStatistics.Mean;
    //  Stats.Variance          = lDescriptiveStatistics.Variance;
    //  Stats.StandardDeviation = lDescriptiveStatistics.StandardDeviation;
    //  Stats.Skewness          = lDescriptiveStatistics.Skewness;
    //}

    //public Stats Stats = new Stats();

    //public enum SortStateE { Unsorted, Ascending, Descending } ;

    //public SortStateE SortState = SortStateE.Unsorted ;



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
    public IReadOnlyList<Sample> Samples => mSamples.AsReadOnlyList();
    public IReadOnlyList<double> Values  => mValues .AsReadOnlyList();

    [JsonProperty]
    List<Sample> mSamples = new List<Sample>() ;

    List<double> mValues  = new List<double>() ; 
  }

}
