using System ;
using System.Collections.Generic ;
using System.Linq;
using System.IO;

using Newtonsoft.Json;

using OxyPlot;
using OxyPlot.Series;

namespace DIGITC2_ENGINE
{
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

  [JsonObject(MemberSerialization.OptIn)]
  public class DTable
  {
    public DTable() { } 

    public DTable( IEnumerable<DPoint> aPoints )
    {
      mPoints.AddRange(aPoints); 

      Setup();
    }

    public Plot CreatePlot(Plot.Options aOptions = null)
    {
      Plot rPlot = new Plot(aOptions);

      DataPointSeries lSeries = ( aOptions.Type == Plot.Options.TypeE.Lines ? new LineSeries() as DataPointSeries : new LinearBarSeries() as DataPointSeries ) ;
      
      lSeries.Points.AddRange( Points.Select( p => p.ToPlot() ));
          
      rPlot.AddSeries(lSeries);

      return rPlot;
    }

    public DTable Transformed( Func<double, double> f ) 
    { 
      return new DTable(Points.Select( p => p.Transformed(f) ));  
    }

    public DTable ToLog() => Transformed( s => Math.Log(s) );  

    public DTable Normalized()
    {
      List<DPoint> lPoints = new List<DPoint>();

      double lMaxY = 0 ;
      mPoints.ForEach( p => lMaxY = Math.Max(lMaxY,p.Y ) ) ;

      mPoints.ForEach( p => lPoints.Add( new DPoint(p.X.Copy(), p.Y / lMaxY) ) ) ; 

      return new DTable(lPoints);
    }

    public DTable ToRankSize()
    {
      var lPointsCopy = Points.Select( p => p.Copy() ) ;

      DTable rR = new DTable( lPointsCopy.OrderByDescending( p => p.Y) ) ;

      for ( int lRank = 0 ;  lRank < Points.Count ; ++ lRank )
        rR.Points[lRank].X.Value = lRank ;

      return rR ;
    }

    public void Save( string aFilename ) 
    {
      File.WriteAllText( aFilename, this.ToJSON() );
    }

    static public DTable FromFile( string aFilename ) 
    {
      string lJson = File.ReadAllText( aFilename );

      DTable rR = JsonConvert.DeserializeObject<DTable>( lJson );

      rR.Setup();

      return  rR ; 
    }

    public IReadOnlyList<DPoint> Points  => mPoints .AsReadOnlyList();
    public IReadOnlyList<double> XValues => mXValues.AsReadOnlyList();
    public IReadOnlyList<double> YValues => mYValues.AsReadOnlyList();

    void Setup()
    {
      mXValues = new List<double>();
      mYValues = new List<double>();

      mXValues.AddRange( Points.Select( p => p.X.Value) );
      mYValues.AddRange( Points.Select( p => p.Y) );
    }

    [JsonProperty] List<DPoint> mPoints  = new List<DPoint>();

    List<double> mXValues = null ;
    List<double> mYValues = null ;

  }


}
