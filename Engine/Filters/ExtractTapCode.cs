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

  public class ExtractTapCode : WaveFilter
  {
    public ExtractTapCode() 
    { 
    }

    double Interval( double aET, double aST )
    {
      return Math.Ceiling( ( aET - aST  ) * 10 );  
    }

    public class Tap
    {
      public Tap( double aTime, double aDuration) 
      {
        Time = aTime; 
        Duration = aDuration; 
        Counter = 0 ;
      }

      public double Time ;
      public double Duration ;
      public bool   IsShort ;
      public int    Counter ;

      public override string ToString() => $"[{Time}s|{Duration}s {(IsShort?"S":"L")} {Counter}]";
    }


    List<Tap> GetTaps( List<double> aTimes )
    {
      List<Tap> rTaps = new List<Tap> ();
      rTaps.Add( new Tap(aTimes[0], Interval(0,aTimes[0]) ) ) ;
      for( int i = 1 ; i < aTimes.Count; ++ i )
        rTaps.Add( new Tap(aTimes[i], Interval(aTimes[i],aTimes[i-1]) ) ) ;
      return rTaps ; 
    }

    protected override void Process ( WaveSignal aInput, Branch aInputBranch, List<Branch> rOutput )
    {
       Context.WriteLine("Decoding Taps...");
       Context.Indent();

       OnsetDetection.Onset lOnset = aInputBranch.GetData<OnsetDetection.Onset>();

       var lTaps = GetTaps(lOnset.Times );

       Context.WriteLine("Raw Taps:");
       lTaps.ForEach( t => Context.WriteLine(t.ToString()));

       var lDurations = lTaps.ConvertAll( t => t.Duration ) ;

       var lTapClassifier = BuildTapClassifier(lDurations);

       lTaps.ForEach( t => lTapClassifier.ClassifyTap(t));

       Context.WriteLine("Classified Taps:");
       lTaps.ForEach( t => Context.WriteLine(t.ToString()));

       SetTapCounters(lTaps); 

       Context.WriteLine("Counted Taps:");
       lTaps.ForEach( t => Context.WriteLine(t.ToString()));

       var lCodes = GetCodes(lTaps);

       Context.WriteLine( $"Code: {string.Join(",", lCodes )}");

       int lIdx = 0 ;
       var lSymbols = lCodes.ConvertAll( c => new TapCodeSymbol(lIdx++,c) ); 

       rOutput.Add( new Branch(aInputBranch, new LexicalSignal(lSymbols), "TapCodes") ) ;

       Context.Unindent();  
    }

    public class TapClassifier
    {
      public TapClassifier( double aShortDurationIntervalL, double aShortDurationIntervalR )
      {
        Context.WriteLine($"TaClassifier. Short duration interval: {aShortDurationIntervalL}->{aShortDurationIntervalR}");
        ShortDurationIntervalL = aShortDurationIntervalL ;
        ShortDurationIntervalR = aShortDurationIntervalR ;
      }

      public void ClassifyTap( Tap aTap )
      {
        aTap.IsShort = ShortDurationIntervalL <= aTap.Duration && aTap.Duration <= ShortDurationIntervalR;
      }

      public double ShortDurationIntervalL ;
      public double ShortDurationIntervalR ;
    }

    TapClassifier BuildTapClassifier( List<double> aDurations ) 
    { 
      var lDist = new Distribution(aDurations) ;

      var lFullRangeHistogram = new Histogram(lDist).Table ;

      if ( Context.Session.Args.GetBool("Plot") )
      { 
        lFullRangeHistogram.CreatePlot(Plot.Options.Bars).SavePNG(Context.Session.LogFile($"_Durations_Histogram.png"));
      }

      var lXPs = ExtremePointsFinder.Find(lFullRangeHistogram.Points);

      var lPeak = lXPs.Find( xp => xp.IsPeak ) ;

      Context.WriteLine($"Very First Peak: {lPeak}");

      double lShortDurationReference ;

      if ( lPeak != null)
      {
        lShortDurationReference = lPeak.Value.X.Value ;
        Context.WriteLine($"Short duration referenc (from peak): {lShortDurationReference}");
      }
      else
      { 
        lShortDurationReference = aDurations.Minimum();
        Context.WriteLine($"Short duration reference ( from minimum): {lShortDurationReference}");
      }

      var lDurationIntervalL = lShortDurationReference - lShortDurationReference * .50 ; 
      var lDurationIntervalR = lShortDurationReference + lShortDurationReference * .25 ; 

      return new TapClassifier(lDurationIntervalL, lDurationIntervalR) ;
    } 

    void SetTapCounters( List<Tap> aTaps )
    {
      int lCounter = 0 ;

      foreach (var lTap in aTaps)
      {
        if ( lTap.IsShort )           
        {
          lTap.Counter = lCounter ++ ; 
        }
        else
        {
          lCounter = 0 ;
          lTap.Counter = lCounter ++ ;
        }
      }
    }
    
    bool IsEven ( int aN ) { return aN % 2 == 0 ; }

    List<TapCode> GetCodes( List<Tap> aTaps ) 
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

      Context.WriteLine( $"Counts: {string.Join(",", lCounts )}");

      if ( ! IsEven(lCounts.Count) )
        lCounts.Add(1);
        
      for (int i = 0 ; i < lCounts.Count ; i += 2 )
      {
        rCodes.Add( new TapCode(lCounts[i],lCounts[i+1]) ) ;
      }

      return rCodes ;
    }

    protected override string Name => "ExtractTapCode" ;

  }

}
