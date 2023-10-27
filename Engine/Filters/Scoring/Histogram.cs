using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Media.Converters;
using System.Xml.Schema;

using MathNet.Numerics.Statistics;

using OxyPlot;
using OxyPlot.Series;

namespace DIGITC2
{
  public class Stats : IWithState
  {
    public Stats() 
    {
    } 

    public State GetState() 
    {
      State rS = new State("Stats") ;

      rS.Add( State.With("Zipf_Likelihood"  , Zipf_Likelihood  ) ) ;
      rS.Add( State.With("Kurtosis"         , Kurtosis         ) ) ;
      rS.Add( State.With("Maximum"          , Maximum          ) ) ;
      rS.Add( State.With("Minimum"          , Minimum          ) ) ;
      rS.Add( State.With("Mean"             , Mean             ) ) ;
      rS.Add( State.With("Variance"         , Variance         ) ) ;
      rS.Add( State.With("StandardDeviation", StandardDeviation) ) ;
      rS.Add( State.With("Skewness"         , Skewness         ) ) ;

      return rS ;
    }

    public double Zipf_Likelihood   = 0 ;
    public double Kurtosis          = 0 ;
    public double Maximum           = 0 ;
    public double Minimum           = 0 ;
    public double Mean              = 0 ;
    public double Variance          = 0 ;
    public double StandardDeviation = 0 ;
    public double Skewness          = 0 ;
  }

  public abstract class SampleSource
  {
    public abstract string Key { get ; }

    public override string ToString() => Key ;
  }

  public sealed class WaveValueSampleSource : SampleSource
  {
    public WaveValueSampleSource( int aIdx ) { Idx = aIdx ; }

    public int Idx ;

    public override string Key => Idx.ToString() ;
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

  public class Distribution 
  {  
    public Distribution() {}

    public Distribution( IEnumerable<Sample> aSamples )  {  Samples.AddRange(aSamples); }

    public Distribution Transformed( Func<double, double> f ) 
    { 
      return new Distribution( Samples.ConvertAll( s => s.Transformed(f)));
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

    public void Add( Sample aSample ) 
    {
      Samples.Add(aSample) ;
    }

    public List<Sample> Samples = new List<Sample>() ;

    public IEnumerable<double> Values => Samples.Select( s => s.Value ) ;

    public int Count => Samples.Count ;

    public Sample this [int i ]  => Samples[i] ;  
  }

  public class DPoint
  {
    public DPoint( Sample aX, double aY) { X = aX ; Y = aY ; }

    public DataPoint ToPlot() => new DataPoint(X.Value,Y);

    public DPoint Transformed( Func<double, double> f ) 
    {
      return new DPoint( X.Transformed(f), f(Y) );
    }

    public DPoint Copy() => new DPoint(X.Copy(),Y);

    public override string ToString() => $"({X},{Y})]";

    public Sample X ;
    public double Y ;
  }

  public class DTable
  {
    public DTable() { } 

    public DTable( IEnumerable<DPoint> aPoints ) { Points.AddRange(aPoints); }

    public Plot CreatePlot(Plot.Options aOptions = null)
    {
      Plot rPlot = new Plot(aOptions);

      DataPointSeries lSeries = ( aOptions.Type == Plot.Options.TypeE.Lines ? new LineSeries() as DataPointSeries : new LinearBarSeries() as DataPointSeries ) ;
      
      lSeries.Points.AddRange( Points.ConvertAll( p => p.ToPlot() ));
          
      rPlot.AddSeries(lSeries);

      return rPlot;
    }

    public DTable Transformed( Func<double, double> f ) 
    { 
      return new DTable(Points.ConvertAll( p => p.Transformed(f) ));  
    }

    public DTable ToLog() => Transformed( s => Math.Log(s) );  

    public DTable ToRankSize()
    {
      DTable rR = new DTable( Points.OrderByDescending( p => p.Y) ) ;

      for ( int lRank = 0 ;  lRank < Points.Count ; ++ lRank )
        rR.Points[lRank].X.Value = lRank ;

      return rR ;
    }

    public List<DPoint> Points = new List<DPoint>();

  }

  public class Histogram
  {
    class Bucket
    {
      internal Bucket( Sample aSample, int aCount ) { Sample = aSample ; Count = aCount ; } 

      internal Sample Sample ;
      internal int    Count  ;

      public override string ToString() => $"{Sample}:{Count}";
    }

    public Histogram( Distribution aDist ) : this(aDist.Samples)
    {
    }

    public Histogram( IEnumerable<Sample> aSamples )
    {
      foreach( Sample lSample in aSamples )  
       Add(lSample);

      BuildTable();
    }

    void Add( Sample aSample )
    {
      if ( mMap.ContainsKey( aSample.Source.Key ) )  
      {
        mMap[aSample.Source.Key].Count++;
      }
      else
      {
//        mMap.Add( aPoint.X.Source.Key, new Bucket( aPoint, aPoint.X.Source.Symbol != null ? 1 : 0 ) ); 
        mMap.Add( aSample.Source.Key, new Bucket( aSample, 1 ) ); 
      }
    }

    void BuildTable()
    {
      foreach( var lBucket in mMap.Values.OrderBy( b => b.Sample.Value ) )
      {
        Table.Points.Add( new DPoint(lBucket.Sample, lBucket.Count) );
      }
    }

    Dictionary<string,Bucket> mMap = new Dictionary<string,Bucket>();

    public DTable Table = new DTable();
  }

}
