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
  public class PulseFilterHelper
  {
    static public PulseSymbol CreateOnePulse( PulseSymbol aSource )
    {
    }

    static public PulseSymbol CreateZeroPulse( PulseSymbol aSource )
    {
    }

    static public (DTable,DTable) GetHistogramAndRankSize( List<PulseSymbol> aPulses )
    {
      var lDist = new Distribution( aPulses.ConvertAll( s => s.ToSample() ) ) ;

      var rHistogram = new Histogram(lDist).Table ;

      var lFullRangeRankSize = rHistogram.ToRankSize();

      var rRankSize = lFullRangeRankSize.Normalized();

      return (rHistogram, rRankSize);
    }

    static public void PlotPulses( List<PulseSymbol> aPulses, int aSamplingRate, string aLabel )
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

    static public void PlotPulseDurationHistogram( List<PulseSymbol> aPulses, string aName )
    {
      (DTable lHistogram, DTable   lRankSize) = GetHistogramAndRankSize(aPulses) ;  

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
    }

    protected override void Process (WaveSignal aInput, Branch aInputBranch, List<Branch> rOutput )
    {
      Process( new Options(){ Label = "A", DullThreshold = 35, SplitThreshold = 25, SplitLevelDiff = 20 }
             , aInput, aInputBranch, rOutput );

    }

    void Process ( Options aOptions, WaveSignal aInput, Branch aInputBranch, List<Branch> rOutput )
    {
      mInput   = aInput ; 

      mData = new Data(){ Options = aOptions } ;

      CreatePulses();
      RemoveDullPulses();
      SplitPulses();  
      RemoveUnfitPulses();

      rOutput.Add( new Branch(new LexicalSignal(mData.FinalPulses), mData.Options.Label) ) ;
    }
    
    protected override string Name => "ExtractPulses" ;

    void CreatePulses()
    {
      mData.InGap = mInput.Samples[0] == 0 ;

      mData.PulseStart = 0 ;
      mData.Pos        = 0 ;

      Context.WriteLine($"Creating pulses for WaveSignal of Length {mInput.Samples.Length}");

      for ( mData.Pos = 0 ; mData.Pos < mInput.Samples.Length ; ++ mData.Pos )
      {
        float lV = mInput.Samples[mData.Pos] ;

        if ( lV > 0 )
        {
          if ( mData.InGap )
          {
            mData.PulseStart = mData.Pos ;
            Context.WriteLine($"Pulse start at {mData.PulseStart}");
          }
          mData.InGap = false ;  
        }
        else
        {
          AddPulse();
          mData.InGap = true ;  
        }
      }

      AddPulse();

      PulseFilterHelper.PlotPulses(mData.RawPulses, mInput.SamplingRate, $"{mData.Options.Label}_Raw");
      PulseFilterHelper.PlotPulseDurationHistogram(mData.RawPulses, $"{mData.Options.Label}_Raw");
    }

    void AddPulse()
    {
      if ( ! mData.InGap )
      {
        var lSteps = PulseStepBuilder.Build(mInput, mData.PulseStart, mData.Pos ) ;

        Context.WriteLine($"Creatign new Pulse from {mData.PulseStart} to {mData.Pos} with {lSteps}");

        mData.RawPulses.Add( new PulseSymbol(mData.RawPulses.Count, mInput.SamplingRate, mData.PulseStart, mData.Pos, lSteps ) );
      }
    }

    void RemoveDullPulses()
    {
      foreach( var lPulse in mData.RawPulses )
      {
        if ( lPulse.MaxLevel > mData.Options.DullThreshold )
          mData.LoudPulses.Add(lPulse );
      }

      PulseFilterHelper.PlotPulses(mData.LoudPulses, mInput.SamplingRate, $"{mData.Options.Label}_Loud");
      PulseFilterHelper.PlotPulseDurationHistogram(mData.LoudPulses, $"{mData.Options.Label}_Loud");
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
          bool lDullEnough  = lStep.Level < mData.Options.SplitThreshold ;
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
                if ( lDiff >= mData.Options.SplitLevelDiff )
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
                  if ( lDiff >= mData.Options.SplitLevelDiff )
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
          var lNewPulse = new PulseSymbol(mData.SplitPulses.Count, mInput.SamplingRate, lNewPulseStart, lNewPulseEnd, lSteps );
          mData.SplitPulses.Add(lNewPulse);
        }
      }
    }

    void SplitPulses()
    {
      Context.WriteLine("Splitting Pulses");

      foreach( var lPulse in mData.LoudPulses )
      {
        Context.WriteLine($"  Checking  pulse {lPulse}");
        var lRuns = FindRuns( lPulse );  
        if (  lRuns.Count > 1 )  
        {
          SplitPulse(lPulse,lRuns);
        }
        else
        {
          mData.SplitPulses.Add( lPulse ); 
        }
      }

      PulseFilterHelper.PlotPulses(mData.SplitPulses, mInput.SamplingRate, $"{mData.Options.Label}_Split");
      PulseFilterHelper.PlotPulseDurationHistogram(mData.SplitPulses, $"{mData.Options.Label}_Split");
    }

    void RemoveUnfitPulses()
    {
      mData.FinalPulses.AddRange( mData.SplitPulses ) ;
    }

    class Options
    {
      internal string Label ;
      internal int    DullThreshold ;
      internal int    SplitThreshold ;
      internal int    SplitLevelDiff ;
    }

    class Data
    {
      internal Options           Options ;
      internal List<PulseSymbol> RawPulses   = new List<PulseSymbol>();
      internal List<PulseSymbol> LoudPulses  = new List<PulseSymbol>();
      internal List<PulseSymbol> SplitPulses = new List<PulseSymbol>();
      internal List<PulseSymbol> FinalPulses = new List<PulseSymbol>() ;
      internal bool              InGap ;
      internal int               PulseStart ;
      internal int               Pos ;
    }

    WaveSignal mInput ;
    Data       mData ;

  }


}
