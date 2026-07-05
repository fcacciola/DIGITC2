using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace ENGINE
{
  
  public class Score
  {
    public Score( string aFilterName, double aValue, bool aIsCoverage = false )
    {
      FilterName = aFilterName;
      Value      = aValue;
      IsCoverage = aIsCoverage; // Values are either a coverage fraction (0..1) or a likelihood score (unbounded)
    }

    public override string ToString() => $"{FilterName}: {Value:F4}{(IsCoverage ? " Coverage" : " Likelihood")}";

    public string  FilterName ;
    public double  Value        = 0.0 ;
    public bool    IsCoverage ;
  }


  // ---------------------------------------------------------------------------
  //  Online mean/variance (Welford). Used only during calibration.
  // ---------------------------------------------------------------------------
  public class RunningStats
  {
    public void Add( double aValue )
    {
      mCount += 1;
      double lDelta  = aValue - mMean;
      mMean  += lDelta / mCount;
      double lDelta2 = aValue - mMean;
      mM2    += lDelta * lDelta2;
    }

    public FilterScoreBaseline Snapshot() => new FilterScoreBaseline( mMean, StdDev, mCount );

    public long    Count     => mCount;
    public double  Mean      => mMean;
    public double  Variance  => mCount > 1 ? mM2 / (mCount - 1) : 0.0;
    public double  StdDev    => Math.Sqrt( Variance );

    long    mCount  = 0 ;
    double  mMean   = 0.0 ;
    double  mM2     = 0.0 ;
  }

  public class FilterScoreBaseline
  {
    public FilterScoreBaseline()
    {
    }

    public FilterScoreBaseline( double aMean, double aStd, long aCount )
    {
      Mean   = aMean;
      Std    = aStd;
      Count  = aCount;
    }

    public double  Mean   { get; set; }
    public double  Std    { get; set; }
    public long    Count  { get; set; }
  }


  // ---------------------------------------------------------------------------
  //  Result of combining one branch's per-filter scores. Carries the breakdown.
  // ---------------------------------------------------------------------------
  public class CombinedScore
  {
    public CombinedScore( double aValue, double aCoverage, IReadOnlyList<(string Name, double Z)> aTerms )
    {
      Value     = aValue;
      Coverage  = aCoverage;
      Terms     = aTerms;
    }

    public override string ToString()
    {
      string lTerms   = string.Join( ", ", Terms.Select( t => $"{t.Name}={t.Z:F2}σ" ) );
      string lVerdict = Accepted.HasValue ? (Accepted.Value ? " [ACCEPT]" : " [REJECT]") : "";
      return $"S={Value:F3} (coverage={Coverage:F2}; {lTerms}){lVerdict}";
    }

    public double                                  Value     ;
    public double                                  Coverage  ;
    public IReadOnlyList<(string Name, double Z)>  Terms     ;
    public bool?                                   Accepted  ;   // null until Decide() runs
  }


  // ---------------------------------------------------------------------------
  //  The serializable calibration blob. This is the whole shipped artifact.
  // ---------------------------------------------------------------------------
  public class Calibration
  {
    public int                                      SchemaVersion     { get; set; } = 1;
    public Dictionary<string, FilterScoreBaseline>  Baselines         { get; set; } = new Dictionary<string, FilterScoreBaseline>();
    public double[]                                 NullWinnerScores  { get; set; }   // sorted; threshold derivable at any quantile
    public double                                   RejectThreshold   { get; set; }
    public double                                   Quantile          { get; set; }
    public int                                      RefInputCount     { get; set; }
    public string                                   CalibratedUtc     { get; set; }

    public string ToJson() => JsonConvert.SerializeObject( this, Formatting.Indented );

    public static Calibration FromJson( string aJson ) => JsonConvert.DeserializeObject<Calibration>( aJson );

    public void Save( string aPath ) => File.WriteAllText( aPath, ToJson() );

    public static Calibration Load( string aPath ) => FromJson( File.ReadAllText( aPath ) );
  }


  // ---------------------------------------------------------------------------
  //  Combines per-filter scores into a single standardized score, selects the
  //  winning branch, and decides accept/reject against a reference-calibrated null.
  //
  //    S = coverage * Σ z_i      (coverage is a 0..1 reliability multiplier)
  //
  //  The coverage score (flagged Score.IsCoverage) is NOT a peer term; it scales
  //  the linguistic evidence so low coverage shrinks it toward zero.
  // ---------------------------------------------------------------------------
  public class ScoreModel
  {
    public ScoreModel( DriverApp aGUI )
    {
      mDriverApp = aGUI;
    }

    // ---- Calibration: feed NOISE inputs. Each input produced a set of candidate
    //      branches; each branch is its list of raw per-filter Scores. ----

    public void AddRefInput( IReadOnlyList<IReadOnlyList<Score>> aBranches )
    {
      mDriverApp?.AddMessage($"ScoreModel.AddRefInput: {aBranches.Count} branches");

      // Per-filter baseline is taken over ALL branches (more samples is better);
      // the winner-selection bias is handled separately, on the combined score.
      foreach (IReadOnlyList<Score> lBranch in aBranches)
      {
        foreach (Score lScore in lBranch)
        {
          mDriverApp?.AddMessage( $"  {lScore}" );

          if (!lScore.IsCoverage)
            GetStats( lScore.FilterName ).Add( lScore.Value );

        }

        mRefInputs.Add( aBranches );
      }
    }

    public void AddRefInput( IReadOnlyList<PipelineResult> aBranches ) => AddRefInput( ProjectScores( aBranches ) );

    public void Calibrate( double aRejectQuantile = 0.99 )
    {
      mDriverApp?.AddMessage($"ScoreModel.Calibrate: {mRefInputs.Count} inputs");

      // 1) Freeze per-filter baselines from the accumulators.
      mBaselines = new Dictionary<string, FilterScoreBaseline>();
      foreach (KeyValuePair<string, RunningStats> lKV in mFilterStats)
        mBaselines[lKV.Key] = lKV.Value.Snapshot();

      // 2) With baselines known, take the WINNER's combined score per reference input.
      //    Thresholding the max-over-branches corrects the selection bias.
      List<double> lWinners = new List<double>( mRefInputs.Count );
      foreach (IReadOnlyList<IReadOnlyList<Score>> lBranches in mRefInputs)
      {
        CombinedScore lWinner = SelectWinner( lBranches );
        if (lWinner != null)
          lWinners.Add( lWinner.Value );
      }
      lWinners.Sort();

      mNullWinnerScores = lWinners;
      mRefInputCount    = lWinners.Count;
      mQuantile         = aRejectQuantile;
      mRejectThreshold  = Quantile( lWinners, aRejectQuantile );
      mCalibrated       = true;

      // 3) Calibration scaffolding is no longer needed; free it.
      mFilterStats.Clear();
      mRefInputs.Clear();
    }

    // ---- Runtime ----

    public CombinedScore Decide( IReadOnlyList<IReadOnlyList<Score>> aBranches )
    {
      CombinedScore lWinner = SelectWinner( aBranches );
      if (lWinner != null && mCalibrated)
        lWinner.Accepted = lWinner.Value >= mRejectThreshold;
      return lWinner;
    }

    public PipelineResult Decide( IReadOnlyList<PipelineResult> aBranches )
    {
      PipelineResult lWinner = SelectWinner( aBranches );
      if (lWinner != null && mCalibrated)
        lWinner.CombinedScore.Accepted = lWinner.CombinedScore.Value >= mRejectThreshold;
      return lWinner;
    }

    public CombinedScore SelectWinner( IReadOnlyList<IReadOnlyList<Score>> aBranches )
    {
      CombinedScore lBest = null;
      foreach (IReadOnlyList<Score> lBranch in aBranches)
      {
        CombinedScore lCombined = Combine( lBranch );
        if (lBest == null || lCombined.Value > lBest.Value)
          lBest = lCombined;
      }
      return lBest;
    }

    public PipelineResult SelectWinner( IReadOnlyList<PipelineResult> aBranches )
    {
      PipelineResult lBest = null;
      foreach (PipelineResult lBranch in aBranches)
      {
        lBranch.CombinedScore = Combine( lBranch.FilterScores );
        if (lBest == null || lBranch.CombinedScore.Value > lBest.CombinedScore.Value)
          lBest = lBranch;
      }
      return lBest;
    }

    public CombinedScore Combine( IReadOnlyList<Score> aBranch )
    {
      double                         lCoverage = 1.0;
      double                         lZSum     = 0.0;
      List<(string Name, double Z)>  lTerms    = new List<(string, double)>();

      foreach (Score lScore in aBranch)
      {
        if (lScore.IsCoverage)
        {
          lCoverage = Clamp01( lScore.Value );   // coverage filter emits a 0..1 fraction
          continue;
        }
        double lZ = Standardize( lScore );
        lZSum += lZ;
        lTerms.Add( (lScore.FilterName, lZ) );
      }

      return new CombinedScore( lCoverage * lZSum, lCoverage, lTerms );
    }

    // ---- Threshold helpers (operate on the shipped null distribution) ----

    // Re-derive the cutoff at a different quantile without recalibrating
    // (e.g. to apply a look-elsewhere correction for the session's search volume).
    public double ThresholdForQuantile( double aQuantile )
      => Quantile( mNullWinnerScores, aQuantile );

    // Fraction of the noise null at or below aValue: a percentile / p-value report.
    public double PercentileOf( double aValue )
    {
      if (mNullWinnerScores == null || mNullWinnerScores.Count == 0)
        return double.NaN;
      int lBelow = 0;
      foreach (double lS in mNullWinnerScores)
        if (lS <= aValue)
          lBelow += 1;
      return (double) lBelow / mNullWinnerScores.Count;
    }

    // ---- Serialization (Newtonsoft) ----

    public Calibration Export()
    {
      return new Calibration
      {
        Baselines        = mBaselines,
        NullWinnerScores = mNullWinnerScores?.ToArray(),
        RejectThreshold  = mRejectThreshold,
        Quantile         = mQuantile,
        RefInputCount    = mRefInputCount,
        CalibratedUtc    = DateTime.UtcNow.ToString( "o" ),
      };
    }

    public static ScoreModel FromCalibration( Calibration aCal, DriverApp aDriverApp  )
    {
      ScoreModel lModel = new ScoreModel(aDriverApp);
      lModel.mBaselines        = aCal.Baselines ?? new Dictionary<string, FilterScoreBaseline>();
      lModel.mNullWinnerScores = aCal.NullWinnerScores?.ToList();
      lModel.mRejectThreshold  = aCal.RejectThreshold;
      lModel.mQuantile         = aCal.Quantile;
      lModel.mRefInputCount    = aCal.RefInputCount;
      lModel.mCalibrated       = true;
      return lModel;
    }

    public void Save( string aPath )
    {
      Calibration lCal = Export();
      lCal.Save( aPath );
    }

    public static ScoreModel Load( string aPath, DriverApp aDriverApp ) => FromCalibration( Calibration.Load( aPath ), aDriverApp );

    // ---- Diagnostics ----

    public double  RejectThreshold  => mRejectThreshold;
    public bool    IsCalibrated     => mCalibrated;
    public int     RefInputCount    => mRefInputCount;

    public long SampleCount( string aFilterName )
      => mBaselines != null && mBaselines.TryGetValue( aFilterName, out FilterScoreBaseline lBase ) ? lBase.Count : 0;

    // ---- Internals ----

    double Standardize( Score aScore )
    {
      if (mBaselines == null || !mBaselines.TryGetValue( aScore.FilterName, out FilterScoreBaseline lBase ))
        return 0.0;                                  // uncalibrated filter contributes nothing
      if (lBase.Std < cEpsilon)
        return 0.0;                                  // constant-on-noise filter contributes nothing
      return (aScore.Value - lBase.Mean) / lBase.Std;
    }

    RunningStats GetStats( string aFilterName )
    {
      if (!mFilterStats.TryGetValue( aFilterName, out RunningStats lStats ))
      {
        lStats = new RunningStats();
        mFilterStats[aFilterName] = lStats;
      }
      return lStats;
    }

    static List<IReadOnlyList<Score>> ProjectScores( IReadOnlyList<PipelineResult> aBranches )
    {
      List<IReadOnlyList<Score>> lLists = new List<IReadOnlyList<Score>>( aBranches.Count );
      foreach (PipelineResult lBranch in aBranches)
        lLists.Add( lBranch.FilterScores );          // FilterScores is List<Score> == IReadOnlyList<Score>
      return lLists;
    }

    static double Quantile( IReadOnlyList<double> aSorted, double aQ )
    {
      if (aSorted == null || aSorted.Count == 0)
        return double.NegativeInfinity;
      double  lPos  = Clamp01( aQ ) * (aSorted.Count - 1);
      int     lLow  = (int) Math.Floor( lPos );
      int     lHigh = (int) Math.Ceiling( lPos );
      return aSorted[lLow] + (lPos - lLow) * (aSorted[lHigh] - aSorted[lLow]);
    }

    static double Clamp01( double aValue ) => Math.Max( 0.0, Math.Min( 1.0, aValue ) );

    const double cEpsilon = 1e-9 ;

    // calibration-time only (never serialized, cleared after Calibrate)
    Dictionary<string, RunningStats>           mFilterStats   = new Dictionary<string, RunningStats>();
    List<IReadOnlyList<IReadOnlyList<Score>>>  mRefInputs   = new List<IReadOnlyList<IReadOnlyList<Score>>>();

    // runtime essentials (the serialized state)
    Dictionary<string, FilterScoreBaseline>  mBaselines           ;
    List<double>                             mNullWinnerScores    ;
    double                                   mRejectThreshold     = double.NegativeInfinity ;
    double                                   mQuantile            = 0.99 ;  
    int                                      mRefInputCount       = 0 ;
    bool                                     mCalibrated          = false ;
    DriverApp                                mDriverApp           = null;
  }
}