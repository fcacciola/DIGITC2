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
  public class PulseFilterHelper
  {
    static public PulseSymbol CreateSpecialPulse( PulseSymbol aSource, float aAmplitude )
    {
      var lSteps = new List<PulseStep>
      {
        new PulseStep(aAmplitude, aSource.Start, aSource.End)
      };  
      return new PulseSymbol( aSource.Idx, aSource.Start, aSource.End, lSteps ); 
    }

    static public PulseSymbol CreateOnePulse( PulseSymbol aSource )
    {
      return CreateSpecialPulse(aSource, 0.9f);
    }

    static public PulseSymbol CreateZeroPulse( PulseSymbol aSource )
    {
      return CreateSpecialPulse(aSource, 0.3f);
    }

    static public (DTable,DTable) GetHistogramAndRankSize( List<PulseSymbol> aPulses, bool aNormalizeHistogram = false, bool aNormalizeRankSize = true )
    {
      return GetHistogramAndRankSize( aPulses.ConvertAll( s => s.ToSample() ), aNormalizeHistogram, aNormalizeRankSize ) ;
    }

    static public (DTable,DTable) GetHistogramAndRankSize( List<Sample> aSamples, bool aNormalizeHistogram = false, bool aNormalizeRankSize = true )
    {
      var lDist = new Distribution(aSamples) ;

      var lFullRangeHistogram = new Histogram(lDist).Table ;

      var lFullRangeRankSize = lFullRangeHistogram.ToRankSize();

      var rRankSize = aNormalizeRankSize ? lFullRangeRankSize.Normalized() : lFullRangeRankSize ;

      var rHistogram = aNormalizeHistogram ? lFullRangeHistogram.Normalized() : lFullRangeHistogram ;  

      return (rHistogram, rRankSize);
    }

    static public void PlotPulses( List<PulseSymbol> aPulses, string aLabel )
    {
      if ( DContext.Session.Args.GetBool("Plot") )
      {
        List<float> lSamples = new List<float> ();
        aPulses.ForEach( s => s.DumpSamples(lSamples ) );
        DiscreteSignal lWaveRep = new DiscreteSignal(SIG.SamplingRate, lSamples);
        WaveSignal lWave = new WaveSignal(lWaveRep);
        lWave.SaveTo( DContext.Session.OutputFile( "Pulses" + aLabel + ".wav") ) ;
      }
    }

    static public void PlotPulseDurationHistogram( List<PulseSymbol> aPulses, string aName )
    {
      (DTable lHistogram, DTable lRankSize) = GetHistogramAndRankSize(aPulses) ;  

      if ( DContext.Session.Args.GetBool("Plot") )
      { 
        lHistogram.CreatePlot(Plot.Options.Bars).SavePNG(DContext.Session.OutputFile($"{aName}_Durations_Histogram.png"));
        lRankSize .CreatePlot(Plot.Options.Bars).SavePNG(DContext.Session.OutputFile($"{aName}_Durations_RankSize.png"));
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

      //DContext.WriteLine($"Creating steps for pulse from {mPulseStart} to {mPulseEnd}");
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
        //DContext.WriteLine($"  Step of {mAmplitude} from {mStepStart} to {mPos}");
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

    protected override void Process (WaveSignal aInput, Packet aInputPacket, List<Packet> rOutput )
    {
      Process( new Options(){ Label              = "A"
                            , VeryShortThreshold = SIG.SamplingRate / 1000 * 15 
                            , DullThreshold      = 5
                            , SplitThreshold     = 5
                            , SplitLevelDiff     = 5
                            }
             , aInput
             , aInputPacket
             , rOutput );

    }

    void Process ( Options aOptions, WaveSignal aInput, Packet aInputPacket, List<Packet> rOutput )
    {
      mInput   = aInput ; 

      mData = new Data(){ Options = aOptions } ;

      CreatePulses();
      SelectValidPulses();
      SplitPulses();  
      MergeContiguousPulses();
      RemoveUnfitPulses();

      rOutput.Add( new Packet(Name, aInputPacket, new LexicalSignal(mData.Pulses4), mData.Options.Label) ) ;
    }
    
    public override string Name => this.GetType().Name ;

    void CreatePulses()
    {
      mData.InGap = mInput.Samples[0] == 0 ;

      mData.PulseStart = 0 ;
      mData.Pos        = 0 ;

      DContext.WriteLine($"Creating pulses for WaveSignal of Length {mInput.Samples.Length}");
      DContext.Indent();  

      for ( mData.Pos = 0 ; mData.Pos < mInput.Samples.Length ; ++ mData.Pos )
      {
        float lV = mInput.Samples[mData.Pos] ;

        if ( lV > 0 )
        {
          if ( mData.InGap )
          {
            mData.PulseStart = mData.Pos ;
            //DContext.WriteLine($"Pulse start at {mData.PulseStart}");
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

      PulseFilterHelper.PlotPulses(mData.Pulses0, $"{mData.Options.Label}_0");

      DContext.Unindent();  
    }

    void AddPulse()
    {
      if ( ! mData.InGap )
      {
        var lSteps = PulseStepBuilder.Build(mInput, mData.PulseStart, mData.Pos ) ;

        //DContext.WriteLine($"Creatign new Pulse from {mData.PulseStart} to {mData.Pos} with {lSteps.Textualize()}");

        mData.Pulses0.Add( new PulseSymbol(mData.Pulses0.Count, mData.PulseStart, mData.Pos, lSteps ) );
      }
    }

    void SelectValidPulses()
    {
      double lInsignificantDurationThreshold = 0.02 ;

      foreach( var lPulse in mData.Pulses0 )
      {
        if ( lPulse.Length > lInsignificantDurationThreshold && lPulse.MaxLevel > mData.Options.DullThreshold )
          mData.Pulses1.Add(lPulse );
      }

      PulseFilterHelper.PlotPulses(mData.Pulses1, $"{mData.Options.Label}_1");
      PulseFilterHelper.PlotPulseDurationHistogram(mData.Pulses1, $"{mData.Options.Label}_1");
    }

    struct Run
    {
      internal int  From ;
      internal int  To ;
      internal bool Gap ;
    }

    List<Run> FindRuns( PulseSymbol aPulse )
    {
      //DContext.WriteLine($"Finding runs");
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

                //DContext.WriteLine($"Local minima found at step {i}");

                lRuns.Add( new Run{ From = i, To = k, Gap = true} ) ;

                lRunStart = k ;
              }
            }
          }
        }
        //DContext.Unindent();  
      }

      if ( lRunStart < lC )
        lRuns.Add( new Run{ From = lRunStart, To = lC, Gap = false} ) ;

      return lRuns;
    }

    void SplitPulse( PulseSymbol aPulse, List<Run> aRuns )
    {
      //DContext.WriteLine($"Splittig pulse with {aRuns.Count} runs");
      //DContext.Indent();  

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
          var lNewPulse = new PulseSymbol(mData.Pulses2.Count, lNewPulseStart, lNewPulseEnd, lSteps );
          mData.Pulses2.Add(lNewPulse);
        }
      }
      //DContext.Unindent();  
    }

    void SplitPulses()
    {
      DContext.WriteLine("Splitting Pulses");
      DContext.Indent();  

      foreach( var lPulse in mData.Pulses1 )
      {
        //DContext.WriteLine($"Checking  pulse {lPulse}");
        //DContext.Indent();  
        var lRuns = FindRuns( lPulse );  
        if (  lRuns.Count > 1 )  
        {
          SplitPulse(lPulse,lRuns);
        }
        else
        {
          mData.Pulses2.Add( lPulse ); 
        }
        //DContext.Unindent();  
      }

      PulseFilterHelper.PlotPulses(mData.Pulses2, $"{mData.Options.Label}_2");
      PulseFilterHelper.PlotPulseDurationHistogram(mData.Pulses2, $"{mData.Options.Label}_2");
      DContext.Unindent();  
    }

    double FindContiguousPulsesGapDuration()
    {
      double rR = 0 ;

      var lDurations = mData.Pulses2.ConvertAll( s => s.ToSample() ) ;

      var lDist = new Distribution( lDurations ) ;

      var lFullRangeHistogram = new Histogram(lDist).Table ;

      var lXPs = ExtremePointsFinder.Find(lFullRangeHistogram.Points);

      var lRawPeaks1 = lXPs.Where( xp => xp.IsPeak).OrderByDescending( xp => xp.Value.Y ).ToList();
      var lRawPeaks2 = lRawPeaks1.OrderBy( xp => xp.Value.X.Value ).ToList();
      var lPeaks = lRawPeaks2.ConvertAll( p => p.Value.X.Value ) ; // These are the peak durations from shorest to largest

      if ( lPeaks.Count >= 3 ) 
      {  
        rR = MathX.LERP(lPeaks[0],lPeaks[1],.4);
      }

      return rR;
    }

    void MergeContiguousPulses()
    {
      if ( mData.Pulses2.Count < 2 )
        return ;

      DContext.WriteLine("Merging Contiguous Pulses");
      DContext.Indent();  

      double lContiguousPulsesGapDuration = FindContiguousPulsesGapDuration() ; //0.0070 ;

      var lPulseA = mData.Pulses2[0];

      for ( int i = 1; i < mData.Pulses2.Count ; i++ )
      { 
        var lPulseB = mData.Pulses2[i]; 

        var lGap = lPulseB.StartTime - lPulseA.EndTime;

        if ( lGap < lContiguousPulsesGapDuration ) 
        {
          DContext.WriteLine($"Merging pulses {lPulseA} and {lPulseB}.");
          var lFilteredPulse = PulseSymbol.Merge(lPulseA,lPulseB);  
          lPulseA = lFilteredPulse;
        }
        else 
        {
          DContext.WriteLine($"Adding pulse {lPulseA} to result.");
          mData.Pulses3.Add(lPulseA);
          lPulseA = lPulseB;
        }
      }

      PulseFilterHelper.PlotPulses(mData.Pulses3, $"{mData.Options.Label}_3");
      DContext.Unindent();  
    }

    double FindVeryShortThreshold()
    {
      double rR = 0 ;

      var lDurations = mData.Pulses3.ConvertAll( s => s.ToSample() ) ;

      var lDist = new Distribution( lDurations ) ;

      var lFullRangeHistogram = new Histogram(lDist).Table ;

      var lXPs = ExtremePointsFinder.Find(lFullRangeHistogram.Points);

      var lRawPeaks1 = lXPs.Where( xp => xp.IsPeak).OrderByDescending( xp => xp.Value.Y ).ToList();
      var lRawPeaks2 = lRawPeaks1.OrderBy( xp => xp.Value.X.Value ).ToList();
      var lPeaks = lRawPeaks2.ConvertAll( p => p.Value.X.Value ) ; // These are the peak durations from shorest to largest

      if ( lPeaks.Count >= 2 ) 
      {  
        rR = MathX.LERP(lPeaks[0],lPeaks[1],.6);
      }

      return rR;
    }

    void RemoveUnfitPulses()
    {
      double lVeryShortPulses = FindVeryShortThreshold();

      mData.Pulses4.AddRange( mData.Pulses3.Where( lPulse => lPulse.Duration >= lVeryShortPulses ) ) ;

      PulseFilterHelper.PlotPulses(mData.Pulses4, $"{mData.Options.Label}_4");
    }


    class Options
    {
      internal string Label ;
      internal int    VeryShortThreshold ;
      internal int    DullThreshold ;
      internal int    SplitThreshold ;
      internal int    SplitLevelDiff ;
    }

    class Data
    {
      internal Options           Options ;
      internal List<PulseSymbol> Pulses0 = new List<PulseSymbol>();
      internal List<PulseSymbol> Pulses1 = new List<PulseSymbol>();
      internal List<PulseSymbol> Pulses2 = new List<PulseSymbol>();
      internal List<PulseSymbol> Pulses3 = new List<PulseSymbol>() ;
      internal List<PulseSymbol> Pulses4 = new List<PulseSymbol>() ;
      internal bool              InGap ;
      internal int               PulseStart ;
      internal int               Pos ;
    }

    WaveSignal mInput ;
    Data       mData ;

  }


}
