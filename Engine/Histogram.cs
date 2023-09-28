using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Schema;

using OxyPlot;
using OxyPlot.Series;

namespace DIGITC2
{

  public class Histogram
  {
    public Histogram( IEnumerable<double> aData, int aBucketCount = 0 )
    {
      int lBucketCount = aBucketCount > 0 ? aBucketCount : aData.Count();
      mRep = new MathNet.Numerics.Statistics.Histogram( aData, lBucketCount );
    }

    public Plot CreatePlot(Plot.Options aOptions)
    {
      Plot rPlot = new Plot(aOptions);

      var lSeries = new LinearBarSeries();

      for (var col = 0; col < mRep.BucketCount; col++)
      {
        var lBucket = mRep[col]  ;

        var lMidpoint = ( lBucket.LowerBound + lBucket.UpperBound ) / 2 ;
        
        lSeries.Points.Add(new DataPoint(lMidpoint, lBucket.Count) ) ;
      }
            
      rPlot.AddSeries(lSeries);

      return rPlot;
    }

    MathNet.Numerics.Statistics.Histogram mRep ;
  }




}
