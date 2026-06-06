using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Operations;
using NWaves.Signals;

using MathNet.Numerics.Statistics;
using System.Runtime.InteropServices;

namespace DIGITC2_ENGINE
{

  public class FilterHelper
  {

    static public void DumpValues<T>( string aName, List<T> aValues )
    {
      try
      {
        string[] lAsStrings = new string[aValues.Count];
        for( int i = 0 ; i <  aValues.Count ; i++ ) 
          lAsStrings[i] = $"{aValues[i]}";

        var lCSV = string.Join(" , ", lAsStrings ) ;

        File.WriteAllText(DContext.Session.OutputFile($"{aName}_CSV.txt"), lCSV);
      }
      catch( Exception e )
      {
        DContext.Error(e);
      }
    }
  }

  public static class PulseSymbolExtensions
  {
    static public void SetupGapDurations( this List<PulseSymbol> aPulses )
    {
      var rGapDurations = new double[aPulses.Count-1];

      var lPulseA = aPulses[0];
      lPulseA.Gap = 0;  

      for ( int i = 1; i < aPulses.Count ; i++ )
      { 
        var lPulseB = aPulses[i]; 

        var lGap = lPulseB.StartTime - lPulseA.EndTime;

        lPulseB.Gap = lGap ;

        lPulseA = lPulseB;
      }
    }

    static public List<double> Gaps( this List<PulseSymbol> aPulses )
    {
      return aPulses.Select( p => p.Gap ).Skip(1).ToList();
    }

    static public List<double> Durations( this List<PulseSymbol> aPulses )
    {
      return aPulses.Select( p => p.Duration ).ToList();
    }

    static public void Plot( this List<PulseSymbol> aPulses, string aLabel )
    {
      List<float> lSamples = new List<float> ();
      aPulses.ForEach( s => s.DumpSamples(lSamples ) );
      DiscreteSignal lWaveRep = new DiscreteSignal(SIG.SamplingRate, lSamples);
      WaveSignal lWave = new WaveSignal(lWaveRep);
      lWave.SaveTo( DContext.Session.OutputFile( aLabel + ".wav") ) ;
    }
  }

  public class PulseFilterHelper : FilterHelper
  {
  }

  internal class PulseStepBuilder
  {
    internal static List<PulseStep> Build( WaveSignal aSource, int aPulseStart, int aPulseEnd )
    {
      var lB = new PulseStepBuilder( aSource, aPulseStart, aPulseEnd );  
      return lB.Create();
    }

    PulseStepBuilder( WaveSignal aSource, int aPulseStart, int aPulseEnd )
    {
      mSource     = aSource;  
      mPulseStart = aPulseStart;  
      mPulseEnd   = aPulseEnd;
    }

    List<PulseStep> Create()
    {
      mCurrCount = 0; 
      mPos = mPulseStart ;
      mStepStart = mPos ;
      mAmplitude = -1 ;

      for ( int i = mPulseStart; i < mPulseEnd ; ++ i )
      {
        float lV = mSource.Samples[i];

        if( mCurrCount == 0 || mAmplitude != lV ) 
        {
          AddStep();

          mAmplitude = lV;
          mCurrCount = 1 ;
        }
        else
        { 
          mCurrCount ++ ;
        }

        mPos ++ ;
      }

      AddStep();

      return mSteps ;
    }

    void AddStep()
    {
      if ( mCurrCount > 0 )
      {
        mSteps.Add( new PulseStep(mAmplitude, mStepStart, mPos ) );
        mStepStart = mPos ;
      }
    }


    WaveSignal      mSource ;
    int             mPulseStart ;
    int             mPulseEnd ;
    List<PulseStep> mSteps = new List<PulseStep>();
    int             mStepStart ;
    int             mPos ;
    float           mAmplitude ;
    int             mCurrCount ;
  }

  public class ExtractPulseSymbols : WaveFilter
  {
    public ExtractPulseSymbols() 
    { 
    }

    Options CreateOptions()
    {
      Options rOptions = new ();

      rOptions.MinLevelThreshold           = Params.GetInt("MinLevelThreshold");
      rOptions.MinDurationThreshold        = Params.GetDouble("MinDurationThreshold");
      rOptions.ContiguousPulsesGapDuration = Params.GetDouble("ContiguousPulsesGapDuration");
      rOptions.VeryShortThreshold          = Params.GetDouble("VeryShortThreshold");

      return rOptions;

    }

    protected override Packet Process ()
    {
      mOptions = CreateOptions() ;

      CreatePulses();
      SelectValidPulses();
      MergeContiguousPulses();
      RemoveVeryShortPulses();

      return CreateOutput( new LexicalSignal(CurrPulses), Name, null, false) ;
    }
   
    public override string Name => this.GetType().Name ;

    List<PulseSymbol> CurrPulses => mPulsesBag.Last() ;

    void CreatePulses()
    {
      mPulsesBag.Add( new List<PulseSymbol>() );

      mInGap = WaveInput.Samples[0] == 0 ;

      mPulseStart = 0 ;
      mPos        = 0 ;

      WriteLine2GUI($"Creating pulses for WaveSignal of Length {WaveInput.Samples.Length}");

      for ( mPos = 0 ; mPos < WaveInput.Samples.Length ; ++ mPos )
      {
        float lV = WaveInput.Samples[mPos] ;

        if ( lV > 0 )
        {
          if ( mInGap )
          {
            mPulseStart = mPos ;
          }
          mInGap = false ;  
        }
        else
        {
          AddPulse();
          mInGap = true ;  
        }
      }

      AddPulse();

      CurrPulses.SetupGapDurations(); 
    }

    void AddPulse()
    {
      if ( ! mInGap )
      {
        var lSteps = PulseStepBuilder.Build(WaveInput, mPulseStart, mPos ) ;

        CurrPulses.Add( new PulseSymbol(CurrPulses.Count, mPulseStart, mPos, lSteps ) );
      }
    }

    void SelectValidPulses()
    {
      WriteLine2GUI($"Filtering Valid Pulses...");
      Indent();
      WriteDetailLine($"Initial Count={CurrPulses.Count}");
      WriteLine2GUI($"Min Duration Threshold={mOptions.MinDurationThreshold}s");
      WriteLine2GUI($"Miin Level Threshold={mOptions.MinLevelThreshold}%");

      List<PulseSymbol> lValidPulses = new List<PulseSymbol>();

      foreach( var lPulse in CurrPulses )
      {
        if ( lPulse.Duration > mOptions.MinDurationThreshold && lPulse.MaxLevel > mOptions.MinLevelThreshold )
          lValidPulses.Add(lPulse );
      }
      WriteDetailLine($"Final Count={lValidPulses.Count}");
      Unindent();

      mPulsesBag.Add(lValidPulses);

      CurrPulses.SetupGapDurations(); 

      CurrPulses.Plot("0_Initial_Pulses");
    }

    double CalculateMergeThreshold()
    {
      var lGaps = CurrPulses.Gaps();
      FilterHelper.DumpValues("Pulse_Gaps",lGaps);
      var lGMM = GmmFitter.Fit(lGaps);

      double rMergeThreshold = 0.0; 

      if ( lGMM != null)
      {
        const double MIN_SPLIT_SPREAD = 0.0001; 

        // Less than 3 components there are NO splits.
        if ( lGMM.Components.Count == 3)
        {
          if ( lGMM.Components[0].StdDev > MIN_SPLIT_SPREAD )
          {
            double lK0_K1_Midpoint = lGMM.InterpolateMean(0,1); 

            double lK0_2_Sigma = lGMM.Components[0].N_Sigma(2);

            rMergeThreshold = Math.Min(lK0_K1_Midpoint, lK0_2_Sigma)  ;

            double lMergeThreshold2 = lGMM.Intersection(0,1) ;

            AddBranch("ContiguousPulsesGapDuration",$"{(lMergeThreshold2)}");
            AddBranch("ContiguousPulsesGapDuration",$"{(0.01)}");
          } 
        }
        else if ( lGMM.Components.Count >= 4)
        {
          if ( lGMM.Components[0].StdDev > MIN_SPLIT_SPREAD )
            rMergeThreshold = Gmm.Intersection(lGMM.Components[0], lGMM.Components[1] ) ;
        }
      }
      else
      {
        WriteLine("Not enough components in GMM to calculate Merge Threshold.");
      }

      lGMM.Plot("Gaps_Histogram_For_Merge_Threshold_Calculation"); 

      return rMergeThreshold ;
    }

    double FindContiguousPulsesGapDuration()
    {
      if ( mOptions.ContiguousPulsesGapDuration == -1 )
        return CalculateMergeThreshold();

      return mOptions.ContiguousPulsesGapDuration ;
    }

    void MergeContiguousPulses()
    {
      if ( CurrPulses.Count < 2 )
        return ;

      WriteLine2GUI("Merging Contiguous Pulses...");
      Indent();  
      WriteDetailLine($"Initial Count={CurrPulses.Count}");

      double lContiguousPulsesGapDuration = FindContiguousPulsesGapDuration() ; 
      WriteLine2GUI($"Contiguous Pulses Gap Duration Threshold={lContiguousPulsesGapDuration}s");

      var lMergedPulses = new List<PulseSymbol>();

      if ( lContiguousPulsesGapDuration > 0 ) 
      {
        var lPulseA = CurrPulses[0];

        for ( int i = 1; i < CurrPulses.Count ; i++ )
        { 
          var lPulseB = CurrPulses[i]; 
          var lGap = lPulseB.StartTime - lPulseA.EndTime;

          if ( lGap < lContiguousPulsesGapDuration ) 
          {
            WriteDetailLine($"Merging pulses {lPulseA} and {lPulseB}.");
            var lFilteredPulse = PulseSymbol.Merge(lPulseA,lPulseB);  
            lPulseA = lFilteredPulse;
          }
          else 
          {
            WriteDetailLine($"Adding pulse {lPulseA} to result.");
            lMergedPulses.Add(lPulseA);
            lPulseA = lPulseB;
          }
        }

        lMergedPulses.SetupGapDurations(); 
        mPulsesBag.Add(lMergedPulses);
      }

      WriteDetailLine($"Final Count={CurrPulses.Count}");

      CurrPulses.Plot("1_Merged_Pulses");
      Unindent();  
    }

    double FindVeryShortThreshold()
    {
      if ( mOptions.VeryShortThreshold == -1 )
      {
        double rR = 0 ;

        var lGMM = GmmFitter.Fit(CurrPulses.Durations());
        lGMM.Plot("Durations_Histogram_For_VeryShort_Threshold_Calculation"); 

        if ( lGMM.Components.Count < 2 )
        {
          WriteLine2GUI("Not enough components in GMM to calculate Very Short Threshold. Using default value of 0.05s");
          return 0.005 ;
        }

        rR = Gmm.Intersection(lGMM.Components[0], lGMM.Components[1] ) * .5;

        AddBranch("VeryShortThreshold",$"{(rR *   6)}");
        AddBranch("VeryShortThreshold",$"{(rR * 1.4)}");

        return rR;
      }
      else 
        return mOptions.VeryShortThreshold ;
    }

    void RemoveVeryShortPulses()
    {
      double lVeryShortThreshold = FindVeryShortThreshold();

      WriteLine2GUI($"Removing Very Short Pulses...");
      Indent();
      WriteLine2GUI($"Very Short Threshold={lVeryShortThreshold}s");
      WriteDetailLine($"Initial Count={CurrPulses.Count}");

      var lFinalPulses = new List<PulseSymbol>(); 

      lFinalPulses.AddRange( CurrPulses.Where( lPulse => lPulse.Duration >= lVeryShortThreshold ) ) ;

      mPulsesBag.Add(lFinalPulses); 

      CurrPulses.SetupGapDurations(); 

      CurrPulses.Plot("2_Final_Pulses");

      WriteDetailLine($"Final count: {CurrPulses.Count}");
      Unindent();
    }


    class Options
    {
      internal int    MinLevelThreshold ;
      internal double MinDurationThreshold ;
      internal double ContiguousPulsesGapDuration = -1 ;
      internal double VeryShortThreshold = -1 ;
    }

    Options                   mOptions ;
    List< List<PulseSymbol> > mPulsesBag = new List< List<PulseSymbol> >();
    bool                      mInGap ;
    int                       mPulseStart ;
    int                       mPos ;
  }


}
