using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Spreadsheet;

using MathNet.Numerics.Integration;
using MathNet.Numerics.Statistics;

using Newtonsoft.Json.Serialization;

using NWaves.Operations;
using NWaves.Signals;

namespace DIGITC2_ENGINE
{
 
  public class TapCode
  {
    public TapCode( int aR, int aC ) {  Row = aR ; Col = aC ; }

    public readonly int Row ;  
    public readonly int Col ;

    public override string ToString() => $"(R:{Row}xC:{Col})";
  }

  public class TapCodeSymbol : Symbol
  {
    public TapCodeSymbol( int aIdx, TapCode aCode ) : base(aIdx) { Code = aCode; }

    public override string Type => "TapCode" ;

    public override Symbol Copy() { return new TapCodeSymbol( Idx, Code ); }  

    public override string Meaning => Code.ToString() ;

    public override double Value => double.Parse($"{Code.Row}.{Code.Col}") ;

    public TapCode Code ;

  }

  public class ExtractTapCode : LexicalFilter
  {
    public ExtractTapCode() 
    { 
    }

    public override void Setup() 
    { 
      mMinTapCount = DContext.Session.Args.GetOptionalInt("ExtractTapCode_MinTapCount").GetValueOrDefault(16);
    }

    double Interval( double aET, double aST )
    {
      return Math.Ceiling( ( aET - aST  ) * 10 );  
    }

    public class Tap
    {
      public enum LagTypeE { Space, Bridge, Internal, UNKNOWN } ;

      public Tap( double aTime, double aLagFromPrev) 
      {
        LagFromPrev = aLagFromPrev; 
        Time        = aTime; 
        Counter     = 0 ;
      }

      public double   LagFromPrev ;
      public double   Time ;
      public LagTypeE LagType = LagTypeE.UNKNOWN ;
      public int      Counter ;

      public override string ToString() => $"[{LagFromPrev}s->{Time}s {LagType} {Counter}]";
    }

    // All taps separated by Spaces
    public class TapGroup
    {
      public List<Tap> Taps = new List<Tap>();
    }

    double PulseOnsetTime( PulseSymbol aPulse ) => ( aPulse.StartTime + aPulse.EndTime ) / 2.0 ;

    Tap TapFromPulse( PulseSymbol aCurrPulse, PulseSymbol aPrevPulse = null ) 
    {
      double lPrevTime = aPrevPulse != null ? PulseOnsetTime(aPrevPulse) : 0 ;  
      double lCurrTime = PulseOnsetTime( aCurrPulse ) ;

      double lLagFromPrev = Math.Ceiling( (lCurrTime - lPrevTime) * 100 ) ;

      return new Tap( lCurrTime, lLagFromPrev) ;
    }

    List<Tap> GetTaps( List<PulseSymbol> aPulses )
    {
      List<Tap> rTaps = new List<Tap> ();
      if ( aPulses.Count >= mMinTapCount )
      {
        rTaps.Add( TapFromPulse( aPulses[0] ) ) ;
        for( int i = 1 ; i < aPulses.Count; ++ i )
          rTaps.Add( TapFromPulse(aPulses[i], aPulses[i-1]) ) ;
      }
      return rTaps ; 
    }

    protected override void Process (LexicalSignal aInput, Branch aInputBranch, List<Branch> rOutput )
    {
      DContext.WriteLine("Decoding Taps...");
      DContext.Indent();

      var lPulses = aInput.GetSymbols<PulseSymbol>() ;

      var lTaps = GetTaps(lPulses);
      if ( lTaps.Count == 0 )
      {
        rOutput.Add( Branch.Quit(aInputBranch, "TapCodes") ) ;
        return ;
      }

      DContext.WriteLine("Raw Taps:");
      lTaps.ForEach( t => DContext.WriteLine(t.ToString()));

      var lDurations = lTaps.ConvertAll( t => t.LagFromPrev ) ;

      var lTapClassifier = BuildTapClassifier(lDurations);

      lTaps.ForEach( t => lTapClassifier.ClassifyTap(t));

      DContext.WriteLine("Classified Taps:");
      lTaps.ForEach( t => DContext.WriteLine(t.ToString()));

      List<TapGroup> lGroups = new List<TapGroup>();

      TapGroup lCurrTG = new TapGroup() ;

      foreach( var lTap in lTaps )
      {
        if ( lTap.LagType == Tap.LagTypeE.Space )
        {
          if ( lCurrTG != null && lCurrTG.Taps.Count > 0 )
            lGroups.Add(lCurrTG);

          lCurrTG = new TapGroup();  
        }
        lCurrTG.Taps.Add(lTap);
      }
      if ( lCurrTG != null && lCurrTG.Taps.Count > 0 )
        lGroups.Add(lCurrTG);
 
      List<TapCode> lCodes = new List<TapCode>();
      
      foreach( var lTG in lGroups )
      {
        SetTapCounters(lTG.Taps); 

        DContext.WriteLine("Counted Taps:");
        lTG.Taps.ForEach( t => DContext.WriteLine(t.ToString()));

        var lCode = GetCode(lTG.Taps);

        lCodes.Add(lCode);
      }

      DContext.WriteLine( $"Code: {string.Join(",", lCodes )}");

      int lIdx = 0 ;
      var lSymbols = lCodes.ConvertAll( c => new TapCodeSymbol(lIdx++,c) ); 

      rOutput.Add( new Branch(aInputBranch, new LexicalSignal(lSymbols), "TapCodes") ) ;

      DContext.Unindent();  
    }

    public class DurationInterval
    {
      public DurationInterval( double aL, double aH )
      {
        L = aL ;
        H = aH ;
      }

      public DurationInterval( double aC )
      {
      }

      public double L ;
      public double H ;

      public bool IsInside( double t ) { return ( L <= t ) && ( t <= H ); } 
    }

    // Taps are arranged into a single TapCode, based on the duration from the previous Tap
    // There are 3 types of durations: Space, Bride and Internal.
    // For example, the codes (2x1) (1x2) would be encoded as tap with the following sequence of "from 
    // 
    //  Space | Internal | Internal | Bridge | Space | Bridge | Internal | Internal
    public class TapClassifier
    {
      public void ClassifyTap( Tap aTap ) 
      {
        if ( Bridge.IsInside(aTap.LagFromPrev) )
             aTap.LagType= Tap.LagTypeE.Bridge ;
        else if( aTap.LagFromPrev < Bridge.L )
             aTap.LagType = Tap.LagTypeE.Internal ;
        else aTap.LagType = Tap.LagTypeE.Space ;
      }

      public DurationInterval Bridge   ;
      public DurationInterval Internal ;
    }

    TapClassifier BuildTapClassifier( List<double> aDurations ) 
    { 
      var lDist0 = new Distribution(aDurations) ;

      double lMax = aDurations.Max();

      var lDist = lDist0.ExtendedWithBaseline(0,lMax+10,1);

      var lFullRangeHistogram = new Histogram(lDist).Table ;

      if ( DContext.Session.Args.GetBool("Plot") )
      { 
        lFullRangeHistogram.CreatePlot(Plot.Options.Bars).SavePNG(DContext.Session.LogFile($"_Durations_Histogram.png"));
      }

      var lXPs = ExtremePointsFinder.Find(lFullRangeHistogram.Points);

      var lPeaksByH = lXPs.Where( xp => xp.IsPeak).OrderByDescending( xp => xp.Value.Y ).ToList();
      var lBest3    = lPeaksByH.Take(3).ToList();
      var lPeaks    = lBest3.OrderBy( xp => xp.Value.X.Value ).ToList();

      var lPeak1 = lPeaks.Count > 0 ? lPeaks[0] : null ;
      var lPeak2 = lPeaks.Count > 1 ? lPeaks[1] : null ;
      var lPeak3 = lPeaks.Count > 2 ? lPeaks[2] : null ;

      DContext.WriteLine($"Peaks: {lPeak1}, {lPeak2}, {lPeak3}");

      double lInternalLag ;
      double lBridgeLag ;
      double lSpaceLag ;

      if ( lPeak1 != null)
      {
        lInternalLag = lPeak1.Value.X.Value ;
        DContext.WriteLine($"Internal Lag (from peak): {lInternalLag}");
      }
      else
      { 
        lInternalLag = aDurations.Minimum();
        DContext.WriteLine($"Internal Lag (from minimum): {lInternalLag}");
      }

      if ( lPeak2 != null)
      {
        lBridgeLag = lPeak2.Value.X.Value ;
        DContext.WriteLine($"Bridge Lag (from peak): {lBridgeLag}");
      }
      else
      { 
        lBridgeLag = lInternalLag * 2.5;
        DContext.WriteLine($"Bridge Lag (from minimum): {lBridgeLag}");
      }

      if ( lPeak3 != null)
      {
        lSpaceLag = lPeak3.Value.X.Value ;
        DContext.WriteLine($"Space Lag (from peak): {lSpaceLag}");
      }
      else
      { 
        lSpaceLag = lBridgeLag * 2.5;
        DContext.WriteLine($"Space Lag (from minimum): {lSpaceLag}");
      }

      var lInternalH = ( ( lInternalLag + lBridgeLag ) / 2 ) * 1.1; 
      var lSpaceL    = ( ( lBridgeLag   + lSpaceLag  ) / 2 ) * 0.9; 

      var lBridge = new DurationInterval(lInternalH,lSpaceL) ;

      return new TapClassifier{ Internal = null, Bridge = lBridge };
    } 

    void SetTapCounters( List<Tap> aTaps )
    {
      int lCounter = 1 ;

      foreach (var lTap in aTaps)
      {
        if ( lTap.LagType == Tap.LagTypeE.Internal )           
        {
          lTap.Counter = lCounter ++ ; 
        }
        else
        {
          lCounter = 1 ;
          lTap.Counter = lCounter ++ ;
        }
      }
    }
    
    bool IsEven ( int aN ) { return aN % 2 == 0 ; }

    TapCode GetCode( List<Tap> aTaps ) 
    {
      List<int> lCounts = new List<int>();

      int lMax  = 0 ;
      foreach (var lTap in aTaps)
      {
        if ( lTap.Counter > 0 )  
        {
          if ( lTap.Counter > lMax )
            lMax = lTap.Counter ;
          else
          {
            lCounts.Add( lMax ) ;
            lMax = 0 ;
          }
        }
        else
        {
          if ( lMax > 0)
            lCounts.Add( lMax ) ;
          lMax = 0 ;
        }
      }
      if ( lMax > 0)
        lCounts.Add( lMax ) ;

      List<TapCode> rCodes = new List<TapCode> ();

      DContext.WriteLine( $"Counts: {string.Join(",", lCounts )}");

      if ( ! IsEven(lCounts.Count) )
        lCounts.Add(1);
        
      for (int i = 0 ; i < lCounts.Count ; i += 2 )
      {
        rCodes.Add( new TapCode(lCounts[i],lCounts[i+1]) ) ;
      }

      return rCodes.First() ;
    }

    protected override string Name => "ExtractTapCode" ;

    int mMinTapCount ;

  }

}
