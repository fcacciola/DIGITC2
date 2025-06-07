using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DocumentFormat.OpenXml.Drawing;

using MathNet.Numerics.Integration;
using MathNet.Numerics.Statistics;

using Newtonsoft.Json.Serialization;

using NWaves.Operations;
using NWaves.Signals;

namespace DIGITC2_ENGINE
{
  public class DExtremePoint
  {
    public DPoint Value ;
    public int    Idx ;
    public double Height ;
    public bool   IsPeak ;

    public bool IsValley => !IsPeak ;

    public override string ToString() => $"[{(IsPeak?"Peak":"Valley")}-{Value}@{Idx}({Height})]";
  }

  public class ExtremePointsFinder
  {
    public ExtremePointsFinder( IEnumerable<DPoint> aDistribution ) 
    { 
      mDistribution.Add( new DPoint(null, 0) ) ;
      if ( aDistribution.Count() > 0 )
           mDistribution.AddRange( aDistribution );  
      else mDistribution.Add( new DPoint(null, 0) ); 
      mDistribution.Add( new DPoint(null, 0) );
    }  

    public static List<DExtremePoint> Find( IEnumerable<DPoint> aDistribution )
    {
      var PF = new ExtremePointsFinder(aDistribution);
      PF.DoFind();
      return PF.mExtremePoints ;
    }

    void DoFind() 
    {
      DContext.WriteLine("Finding peaks...");
      DContext.Indent();

      // Filter consecutive equal values
      // Also, pad with zeros at both ends

      List<DPoint> lFiltered = new List<DPoint>();
      lFiltered.Add( new DPoint(null,0) );

      double lPrevY = 0 ;

      foreach( var lDP in mDistribution )
      {
        if ( lDP.Y != lPrevY )
          lFiltered.Add( lDP ); 
        lPrevY = lDP.Y ;  
      }
      lFiltered.Add( new DPoint(null,0) );


      int lSC = lFiltered.Count;

      int lSL = lSC - 1 ;
      var lPrev = lFiltered[0];
      for( int i = 1; i < lSL ; ++ i )
      {
        var lValue = lFiltered[i];
        var lNext  = lFiltered[i+1] ;

        if ( lValue.Y > lPrev.Y && lValue.Y > lNext.Y )
          mExtremePoints.Add( new DExtremePoint{ IsPeak = true, Value = lValue, Idx = i, Height = 0 } );  
        lPrev = lValue ;
      }

      mExtremePoints.ForEach( p => DContext.WriteLine($"Raw Peak: {p}" ) ) ; 

      if ( mExtremePoints.Count  == 0 )
        return ;

      DContext.WriteLine("Finding Valleys...");

      if ( mExtremePoints[0].Idx > 0 )
        mExtremePoints.Add( new DExtremePoint{ IsPeak = false, Value = lFiltered[0], Idx = 0, Height = 0 } );  

      int lPC = mExtremePoints.Count;
      int lPL = lPC - 1 ; 
      for( int i = 0 ; i < lPC ; ++ i )
      {
        var lPeak = mExtremePoints[i];  

        int lMinSearchBegin = lPeak.Idx + 1 ;

        int lMinSearchEnd = i < lPL ? mExtremePoints[i+1].Idx : lFiltered.Count ;

        var lMinIdx = lMinSearchBegin ;
        var lMin = lPeak.Value.Y ;
        for( int j = lMinSearchBegin ;  j < lMinSearchEnd ; ++ j )
        {
          var lV = lFiltered[j].Y ;
          if ( lV < lMin )
          {
            lMin = lV ;
            lMinIdx = j ;

          }
        }

        mExtremePoints.Add( new DExtremePoint{ IsPeak = false, Value = lFiltered[lMinIdx], Idx = lMinIdx, Height = 0 } );  
      }

      DContext.WriteLine("Sorting Extreme Points...");

      mExtremePoints = mExtremePoints.OrderBy( xp => xp.Idx ).ToList() ;

      mExtremePoints.ForEach( p => DContext.WriteLine($"Raw Extreme Points: {p}" ) ) ; 

      DContext.WriteLine("Assigning Weights to Peaks...");

      int lLC = lPC - 1 ;

      for( int i = 0 ; i < lPC ; ++ i )
      {
        if ( mExtremePoints[i].IsPeak )
        {
          var lPeak = mExtremePoints[i];  

          double lValleyL = i >  0  && mExtremePoints[i-1].IsValley ? mExtremePoints[i-1].Value.Y : 0 ;
          double lValleyR = i < lLC && mExtremePoints[i+1].IsValley ? mExtremePoints[i+1].Value.Y : 0 ;

          double lHeightL = lPeak.Value.Y - lValleyL ;
          double lHeightR = lPeak.Value.Y - lValleyR ; 

          lPeak.Height = Math.Min(lHeightL, lHeightR);

        }
      }
      mExtremePoints.ForEach( p => DContext.WriteLine($"Final Extreme Points: {p}" ) ) ; 

      DContext.Unindent();
    }

    readonly List<DPoint> mDistribution = new List<DPoint>();

    List<DExtremePoint> mExtremePoints = new List<DExtremePoint>();
  }



}
