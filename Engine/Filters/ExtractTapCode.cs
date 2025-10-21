using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public override string ToString() => $"({Row},{Col})";
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
      mSeparatorCountThreshold = DContext.Session.Settings.GetOptionalInt(Name, "SeparatorCountThreshold").GetValueOrDefault(5);
      mMinNumberOfTaps         = DContext.Session.Settings.GetOptionalInt(Name, "MinTapCount").GetValueOrDefault(16);
    }

    double Interval( double aET, double aST )
    {
      return Math.Ceiling( ( aET - aST  ) * 10 );  
    }

    public class Tap
    {
      public Tap( double aTime, double aLagFromPrev) 
      {
        LagFromPrev   = aLagFromPrev; 
        Time          = aTime; 
        IsInternalGap = false ; 
      }

      public double LagFromPrev ;
      public double Time ;
      public bool   IsInternalGap ;

      public override string ToString() => $"[LagFromPrev: {(LagFromPrev/100)} s | Onset: {Time}s { ( IsInternalGap ? "IG" : "" ) } ]";
    }

    // Each TapGroup corresponds to a sequence of closely together Taps.
    // The number of elements in each group is THE TAP COUNT
    public class TapCount
    {
      public List<Tap> Taps = new List<Tap>();

      public int Count => Taps.Count;

      public override string ToString() => $"[{Count}]";

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
      if ( aPulses.Count >= mMinNumberOfTaps )
      {
        rTaps.Add( TapFromPulse( aPulses[0] ) ) ;
        for( int i = 1 ; i < aPulses.Count; ++ i )
          rTaps.Add( TapFromPulse(aPulses[i], aPulses[i-1]) ) ;
      }
      return rTaps ; 
    }

    protected override Packet Process ( LexicalSignal aInput, Config aConfig, Packet aInputPacket, List<Config> rBranches )
    {
      DContext.WriteLine("Decoding Taps...");
      DContext.Indent();

      var lPulses = aInput.GetSymbols<PulseSymbol>() ;

      var lTaps = GetTaps(lPulses);
      if ( lTaps.Count < mMinNumberOfTaps )
      {
        DContext.WriteLine("Not enough Taps. Quitting.");
        rOutput.Add( Packet.Quit(Name, aInputPacket, "TapCodes") ) ;
        return ;
      }

      DContext.WriteLine("Raw Taps:");
      lTaps.ForEach( t => DContext.WriteLine(t.ToString()));

      var lDurations = lTaps.ConvertAll( t => t.LagFromPrev ) ;

      var lTapClassifier = BuildTapClassifier(lDurations);

      lTaps.ForEach( t => lTapClassifier.ClassifyTap(t));

      DContext.WriteLine("Classified Taps:");
      lTaps.ForEach( t => DContext.WriteLine(t.ToString()));

      DContext.WriteLine("Building Tap Counts:");
      List<TapCount> lRawCounts = new List<TapCount>();

      TapCount lCurrTC = new TapCount() ;

      foreach( var lTap in lTaps )
      {
        if ( !lTap.IsInternalGap )
        {
          if ( lCurrTC != null && lCurrTC.Taps.Count > 0 )
            lRawCounts.Add(lCurrTC);

          lCurrTC = new TapCount();  
        }
        lCurrTC.Taps.Add(lTap);
      }
      if ( lCurrTC != null && lCurrTC.Taps.Count > 0 )
        lRawCounts.Add(lCurrTC);

      if ( lRawCounts.Count <  2 * mMinNumberOfTaps )
      {
        DContext.WriteLine("Not enough Taps. Quitting.") ;
        rOutput.Add( Packet.Quit(Name, aInputPacket, "TapCodes") ) ;
        return ;
      }
      
      // A TapCount Bag contains a sequence of Tap Counts that should
      // correspond to a byte. It can contain at most 16 counts (for 8 bits)
      // BUT it can cotain less if Tap are missing
      List<TapCount> lCurrBag = new List<TapCount>();

      // The Groups are separated into Bags.
      List<List<TapCount>> lBags = new List<List<TapCount>>();

      lBags.Add( lCurrBag );  

      // There are special SEPARATOR Tap Counts.
      // These are the ones counting > SeparatorCountThreshold.
      // They might have been added to synchronize bytes in case
      // of missing Taps.

      foreach( var lRawCount in lRawCounts )
      {  
        // A new Bag is created when, eiter a separator Count is found or we alrady have 16 counts
        // for the 8 bits of a byte
        if ( lRawCount.Count >= mSeparatorCountThreshold || lCurrBag.Count >= 16 )
        {
          lCurrBag = new List<TapCount>();
          lBags.Add( lCurrBag );  
        }
        else
        {
          lCurrBag.Add(lRawCount);
        }
      }

      List<TapCode> lAllCodes = new List<TapCode>();

      DContext.WriteLine($"Bags:{lBags.Count}");

      foreach( var lBag in lBags ) 
      {  
        if ( lBag.Count == 0 )
          continue ;

        // Make sure there is an EVEN number of counts in the Bag
        if ( ! MathX.IsEven(lBag.Count ) )
        {
          lBag.Add(lBag.Last());
          var lLast = lBag.Last(); 
        }

        DContext.Write("Bag: "); lBag.ForEach( g => DContext.Write(g.ToString())); DContext.WriteLine("");
      
        List<TapCode> lCodes = new List<TapCode>();

        for ( int i = 0 ;  i < lBag.Count ; i += 2 )
        {
          int lRow = lBag[i  ].Count ;
          int lCol = lBag[i+1].Count ;

          lCodes.Add( new TapCode(lRow,lCol) ) ;
        }

        lCodes.Add( new TapCode(0,0) ) ;

        DContext.WriteLine( $"Code: {string.Join(",", lCodes )}");

        lAllCodes.AddRange( lCodes ) ;  
      }

      int lIdx = 0 ;
      var lSymbols = lAllCodes.ConvertAll( c => new TapCodeSymbol(lIdx++,c) ); 

      rOutput.Add( new Packet(Name, aInputPacket, new LexicalSignal(lSymbols), "TapCodes") ) ;

      DContext.Unindent();  
    }



    public class TapClassifier
    {
      public void ClassifyTap( Tap aTap ) 
      {
        aTap.IsInternalGap = aTap.LagFromPrev <= InternalH ;
      }

      public double InternalH ;
    }

    TapClassifier BuildTapClassifier( List<double> aDurations ) 
    { 
      var lDist0 = new Distribution(aDurations) ;

      double lMax = aDurations.Max();

      var lDist = lDist0.ExtendedWithBaseline(0,lMax+10,1);

      var lFullRangeHistogram = new Histogram(lDist).Table ;

      if ( DContext.Session.Settings.GetBool("Plot") )
      { 
        lFullRangeHistogram.CreatePlot(Plot.Options.Bars).SavePNG(DContext.Session.OutputFile("Durations_Histogram.png"));
      }

      var lXPs = ExtremePointsFinder.Find(lFullRangeHistogram.Points);

      var lPeaksByH = lXPs.Where( xp => xp.IsPeak).OrderByDescending( xp => xp.Value.Y ).ToList();
      var lBest3    = lPeaksByH.Take(3).ToList();
      var lPeaks    = lBest3.OrderBy( xp => xp.Value.X.Value ).ToList();

      var lPeak1 = lPeaks.Count > 0 ? lPeaks[0] : null ;
      var lPeak2 = lPeaks.Count > 1 ? lPeaks[1] : null ;

      DContext.WriteLine($"Peaks: {lPeak1}, {lPeak2}");

      double lInternalLag, lNonInternalLag ;

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
        lNonInternalLag = lPeak2.Value.X.Value ;
        DContext.WriteLine($"NON Internal Lag (from peak): {lNonInternalLag}");
      }
      else
      { 
        lNonInternalLag = lInternalLag * 2.5;
        DContext.WriteLine($"NON Internal Lag (from minimum): {lNonInternalLag}");
      }

      var lInternalH = MathX.LERP(lInternalLag,lNonInternalLag,0.4);

//lInternalH = 23 ;

      DContext.WriteLine($"Internal Threshold: {lInternalH}");

      return new TapClassifier{ InternalH = lInternalH };
    } 

    
    public override string Name => this.GetType().Name ;

    int mSeparatorCountThreshold ;
    int mMinNumberOfTaps ;

  }

}
