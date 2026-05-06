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
      rOptions.SplitValleyThreshold        = Params.GetInt("SplitValleyThreshold");
      rOptions.SplitHillLevelDiff          = Params.GetInt("SplitHillLevelDiff");
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
      SplitPulses();  
      MergeContiguousPulses();
      RemoveVeryShortPulses();

      return CreateOutput( new LexicalSignal(mPulses4), Name, null, false) ;
    }
   
    public override string Name => this.GetType().Name ;

    void CreatePulses()
    {
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

      mPulses0.SetupGapDurations(); 

      mPulses0.Plot("0_Raw_Pulses");
    }

    void AddPulse()
    {
      if ( ! mInGap )
      {
        var lSteps = PulseStepBuilder.Build(WaveInput, mPulseStart, mPos ) ;

        mPulses0.Add( new PulseSymbol(mPulses0.Count, mPulseStart, mPos, lSteps ) );
      }
    }

    void SelectValidPulses()
    {
      WriteLine2GUI($"Filtering Valid Pulses...");
      Indent();
      WriteDetailLine($"Initial Count={mPulses0.Count}");
      WriteLine2GUI($"Min Duration Threshold={mOptions.MinDurationThreshold}s");
      WriteLine2GUI($"Miin Level Threshold={mOptions.MinLevelThreshold}%");
      foreach( var lPulse in mPulses0 )
      {
        if ( lPulse.Duration > mOptions.MinDurationThreshold && lPulse.MaxLevel > mOptions.MinLevelThreshold )
          mPulses1.Add(lPulse );
      }
      WriteDetailLine($"Final Count={mPulses1.Count}");
      Unindent();

      mPulses1.SetupGapDurations(); 

      mPulses1.Plot("1_Valid_Pulses");
    }

    struct Run
    {
      internal int  From ;
      internal int  To ;
      internal bool Gap ;
    }

    List<Run> FindRuns( PulseSymbol aPulse )
    {
      List<Run> lRuns = new List<Run>();

      int lC = aPulse.Steps.Count ;
      int lL = lC - 1 ;

      int lRunStart = 0 ;

      for ( int i = 0 ; i < lC  ; ++ i )
      {
        //DContext.Indent();  
        var lStep = aPulse.Steps[i];

        int lLevel = lStep.Level ;

        if ( i > 0 && i < lL )
        {
          bool lValley  = lStep.Level < mOptions.SplitValleyThreshold ;
          if ( lValley )
          {
            bool lFromPeak = false ;

            float lCurrLevel = lLevel ;
            float lDiff = 0 ;
            for ( int j = i - 1 ; j  >= 0 ; j-- )
            {
              var lPrev = aPulse.Steps[j];  
              float lPrevLevel = lPrev.Level ;
              if ( lPrevLevel >= lCurrLevel)
              {
                lDiff += lPrevLevel - lCurrLevel ;
                if ( lDiff >= mOptions.SplitHillLevelDiff )
                {
                  lFromPeak = true ;  
                  break ;
                }
              }
              else
              {
                break ;
              }

              lCurrLevel = lPrevLevel ;
            }

            if ( lFromPeak )
            {
              bool lToPeak = false ;

              int k = i + 1 ;

              lCurrLevel = lLevel ;
              lDiff = 0 ;
              for ( int j = i + 1 ; j  < lC ; j ++ )
              {
                var lNext = aPulse.Steps[j];  
                float lNextLevel = lNext.Level ;
                if ( lNextLevel == lCurrLevel ) 
                  k = j ;
                if ( lNextLevel >= lCurrLevel)
                {
                  lDiff += lNextLevel - lCurrLevel ;
                  if ( lDiff >= mOptions.SplitHillLevelDiff )
                  {
                    lToPeak = true ;  
                    break ;
                  }
                }
                else
                {
                  break ;
                }

                lCurrLevel = lNextLevel ;
              }

              if ( lToPeak )  
              {
                if ( lRunStart < i )
                  lRuns.Add( new Run{ From = lRunStart, To = i, Gap = false} ) ;

                lRuns.Add( new Run{ From = i, To = k, Gap = true} ) ;

                lRunStart = k ;
              }
            }
          }
        }
      }

      if ( lRunStart < lC )
        lRuns.Add( new Run{ From = lRunStart, To = lC, Gap = false} ) ;


      return lRuns;
    }

    void SplitPulse( PulseSymbol aPulse, List<Run> aRuns )
    {
      foreach( var lRun in aRuns ) 
      {
        if ( !lRun.Gap )
        {
          //DContext.WriteLine($"Loud run from {lRun.From} to {lRun.To}");

          List<PulseStep> lSteps = new List<PulseStep>();
          for( int i = lRun.From ; i < lRun.To ; ++ i )
            lSteps.Add(aPulse.Steps[i]);

          var lNewPulseStart = lSteps.First().Start ;
          var lNewPulseEnd   = lSteps.Last ().End ;  
          var lNewPulse = new PulseSymbol(mPulses2.Count, lNewPulseStart, lNewPulseEnd, lSteps );
          mPulses2.Add(lNewPulse);
        }
      }
    }

    void SplitPulses()
    {
mPulses2 = mPulses1;  

      //WriteLine2GUI("Splitting Glued Pulses...");
      //Indent();
      //WriteDetailLine($"Initial Count={mPulses1.Count}");
      //WriteLine2GUI($"Split Valley Threshold={mOptions.SplitValleyThreshold}%");
      //WriteLine2GUI($"Split Hill Level Diff={mOptions.SplitHillLevelDiff}%");

      //foreach (var lPulse in mPulses1)
      //{
      //  var lRuns = FindRuns(lPulse);
      //  if (lRuns.Count > 1)
      //  {
      //    SplitPulse(lPulse, lRuns);
      //  }
      //  else
      //  {
      //    mPulses2.Add(lPulse);
      //  }
      //}

      //mPulses2.SetupGapDurations();

      //mPulses2.Plot("2_Split_Pulses");

      //WriteDetailLine($"Final Runs Count={mPulses2.Count}");
      //Unindent();
    }

    double CalculateMergeThreshold()
    {
      var lGaps = mPulses2.Gaps();
      FilterHelper.DumpValues("Pulses2_Gaps",lGaps);
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
      if ( mPulses2.Count < 2 )
        return ;

      WriteLine2GUI("Merging Contiguous Pulses...");
      Indent();  
      WriteDetailLine($"Initial Count={mPulses2.Count}");

      double lContiguousPulsesGapDuration = FindContiguousPulsesGapDuration() ; 
      WriteLine2GUI($"Contiguous Pulses Gap Duration Threshold={lContiguousPulsesGapDuration}s");

      if ( lContiguousPulsesGapDuration > 0 ) 
      {
        var lPulseA = mPulses2[0];

        for ( int i = 1; i < mPulses2.Count ; i++ )
        { 
          var lPulseB = mPulses2[i]; 

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
            mPulses3.Add(lPulseA);
            lPulseA = lPulseB;
          }
        }
      }
      else
      {
        mPulses3 = mPulses2 ;
      }

      mPulses3.SetupGapDurations(); 

      WriteDetailLine($"Final Count={mPulses3.Count}");

      mPulses3.Plot("3_Contiguous_Pulses_Merged");
      Unindent();  
    }

    double FindVeryShortThreshold()
    {
      if ( mOptions.VeryShortThreshold == -1 )
      {
        double rR = 0 ;

        var lGMM = GmmFitter.Fit(mPulses3.Durations());
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
      WriteDetailLine($"Initial Count={mPulses2.Count}");

      mPulses4.AddRange( mPulses3.Where( lPulse => lPulse.Duration >= lVeryShortThreshold ) ) ;

      mPulses4.SetupGapDurations(); 

      mPulses4.Plot("4_Final_Pulses");

      WriteDetailLine($"Final count: {mPulses4.Count}");
      Unindent();
    }


    class Options
    {
      internal int    MinLevelThreshold ;
      internal int    SplitValleyThreshold ;
      internal int    SplitHillLevelDiff ;
      internal double MinDurationThreshold ;
      internal double ContiguousPulsesGapDuration = -1 ;
      internal double VeryShortThreshold = -1 ;
    }

    Options           mOptions ;
    List<PulseSymbol> mPulses0 = new List<PulseSymbol>();
    List<PulseSymbol> mPulses1 = new List<PulseSymbol>();
    List<PulseSymbol> mPulses2 = new List<PulseSymbol>();
    List<PulseSymbol> mPulses3 = new List<PulseSymbol>() ;
    List<PulseSymbol> mPulses4 = new List<PulseSymbol>() ;
    bool              mInGap ;
    int               mPulseStart ;
    int               mPos ;
  }


}
