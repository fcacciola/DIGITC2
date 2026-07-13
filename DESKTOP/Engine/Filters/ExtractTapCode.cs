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

namespace ENGINE
{
  public class Tap
  {
    public Tap( PulseSymbol aSource, double aTime, double aGap) 
    {
      Source     = aSource;
      Gap        = aGap; 
      Time       = aTime; 
      GapIsIntra = false ; 
    }

    public enum TapType { Row, Col, Separator, IntraGap, OuterGap };

    public void DumpSample(List<float> aSamples, TapType aType )
    {
      int lC = aSamples.Count ;
      for( int i = lC ; i < Source.Start ; i++ )
        aSamples.Add(0);

      float lEffectiveAmplitude = 0 ;
      switch ( aType )
      {
        case TapType.Row:       lEffectiveAmplitude = 0.6f  ; break ; // BLUE
        case TapType.Col:       lEffectiveAmplitude = -0.6f ; break ; // RED
        case TapType.IntraGap:  lEffectiveAmplitude = 0.2f  ; break ; // DARK-GRAY
        case TapType.OuterGap : lEffectiveAmplitude = -0.2f ; break ; // 
        case TapType.Separator: lEffectiveAmplitude = 0.9f  ; break ; // BLACK
      }

      foreach ( var lStep in Source.Steps )
      {
        for( int i = 0; i < lStep.Length; i++ ) 
        {
          aSamples.Add( lEffectiveAmplitude );
        }
      }
    }

    public PulseSymbol Source ;
    public double      Gap ;
    public double      Time ;
    public bool        GapIsIntra ;

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
 
  public class TapCode
  {
    public TapCode( TapCount aR, TapCount aC ) {  Row = aR ; Col = aC ; R = aR.Count ; C = aC.Count ; }

    public TapCode( TapCount aR ) {  Row = aR ; Col = null ; R = 0 ; C = 0 ; }

    public TapCode ( int aR, int aC ) { Row = null; Col = null; R = aR; C = aC; }

    public TapCode ( string aCode )
    {
      Row = null ; 
      Col = null ;

      var lParts = aCode.TrimStart('(').TrimEnd(')').Split(',');
      if (lParts.Length == 2)
      {
        R = int.Parse(lParts[0]);
        C = int.Parse(lParts[1]);
      }
    }

    public readonly TapCount Row ;  
    public readonly TapCount Col ;

    public readonly int R ;
    public readonly int C ;

    public bool IsSeparator => R == 0 && C == 0;

    public override string ToString() => $"({R},{C})";
  }

  public class TapCodeSymbol : Symbol
  {
    public TapCodeSymbol( int aIdx, TapCode aCode ) : base(aIdx) { Code = aCode; SetupSamplePos(); }

    public TapCodeSymbol( int aIdx, TapCode aCode, int aSamplePos ) : base(aIdx) { Code = aCode; SamplePos = aSamplePos ; }

    public TapCodeSymbol(int aIdx, string aStr) : base(aIdx) 
    {
      if ( aStr.Contains(':') )
      {
        var lParts = aStr.Split(':');
        Code = new TapCode(lParts[0]);
        SamplePos = (int)SIG.SamplesForTime(double.Parse(lParts[1]));
      }
      else
      {
        Code = new TapCode(aStr);
        SamplePos = 0;
      }
    }

    public override string Type => "TapCode" ;

    public override Symbol Copy() { return new TapCodeSymbol( Idx, Code, SamplePos ); }  

    public override string ToString() => $"{Code.ToString()}:{SIG.TimeForSample(SamplePos)}";

    public double Value => double.Parse($"{Code.R}.{Code.C}") ;

    public TapCode Code ;

    public override void DumpSamples( List<float> aSamples )
    {
      if ( ! Code.IsSeparator )
      {
        Code.Row?.Taps.ForEach(t => t.DumpSample(aSamples, Tap.TapType.Row));
        Code.Col?.Taps.ForEach(t => t.DumpSample(aSamples, Tap.TapType.Col));
      }
      else
      { 
        Code.Row?.Taps.ForEach(t => t.DumpSample(aSamples, Tap.TapType.Separator));
      }
    }

    void SetupSamplePos()
    {
      if (Code.Row != null && Code.Row.Taps.Count > 0)
        SamplePos = Code.Row.Taps.Last().Source.SamplePos;
      else
        SamplePos = 0;
    }
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
      rOptions.IntraCountGap_HighBound = Params.GetDouble("IntraCountGap");

      return rOptions;

    }

    double PulseOnsetTime( PulseSymbol aPulse ) => aPulse.StartTime ; //+ aPulse.EndTime ) / 2.0 ;

    Tap TapFromPulse( PulseSymbol aCurrPulse ) 
    {
      double lCurrTime = PulseOnsetTime( aCurrPulse ) ;

      return new Tap( aCurrPulse, lCurrTime, aCurrPulse.Gap) ;
    }

    List<Tap> GetTaps( List<PulseSymbol> aPulses )
    {
      List<Tap> rTaps = new List<Tap> ();
      if ( aPulses.Count >= mOptions.MinNumberOfTaps )
      {
        for( int i = 0 ; i < aPulses.Count; ++ i )
          rTaps.Add( TapFromPulse(aPulses[i]) ) ;
      }

      Session.MarkTime("Taps extracted");

      return rTaps ; 
    }

    double ComputeIntraCountGap_HighBound( List<double> aGaps )
    {
      if ( mOptions.IntraCountGap_HighBound == -1 )
      {
        double rIntraCountGap_HighBound = 0.04; // Wild guess if all statistical analysis fails. This is a very high bound, but it is better to have some false positives than to miss real intra counts.

        if (aGaps.Count < 2)
        {
          WriteLine("Not enough gaps to calculate Intra Tap Gap.");
          return rIntraCountGap_HighBound;
        }

        FilterHelper.DumpValues(Session, "Pulses_Gaps",aGaps);
        var lRawGMM = GmmFitter.Fit(aGaps) ;

        if ( lRawGMM != null )
        {
          lRawGMM.Save(Session, "Raw GMM_For_IntraTapGap_Calculation");
          lRawGMM.Plot(Session, "Raw Gaps_Histogram_For_IntraTapGap_Calculation"); 

          var lGMM = lRawGMM.DiscardMeaningless()?.ChooseBest(3);

          if ( lGMM != null && lGMM.Components.Count > 0 )
          {
            lGMM.Save(Session, "GMM_For_IntraTapGap_Calculation");
            lGMM.Plot(Session, "Gaps_Histogram_For_IntraTapGap_Calculation"); 

            if ( lGMM.Components.Count == 1 )
            {
              rIntraCountGap_HighBound = lGMM.Components[0].N_Sigma(3);
            }
            else if ( lGMM.Components.Count > 1 )
            {
              double lK0_K1_Midpoint = lGMM.InterpolateMean(0,1); 

              double lK0_2_Sigma = lGMM.Components[0].N_Sigma(3);

              rIntraCountGap_HighBound = MathX.LERP(lK0_2_Sigma,lK0_K1_Midpoint,0.2)  ;

              double lIntraTapCode2 = lGMM.Intersection(0,1) ;
              AddBranch("IntraCountGap",$"{lIntraTapCode2:F3}");

              if ( lGMM.Components.Count > 2 )
              {
                double lIntraTapCode3 = lGMM.Intersection(1,2) ;

                AddBranch("IntraCountGap",$"{lIntraTapCode3:F3}");
              }
            }
            else
            {
              WriteLine("Not enough components in GMM to calculate Intra Tap Gap.");
            }
          }
        }
        else
        {
          WriteLine("No GMM to calculate Intra Tap Gap.");
        }

        Params.ChangeValue("IntraCountGap",$"{rIntraCountGap_HighBound:F3}");

        Session.MarkTime($"IntraCountGap_HighBound calculated");

        return rIntraCountGap_HighBound ;
      }

      return mOptions.IntraCountGap_HighBound ;
    }

    protected override Packet Process ()
    {
      mOptions = CreateOptions(); 

      var lPulses = LexicalInput.GetSymbols<PulseSymbol>() ;

      lPulses.SetupGapDurations();

      WriteLine2GUI("Extracting Tap Code...");
      Indent();

      List<string> lPartialResultMessages = new List<string>();

      WriteLine("Getting Raw Taps..");
      var lTaps = GetTaps(lPulses);
      if ( lTaps.Count < mOptions.MinNumberOfTaps && Session.QuitEnabled )
      {
        WriteLine2GUI("Not enough Taps. Quitting.");
        return CreateQuitOutput();
      }

      WriteDetailLine("Raw Taps:");
      lTaps.ForEach( t => WriteDetailLine(t.ToString()));

      WriteDetailLine("Classifying Raw Taps...");
      double lIntraTapGap_HighBound = ComputeIntraCountGap_HighBound( lTaps.ConvertAll( t => t.Gap ).Skip(1).ToList() );
      if ( lIntraTapGap_HighBound == 0 && Session.QuitEnabled )
      {
        return CreateQuitOutput();
      }

      WriteLine2GUI($"IntraTap Gap High Bound: {lIntraTapGap_HighBound:F2}s");
      lTaps.ForEach( t => t.GapIsIntra = t.Gap <= lIntraTapGap_HighBound );
      Session.MarkTime("Taps classified");

      List<float> lTapSamples = new List<float>();
      lTaps.ForEach( t => t.DumpSample(lTapSamples,  t.GapIsIntra ? Tap.TapType.IntraGap: Tap.TapType.OuterGap )) ;

      Plot(lTapSamples, "0_Taps-ColorCoded");

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

      if ( lRawCounts.Count <  2 * mOptions.MinNumberOfTaps && Session.QuitEnabled )
      {
        WriteLine("Not enough Taps. Quitting.") ;
        return CreateQuitOutput();
      }

      Session.MarkTime("Raw Tap Counts built");

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
          lCurrBag.Add(lRawCount);
          lCurrBag = new List<TapCount>();
          lBags.Add( lCurrBag );  
        }
        else
        {
          lCurrBag.Add(lRawCount);
        }
      }

      Session.MarkTime("Tap Count Bags created");

      List<TapCode> lAllCodes = new List<TapCode>();

      WriteLine($"Tap Bag Count:{lBags.Count}");
      lPartialResultMessages.Add($"Tap Bag Count:{lBags.Count}");

      foreach( var lBag in lBags ) 
      {  
        if ( lBag.Count == 0 )
          continue ;

        //lBag.ForEach( g => WriteDetailLine(g.ToString()));
        //var lTapsToString = string.Join(",", lBag.ConvertAll( b => $"{b.Count}"));
        //lPartialResultMessages.Add($"Taps: {lTapsToString}");
        //WriteDetailLine($"Taps: {lTapsToString}");

        List<TapCode> lCodes = new List<TapCode>();

        for ( int i = 0 ;  i < lBag.Count ; i += 2 )
        {
          var lRow = lBag[i  ] ;

          if ( i + 1 <  lBag.Count )
          {
            var lCol = lBag[i+1] ;

            lCodes.Add( new TapCode(lRow,lCol) ) ;
          }
          else
          {
            lCodes.Add( new TapCode(lRow) ) ;
          }
        }

        string lPartialResultMessage = $"Tap Code: {string.Join(",", lCodes )}";
        WriteDetailLine( lPartialResultMessage );
        lPartialResultMessages.Add(lPartialResultMessage);

        lAllCodes.AddRange( lCodes ) ;  
      }

      Session.MarkTime("Tap Codes extracted");

      int lIdx = 0 ;
      var lSymbols = lAllCodes.ConvertAll( c => new TapCodeSymbol(lIdx++,c) ); 

      Plot(lSymbols, "1_TapCode-ColorCoded-Blocks");

      string lTapCodeFile = Session.OutputFile("TapCode.txt");

      File.WriteAllLines(lTapCodeFile, lSymbols.ConvertAll(c => c.ToString()));

      Unindent();

      return CreateOutput( new FileSignal(lTapCodeFile), "TapCodes", new Score(Name, lAllCodes.Count, Score.TypeE.Boundless), false, new PartialResultMessage(lPartialResultMessages) ) ;
    }

    public override string Name => this.GetType().Name ;

    class Options
    {
      internal int    SeparatorCountThreshold ;
      internal int    MinNumberOfTaps ;
      internal double IntraCountGap_HighBound = -1 ;
    }

    Options mOptions ;

  }

}
