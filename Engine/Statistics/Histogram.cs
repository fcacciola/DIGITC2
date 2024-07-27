using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Schema;

using MathNet.Numerics.Statistics;

using OxyPlot;
using OxyPlot.Series;

namespace DIGITC2_ENGINE
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
        mMap.Add( aSample.Source.Key, new Bucket( aSample, aSample.Source.HistogramCount ) ); 
      }
    }

    void BuildTable()
    {
      List<DPoint> lDPs = new List<DPoint>();

      foreach( var lBucket in mMap.Values.OrderBy( b => b.Sample.Value ) )
      {
        lDPs.Add( new DPoint(lBucket.Sample, lBucket.Count) );
      }

      Table = new DTable(lDPs);
    }

    Dictionary<string,Bucket> mMap = new Dictionary<string,Bucket>();

    public DTable Table = null;
  }

}
