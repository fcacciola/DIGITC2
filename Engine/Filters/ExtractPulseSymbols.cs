using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Operations;
using NWaves.Signals;

using MathNet.Numerics.Statistics;
using System.Runtime.InteropServices;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DIGITC2
{
  internal class PulseFilterHelper
  {
    static internal (DTable,DTable) GetHistogramAndRankSize( List<PulseSymbol> aPulses )
    {
      var lDist = new Distribution( aPulses.ConvertAll( s => s.ToSample() ) ) ;

      var rHistogram = new Histogram(lDist).Table ;

      var lFullRangeRankSize = rHistogram.ToRankSize();

      var rRankSize = lFullRangeRankSize.Normalized();

      return (rHistogram, rRankSize);
    }

    static internal void PlotPulses( List<PulseSymbol> aPulses, int aSamplingRate, string aLabel )
    {
      if ( Context.Session.Args.GetBool("Plot") )
      {
        List<float> lSamples = new List<float> ();
        aPulses.ForEach( s => s.DumpSamples(lSamples ) );
        DiscreteSignal lWaveRep = new DiscreteSignal(aSamplingRate, lSamples);
        WaveSignal lWave = new WaveSignal(lWaveRep);
        lWave.SaveTo( Context.Session.LogFile( "Pulses" + aLabel + ".wav") ) ;
      }
    }

    static internal void PlotPulseDurationHistogram( List<PulseSymbol> aPulses, string aName )
    {
      (DTable lHistogram, DTable lRankSize) = GetHistogramAndRankSize(aPulses) ;  

      if ( Context.Session.Args.GetBool("Plot") )
      { 
        lHistogram.CreatePlot(Plot.Options.Bars).SavePNG(Context.Session.LogFile($"{aName}_Durations_Histogram.png"));
        lRankSize .CreatePlot(Plot.Options.Bars).SavePNG(Context.Session.LogFile($"{aName}_Durations_RankSize.png"));
      }

    }
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

      Context.WriteLine($"Creating steps for pulse from {mPulseStart} to {mPulseEnd}");
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
        Context.WriteLine($"  Step of {mAmplitude} from {mStepStart} to {mPos}");
        mSteps.Add( new PulseStep(mAmplitude, mStepStart, mPos, (double)mCurrCount / (double)mSource.SamplingRate ) );
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
      mDullThreshold  = Context.Session.Args.GetOptionalInt("Pulses_DullThreshold") .GetValueOrDefault(35);
      mSplitThreshold = Context.Session.Args.GetOptionalInt("Pulses_SplitThreshold").GetValueOrDefault(25);
    }

    protected override Step Process ( WaveSignal aInput, Step aStep )
    {
      mInput = aInput ; 

      CreatePulses();
      RemoveDullPulses();
      SplitPulses();  
      RemoveUnfitPulses();

      mStep = aStep.Next( new LexicalSignal(mFinalPulses), "Raw Pulses", this) ;

      return mStep ;
    }
    
    protected override string Name => "ExtractPulses" ;

    void CreatePulses()
    {
      mRawPulses = new List<PulseSymbol>();

      mInGap = mInput.Samples[0] == 0 ;

      mPulseStart = 0 ;
      mPos        = 0 ;

      Context.WriteLine($"Creating pulses for WaveSignal of Length {mInput.Samples.Length}");

      for ( mPos = 0 ; mPos < mInput.Samples.Length ; ++ mPos )
      {
        float lV = mInput.Samples[mPos] ;

        if ( lV > 0 )
        {
          if ( mInGap )
          {
            mPulseStart = mPos ;
            Context.WriteLine($"Pulse start at {mPulseStart}");
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

      PulseFilterHelper.PlotPulses(mRawPulses, mInput.SamplingRate, "Raw");
      PulseFilterHelper.PlotPulseDurationHistogram(mRawPulses, "Raw");
    }

    void AddPulse()
    {
      if ( ! mInGap )
      {
        var lSteps = PulseStepBuilder.Build(mInput, mPulseStart, mPos ) ;

        Context.WriteLine($"Creatign new Pulse from {mPulseStart} to {mPos} with {lSteps}");

        mRawPulses.Add( new PulseSymbol(mRawPulses.Count, mInput.SamplingRate, mPulseStart, mPos, lSteps ) );
      }
    }

    void RemoveDullPulses()
    {
      mLoudPulses = new List<PulseSymbol>();

      foreach( var lPulse in mRawPulses )
      {
        if ( lPulse.MaxLevel > mDullThreshold )
          mLoudPulses.Add(lPulse );
      }

      PulseFilterHelper.PlotPulses(mLoudPulses, mInput.SamplingRate, "Loud");
      PulseFilterHelper.PlotPulseDurationHistogram(mLoudPulses, "Loud");
    }

    struct Run
    {
      internal int  From ;
      internal int  To ;
      internal bool Gap ;
    }

    List<Run> FindRuns( PulseSymbol aPulse )
    {
      Context.WriteLine($"    Finding runs");
      List<Run> lRuns = new List<Run>();

      int lC = aPulse.Steps.Count ;
      int lL = lC - 1 ;

      int lRunStart = 0 ;

      for ( int i = 0 ; i < lC  ; ++ i )
      {
        var lStep = aPulse.Steps[i];

        int lLevel = lStep.Level ;

        if ( i > 0 && i < lL )
        {
          bool lDullEnough  = lStep.Level < mSplitThreshold ;
          if ( lDullEnough )
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
                if ( lDiff >= 20 )
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
                  if ( lDiff >= 20 )
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

                Context.WriteLine($"      Local minima found at step {i}");

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
      Context.WriteLine($"    Splittig pulse with {aRuns.Count} runs");

      foreach( var lRun in aRuns ) 
      {
        if ( !lRun.Gap )
        {
          Context.WriteLine($"      Loud run from {lRun.From} to {lRun.To}");

          List<PulseStep> lSteps = new List<PulseStep>();
          for( int i = lRun.From ; i < lRun.To ; ++ i )
            lSteps.Add(aPulse.Steps[i]);

          var lNewPulseStart = lSteps.First().Start ;
          var lNewPulseEnd   = lSteps.Last ().End ;  
          var lNewPulse = new PulseSymbol(mSplitPulses.Count, mInput.SamplingRate, lNewPulseStart, lNewPulseEnd, lSteps );
          mSplitPulses.Add(lNewPulse);
        }
      }
    }

    void SplitPulses()
    {
      mSplitPulses = new List<PulseSymbol>();

      Context.WriteLine("Splitting Pulses");

      foreach( var lPulse in mLoudPulses )
      {
        Context.WriteLine($"  Checking  pulse {lPulse}");
        var lRuns = FindRuns( lPulse );  
        if (  lRuns.Count > 1 )  
        {
          SplitPulse(lPulse,lRuns);
        }
        else
        {
          mSplitPulses.Add( lPulse ); 
        }
      }

      PulseFilterHelper.PlotPulses(mSplitPulses, mInput.SamplingRate, "Split");
      PulseFilterHelper.PlotPulseDurationHistogram(mSplitPulses, "Split");
    }

    void RemoveUnfitPulses()
    {
      mFinalPulses = new List<PulseSymbol>();

      mFinalPulses = mSplitPulses ;
    }

    WaveSignal        mInput ;
    bool              mInGap ;
    int               mPulseStart ;
    int               mPos ;
    int               mDullThreshold ;
    int               mSplitThreshold ;
    List<PulseSymbol> mRawPulses ;
    List<PulseSymbol> mLoudPulses ;
    List<PulseSymbol> mSplitPulses ;
    List<PulseSymbol> mFinalPulses ;
  }


}
