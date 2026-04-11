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
  public class Histogram
  {
    class Bucket
    {
      internal Bucket( Sample aSample, int aCount ) { Sample = aSample ; Count = aCount ; } 

      internal Sample Sample ;
      internal int    Count  ;

      public override string ToString() => $"{Sample}:{Count}";
    }

    static public Histogram From<SYMBOL>( List<SYMBOL> aSymbols, GetSymbolMeaningAndValue GMV ) where SYMBOL : Symbol
    {
      return new Histogram( aSymbols.ConvertAll( s => s.ToSample(GMV) ) );
    }

    public Histogram( List<double> aValues ) : this ( new Distribution(aValues) ) {}

    public Histogram( List<Sample> aSamples ) : this ( new Distribution(aSamples) ) {}

    public Histogram( Distribution aDist ) 
    {
      mDistribution = aDist;

      foreach( Sample lSample in mDistribution.Samples )  
       Add(lSample);

      BuildTable();
    }

    public Gmm Gmm 
    {
      get 
      {
        if ( mGmm == null ) 
          mGmm = GmmFitter.Fit(mDistribution.Values);
        return mGmm ;
      }
    }

    public void Plot( string aName )
    {
      if ( DContext.Session.Settings.GetBool("OutputDetails") )
      { 
        Table.CreatePlot(Plotter.Options.Bars)?.SaveSVG(DContext.Session.OutputFile($"{aName}_Histogram.svg"));
        Gmm? .CreatePlot()                    ?.SaveSVG(DContext.Session.OutputFile($"{aName}_GMM.svg"));
      }
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

    Distribution mDistribution ;
    Gmm          mGmm = null;

  }

}
