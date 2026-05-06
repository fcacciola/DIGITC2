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

    public override string ToString() => Code.ToString() ;

    public double Value => double.Parse($"{Code.Row}.{Code.Col}") ;

    public TapCode Code ;

  }

  public class ExtractTapCode : LexicalFilter
  {
    public ExtractTapCode() 
    { 
    }

    Options CreateOptions()
    {
      Options rOptions = new ();

      rOptions.SeparatorCountThreshold = Params.GetInt("SeparatorCountThreshold");
      rOptions.MinNumberOfTaps         = Params.GetInt("MinTapCount");
      rOptions.IntraCountGap           = Params.GetDouble("IntraCountGap");

      return rOptions;

    }

    public class Tap
    {
      public Tap( double aTime, double aGap) 
      {
        Gap        = aGap; 
        Time       = aTime; 
        GapIsIntra = false ; 
      }

      public double Gap ;
      public double Time ;
      public bool   GapIsIntra ;

      public override string ToString() => $"[Gap: {(Gap)} s | Time: {Time}s { ( GapIsIntra ? "I" : "X" ) } ]";
    }

    // Each TapGroup corresponds to a sequence of closely together Taps.
    // The number of elements in each group is THE TAP COUNT
    public class TapCount
    {
      public List<Tap> Taps = new List<Tap>();

      public int Count => Taps.Count;

      public override string ToString() => $"[{Count}]";

    }

    double PulseOnsetTime( PulseSymbol aPulse ) => aPulse.StartTime ; //+ aPulse.EndTime ) / 2.0 ;

    Tap TapFromPulse( PulseSymbol aCurrPulse ) 
    {
      double lCurrTime = PulseOnsetTime( aCurrPulse ) ;

      return new Tap( lCurrTime, aCurrPulse.Gap) ;
    }

    List<Tap> GetTaps( List<PulseSymbol> aPulses )
    {
      List<Tap> rTaps = new List<Tap> ();
      if ( aPulses.Count >= mOptions.MinNumberOfTaps )
      {
        for( int i = 0 ; i < aPulses.Count; ++ i )
          rTaps.Add( TapFromPulse(aPulses[i]) ) ;
      }
      return rTaps ; 
    }

    double ComputeIntraCountGap( List<double> aGaps )
    {
      if ( mOptions.IntraCountGap == -1 )
      {
        FilterHelper.DumpValues("Pulses_Gaps",aGaps);
        var lGMM = GmmFitter.Fit(aGaps);

        double rIntraCountGap = 0.0; 

        if ( lGMM != null && lGMM.Components.Count > 1 )
        {
          double lK0_K1_Midpoint = lGMM.InterpolateMean(0,1); 

          double lK0_2_Sigma = lGMM.Components[0].N_Sigma(3);

         rIntraCountGap = Math.Min(lK0_K1_Midpoint, lK0_2_Sigma)  ;

          double lIntraTapCode2 = lGMM.Intersection(0,1) ;
          AddBranch("IntraCountGap",$"{(lIntraTapCode2)}");

          if ( lGMM.Components.Count > 2 )
          {
            double lIntraTapCode3 = lGMM.Intersection(1,2) ;

            AddBranch("IntraCountGap",$"{(lIntraTapCode3)}");
          }
        }
        else
        {
          WriteLine("Not enough components in GMM to calculate Intra Tap Gap.");
        }

        lGMM?.Plot("Gaps_Histogram_For_IntraTapGap_Calculation"); 

        return rIntraCountGap ;
      }

      return mOptions.IntraCountGap ;
    }

    protected override Packet Process ()
    {
      mOptions = CreateOptions(); 

      var lPulses = LexicalInput.GetSymbols<PulseSymbol>() ;

      WriteLine2GUI("Extracting Tap Code...");
      Indent();

      WriteLine("Getting Raw Taps..");
      var lTaps = GetTaps(lPulses);
      if ( lTaps.Count < mOptions.MinNumberOfTaps )
      {
        WriteLine2GUI("Not enough Taps. Quitting.");
        return CreateQuitOutput();
      }

      WriteDetailLine("Raw Taps:");
      lTaps.ForEach( t => WriteDetailLine(t.ToString()));

      WriteDetailLine("Classifying Raw Taps...");
      double lIntraTapGap_HighBound = ComputeIntraCountGap( lTaps.ConvertAll( t => t.Gap ).Skip(1).ToList() );
      if ( lIntraTapGap_HighBound == 0 )
      {
        return CreateQuitOutput();
      }

      WriteLine2GUI($"IntraTap Gap High Bound: {lIntraTapGap_HighBound:F2}s");
      lTaps.ForEach( t => t.GapIsIntra = t.Gap <= lIntraTapGap_HighBound );

      WriteDetailLine("Classified Taps:");
      lTaps.ForEach( t => WriteDetailLine(t.ToString()));

      WriteLine("Building Tap Counts...");
      List<TapCount> lRawCounts = new List<TapCount>();

      TapCount lCurrTC = new TapCount() ;

      foreach( var lTap in lTaps )
      {
        if ( !lTap.GapIsIntra )
        {
          if ( lCurrTC != null && lCurrTC.Taps.Count > 0 )
            lRawCounts.Add(lCurrTC);

          lCurrTC = new TapCount();  
        }
        lCurrTC.Taps.Add(lTap);
      }
      if ( lCurrTC != null && lCurrTC.Taps.Count > 0 )
        lRawCounts.Add(lCurrTC);

      if ( lRawCounts.Count <  2 * mOptions.MinNumberOfTaps )
      {
        WriteLine("Not enough Taps. Quitting.") ;
        return CreateQuitOutput();
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
        if ( lRawCount.Count >= mOptions.SeparatorCountThreshold || lCurrBag.Count >= 16 )
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

      WriteLine($"Bag Count:{lBags.Count}");

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

        WriteDetailLine("Bag:");
        Indent();
        lBag.ForEach( g => WriteDetailLine(g.ToString()));
        Unindent();
      
        List<TapCode> lCodes = new List<TapCode>();

        for ( int i = 0 ;  i < lBag.Count ; i += 2 )
        {
          int lRow = lBag[i  ].Count ;
          int lCol = lBag[i+1].Count ;

          lCodes.Add( new TapCode(lRow,lCol) ) ;
        }

        lCodes.Add( new TapCode(0,0) ) ;

        WriteDetailLine( $"Code: {string.Join(",", lCodes )}");

        lAllCodes.AddRange( lCodes ) ;  
      }

      int lIdx = 0 ;
      var lSymbols = lAllCodes.ConvertAll( c => new TapCodeSymbol(lIdx++,c) ); 

      Unindent(); 

      return CreateOutput( new LexicalSignal(lSymbols), "TapCodes") ;
    }
   
    public override string Name => this.GetType().Name ;

    class Options
    {
      internal int    SeparatorCountThreshold ;
      internal int    MinNumberOfTaps ;
      internal double IntraCountGap = -1 ;
    }

    Options mOptions ;

  }

}
