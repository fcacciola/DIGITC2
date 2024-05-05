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

namespace DIGITC2
{
 
  public class DecodeTaps : WaveFilter
  {
    public DecodeTaps() 
    { 
    }

    enum BitType { One, Zero, Noise } ;

    static public void PlotBits( LexicalSignal aSignal, string aLabel )
    {
      List<BitSymbol> lBits = aSignal.GetSymbols<BitSymbol>();  

      if ( lBits.Count > 0 ) 
      { 
        List<float> lSamples = new List<float> ();
        lBits.ForEach( b => b.View.DumpSamples(lSamples ) );
        int lSamplingRate = lBits[0].View.SamplingRate;
        DiscreteSignal lWaveRep = new DiscreteSignal(lSamplingRate, lSamples);
        WaveSignal lWave = new WaveSignal(lWaveRep);
        lWave.SaveTo( Context.Session.LogFile( "Bits_" + aLabel + ".wav") ) ;
      }
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

    public class CodeTable
    {
      public CodeTable() 
      { }

      public string Map( Code aCode )
      {
        string rT = "" ;
        return rT ;
      }
    }

    public class Code
    {
      public int Col ;
      public int Row ;  

      public override string ToString() => $"({Col}x{Row})";
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

       var lCounts = GetTapCounts(lTaps);

       Context.WriteLine( $"Counts: {string.Join(",", lCounts )}");
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

      var lShortDurationReference = lPeak.Value.X.Value ;

      Context.WriteLine($"Short duration reference: {lShortDurationReference}");

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
    
    List<int> GetTapCounts( List<Tap> aTaps ) 
    {
      List<int> rCounts = new List<int>();

      int lMax  = 0 ;
      foreach (var lTap in aTaps)
      {
        if ( lTap.Counter > 0 )  
        {
          if ( lTap.Counter > lMax )
            lMax = lTap.Counter ;
          else
          {
            rCounts.Add( lMax ) ;
            lMax = 0 ;
          }
        }
        else
        {
          if ( lMax > 0)
            rCounts.Add( lMax ) ;
          lMax = 0 ;
        }
      }
      if ( lMax > 0)
        rCounts.Add( lMax ) ;

      return rCounts ;
    }

    protected override string Name => "BinarizeByDuration" ;

  }

}
