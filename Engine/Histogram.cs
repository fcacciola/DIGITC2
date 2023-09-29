using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Schema;

using MathNet.Numerics.Statistics;

using OxyPlot;
using OxyPlot.Series;

namespace DIGITC2
{
  public class Samples : List<double>
  {  
    public Samples() {}
    public Samples( IEnumerable<double> aSamples ) { this.AddRange(aSamples); }

    public Samples Transformed( Func<double, double> f ) 
    { 
      return new Samples( this.ConvertAll( s => f(s) ) );
    }
  }

  public class DTable
  {
    public DTable() { } 

    public DTable( Samples aX, Samples aY) 
    {
      X = aX;  
      Y = aY;
    }

    public static DTable FromY( Samples aY )
    {
      DTable rRS  = new DTable();

      rRS.Y = aY ;

      for ( int r = 1; r <= aY.Count ; ++ r )      
        rRS.X.Add(r);

      return rRS ;
    }

    public Samples X = new Samples() ;
    public Samples Y = new Samples() ;

    public Plot CreatePlot(Plot.Options aOptions = null)
    {
      Plot rPlot = new Plot(aOptions);

      var lSeries = new LineSeries();

      for( int i = 0; i < X.Count ; ++ i )
        lSeries.Points.Add(new DataPoint(X[i], Y[i]));
            
      rPlot.AddSeries(lSeries);

      return rPlot;
    }

    public DTable Transformed( Func<double, double> f ) 
    { 
      return new DTable(X.Transformed(f), Y.Transformed(f));
    }

    public DTable ToLog() => Transformed( s => Math.Log10(s) );  
  }

  public class Histogram
  {
    public class Entry
    {
      public Entry( Symbol aSymbol, string aKey, double aXValue ) { Symbol = aSymbol ; Key = aKey ; XValue = aXValue ; }  

      public Symbol Symbol ;
      public string Key ;
      public double XValue ; 

      public override string ToString() => $"{Key}|{XValue}";
    }

    class Bucket
    {
      internal Bucket( Entry aEntry, int aCount ) { Entry = aEntry ; Count = aCount ; } 

      internal Entry Entry ;
      internal int   Count  ;

      public override string ToString() => $"{Entry}:{Count}";
    }

    public Histogram( IEnumerable<Entry> aEntries )
    {
      foreach( Entry lEntry in aEntries )  
       Add(lEntry);

      BuildTable();
    }


    public Samples GetRankSize()
    {
      return new Samples(Table.Y.OrderByDescending( s => s) );
    }

    void Add( Entry aSample )
    {
      if ( mMap.ContainsKey( aSample.Key ) )  
      {
        mMap[aSample.Key].Count++;
      }
      else
      {
        mMap.Add( aSample.Key, new Bucket( aSample, aSample.Symbol != null ? 1 : 0 ) ); 
      }
    }

    void BuildTable()
    {
      foreach( var lBucket in mMap.Values.OrderBy( b => b.Entry.XValue ) )
      {
        Table.X.Add( lBucket.Entry.XValue );
        Table.Y.Add( lBucket.Count );
      }
    }

    Dictionary<string,Bucket> mMap = new Dictionary<string,Bucket>();

    public DTable Table = new DTable();
  }

  public class Histogram2
  {
    public class Params
    {
      public Params( int aBucketCount, double aLower, double aUpper )
      {
        BucketCount = aBucketCount;
        Lower       = aLower; 
        Upper       = aUpper;
      }

      public readonly int    BucketCount ;
      public readonly double Lower ;
      public readonly double Upper ;
    }

    public Histogram2( IEnumerable<double> aData, Params aParams )
    {
      var lRep = new MathNet.Numerics.Statistics.Histogram( aData, aParams.BucketCount, aParams.Lower, aParams.Upper );

      for (var col = 0; col < lRep.BucketCount; col++)
      {
        var lBucket = lRep[col]  ;

        var lMidpoint = ( lBucket.LowerBound + lBucket.UpperBound ) / 2 ;
        
        Table.X.Add(lMidpoint) ;
        Table.Y.Add(lBucket.Count) ;
      }
    }

    public Samples GetRankSize()
    {
      return new Samples(Table.Y.OrderByDescending( s => s).Distinct());
    }


    public DTable Table = new DTable();
  }




}
