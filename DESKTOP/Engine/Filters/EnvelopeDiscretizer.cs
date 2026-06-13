using System;
using System.Collections.Generic;

namespace ENGINE
{
  /// A maximal envelope region together with cached summary statistics.
  public struct PulseRun
  {
    public int   Start;   // inclusive sample index
    public int   End;     // exclusive sample index
    public float Peak;    // max envelope value within [Start, End)
    public float Area;    // integral of max(0, env - lo) over [Start, End)

    public PulseRun(
      int   aStart,
      int   aEnd,
      float aPeak,
      float aArea)
    {
      Start = aStart;
      End   = aEnd;
      Peak  = aPeak;
      Area  = aArea;
    }

    public int Width => End - Start;
  }

  /// Converts an upward-compressed envelope into a clean 0/1 square wave.
  ///
  /// Strategy (see notes per step):
  ///   1. Find maximal regions above the LOW line       -> accurate pulse edges.
  ///   2. Merge runs whose separating valley is shallow  -> heals "cracks", duration-agnostic.
  ///      (topographic prominence; no time / min-gap term at all)
  ///   3. Keep a run only if its peak reaches HIGH        -> rejects chatter / moderate bumps.
  ///   4. Keep a run only if it is wide enough AND has enough area
  ///      (area = "not too high nor too sustained", with height/duration trading off).
  public sealed class EnvelopeDiscretizer
  {
    private readonly float mHiThreshold;       // a run must peak above this to count as a pulse
    private readonly float mLoThreshold;       // run extents are defined by crossings of this line
    private readonly int   mMinWidth;          // minimum kept run length, in samples
    private readonly float mMinArea;           // minimum integrated prominence above lo
    private readonly float mMergeProminence;   // in (0,1): merge a saddle whose relative depth is below this
    private readonly float mBaseline;          // background floor used to normalise valley depth

    public EnvelopeDiscretizer(
      float aHiThreshold,
      float aLoThreshold,
      int   aMinWidth,
      float aMinArea,
      float aMergeProminence,
      float aBaseline)
    {
      mHiThreshold     = aHiThreshold;
      mLoThreshold     = aLoThreshold;
      mMinWidth        = aMinWidth;
      mMinArea         = aMinArea;
      mMergeProminence = aMergeProminence;
      mBaseline        = aBaseline;
    }

    /// Convenience factory: estimate hi/lo/background automatically (Otsu) and
    /// derive the area floor from minWidth and the hysteresis band. The only
    /// tuning knob left is aMergeProminence:
    ///   - near 0.0 -> only near-flat valleys merge (splits aggressively)
    ///   - near 1.0 -> almost everything merges (a valley must reach background to split)
    ///   - 0.5      -> a valley must descend past halfway (peak -> background) to count as a gap
    public static EnvelopeDiscretizer CreateAuto(
      float[] aEnvelope,
      int     aMinWidth,
      float   aMergeProminence = 0.5f)
    {
      float lLo;
      float lHi;
      float lBackground;
      EstimateHysteresisBand(aEnvelope, out lLo, out lHi, out lBackground);

      float lMinArea = aMinWidth * (lHi - lLo);   // area of a minimal "just reaches hi" pulse
      return new EnvelopeDiscretizer(lHi, lLo, aMinWidth, lMinArea, aMergeProminence, lBackground);
    }

    /// Full pipeline. Returns a 0/1 square wave the same length as the input.
    public float[] Discretize(float[] aEnvelope)
    {
      List<PulseRun> lRuns = FindPulses(aEnvelope);
      return Render(lRuns, aEnvelope.Length);
    }

    /// Same pipeline, but returns the intervals (for downstream pulse-symbol work).
    public List<PulseRun> FindPulses(float[] aEnvelope)
    {
      List<PulseRun> lRuns = FindRunsAboveLo(aEnvelope);
      lRuns                = BridgeByProminence(lRuns, aEnvelope);
      lRuns                = FilterWeakRuns(lRuns);
      return lRuns;
    }

    // -- Step 1 -------------------------------------------------------------
    // Maximal regions where env >= lo. Boundaries sit on lo-crossings, which is
    // where a human would visually place the block edges. Peak and area cached.
    private List<PulseRun> FindRunsAboveLo(float[] aEnvelope)
    {
      List<PulseRun> lRuns   = new List<PulseRun>();
      bool           lInside = false;
      int            lStart  = 0;
      float          lPeak   = 0.0f;
      float          lArea   = 0.0f;

      for (int lI = 0; lI < aEnvelope.Length; lI++)
      {
        float lV = aEnvelope[lI];
        if (lV >= mLoThreshold)
        {
          if (!lInside)
          {
            lInside = true;
            lStart  = lI;
            lPeak   = lV;
            lArea   = 0.0f;
          }
          if (lV > lPeak)
          {
            lPeak = lV;
          }
          lArea += lV - mLoThreshold;
        }
        else
        {
          if (lInside)
          {
            lInside = false;
            lRuns.Add(new PulseRun(lStart, lI, lPeak, lArea));
          }
        }
      }

      if (lInside)
      {
        lRuns.Add(new PulseRun(lStart, aEnvelope.Length, lPeak, lArea));
      }
      return lRuns;
    }

    // -- Step 2 -------------------------------------------------------------
    // Merge adjacent runs separated by a SHALLOW valley, by topographic
    // prominence and nothing else (no gap-duration term). For two runs with
    // peaks P_a, P_b and a separating valley floor V, the relative prominence
    // of the smaller peak through that saddle is:
    //
    //     prom = (min(P_a, P_b) - V) / (min(P_a, P_b) - baseline)
    //
    // i.e. the fraction of the lower peak's swing (above background) that the
    // valley descends. prom ~ 0 => barely a dip => merge; prom ~ 1 => valley
    // reaches background => a real gap => keep split. This is invariant to both
    // amplitude and gap duration.
    //
    // Merging is done shallowest-saddle-first and iterated: each merge raises
    // the survivor's peak (max of the two), which changes its other neighbours'
    // relative prominence, so a single left-to-right pass would be order
    // dependent. We always collapse the globally shallowest saddle that is
    // still below threshold, then recompute.
    private List<PulseRun> BridgeByProminence(
      List<PulseRun> aRuns,
      float[]        aEnvelope)
    {
      List<PulseRun> lRuns    = new List<PulseRun>(aRuns);
      List<float>    lSaddles = ComputeSaddles(lRuns, aEnvelope);

      while (lRuns.Count > 1)
      {
        int   lBest     = -1;
        float lBestProm = float.MaxValue;
        for (int lI = 0; lI < lSaddles.Count; lI++)
        {
          float lLowerPeak = Math.Min(lRuns[lI].Peak, lRuns[lI + 1].Peak);
          float lDenom     = lLowerPeak - mBaseline;
          float lProm      = lDenom > 0.0f ? (lLowerPeak - lSaddles[lI]) / lDenom : 1.0f;
          if (lProm < lBestProm)
          {
            lBestProm = lProm;
            lBest     = lI;
          }
        }

        if (lBest < 0 || lBestProm >= mMergeProminence)
        {
          break;
        }

        PulseRun lA = lRuns[lBest];
        PulseRun lB = lRuns[lBest + 1];
        lRuns[lBest] = new PulseRun(
          lA.Start,
          lB.End,
          Math.Max(lA.Peak, lB.Peak),
          lA.Area + lB.Area);     // the bridged gap is below lo, so it adds no area
        lRuns.RemoveAt(lBest + 1);
        lSaddles.RemoveAt(lBest);   // outer saddles still reference unchanged outer boundaries
      }

      return lRuns;
    }

    // Valley floor (minimum envelope value) in the sub-lo gap between each pair
    // of consecutive runs. lSaddles[k] separates lRuns[k] and lRuns[k + 1].
    private List<float> ComputeSaddles(
      List<PulseRun> aRuns,
      float[]        aEnvelope)
    {
      List<float> lSaddles = new List<float>();
      for (int lI = 0; lI < aRuns.Count - 1; lI++)
      {
        int   lFrom = aRuns[lI].End;          // first sub-lo sample of the gap
        int   lTo   = aRuns[lI + 1].Start;    // exclusive end of the gap
        float lMinV = float.MaxValue;
        for (int lJ = lFrom; lJ < lTo; lJ++)
        {
          if (aEnvelope[lJ] < lMinV)
          {
            lMinV = aEnvelope[lJ];
          }
        }
        if (lFrom >= lTo)   // guard: touching runs with no gap samples
        {
          lMinV = mLoThreshold;
        }
        lSaddles.Add(lMinV);
      }
      return lSaddles;
    }

    // -- Steps 3 & 4 --------------------------------------------------------
    // Arm test (peak >= hi) kills chatter; width + area encode "high enough OR
    // sustained enough". Area is the criterion that lets the two trade off.
    private List<PulseRun> FilterWeakRuns(List<PulseRun> aRuns)
    {
      List<PulseRun> lKept = new List<PulseRun>();
      foreach (PulseRun lRun in aRuns)
      {
        bool lTallEnough   = lRun.Peak  >= mHiThreshold;
        bool lWideEnough   = lRun.Width >= mMinWidth;
        bool lStrongEnough = lRun.Area  >= mMinArea;
        if (lTallEnough && lWideEnough && lStrongEnough)
        {
          lKept.Add(lRun);
        }
      }
      return lKept;
    }

    // -- Step 5 -------------------------------------------------------------
    private float[] Render(
      List<PulseRun> aRuns,
      int            aLength)
    {
      float[] lOut = new float[aLength];
      foreach (PulseRun lRun in aRuns)
      {
        for (int lI = lRun.Start; lI < lRun.End; lI++)
        {
          lOut[lI] = 0.8f;
        }
      }
      return lOut;
    }

    // -- Auto thresholds ----------------------------------------------------
    // Otsu split of the envelope histogram, then place the hysteresis band in
    // the valley between the two class means: lo at ~1/3, hi at ~2/3 of the way
    // from background mean to pulse mean. Also returns the background-class mean
    // for use as the prominence normalisation floor. Works well on a bimodal
    // compressed envelope; swap in your own estimator if you have a better prior.
    private static void EstimateHysteresisBand(
      float[]   aSignal,
      out float aLo,
      out float aHi,
      out float aBackground)
    {
      int   lBinCount = 256;
      float lMin      = float.MaxValue;
      float lMax      = float.MinValue;

      for (int lI = 0; lI < aSignal.Length; lI++)
      {
        float lV = aSignal[lI];
        if (lV < lMin)
        {
          lMin = lV;
        }
        if (lV > lMax)
        {
          lMax = lV;
        }
      }

      if (lMax <= lMin)
      {
        aLo         = lMin;
        aHi         = lMin;
        aBackground = lMin;
        return;
      }

      float lScale = (lBinCount - 1) / (lMax - lMin);
      int[] lHist  = new int[lBinCount];
      for (int lI = 0; lI < aSignal.Length; lI++)
      {
        int lBin = (int)((aSignal[lI] - lMin) * lScale);
        lHist[lBin]++;
      }

      long   lTotal = aSignal.Length;
      double lSum   = 0.0;
      for (int lB = 0; lB < lBinCount; lB++)
      {
        lSum += (double)lB * lHist[lB];
      }

      double lSumB      = 0.0;
      long   lWeightB   = 0;
      double lMaxVar    = -1.0;
      int    lThreshBin = 0;
      for (int lB = 0; lB < lBinCount; lB++)
      {
        lWeightB += lHist[lB];
        if (lWeightB == 0)
        {
          continue;
        }
        long lWeightF = lTotal - lWeightB;
        if (lWeightF == 0)
        {
          break;
        }

        lSumB += (double)lB * lHist[lB];
        double lMeanB = lSumB / lWeightB;
        double lMeanF = (lSum - lSumB) / lWeightF;
        double lVar   = (double)lWeightB * lWeightF * (lMeanB - lMeanF) * (lMeanB - lMeanF);
        if (lVar > lMaxVar)
        {
          lMaxVar    = lVar;
          lThreshBin = lB;
        }
      }

      double lSum0   = 0.0;
      long   lCount0 = 0;
      double lSum1   = 0.0;
      long   lCount1 = 0;
      for (int lB = 0; lB < lBinCount; lB++)
      {
        float lCenter = lMin + (lB + 0.5f) / lScale;
        if (lB <= lThreshBin)
        {
          lSum0   += (double)lCenter * lHist[lB];
          lCount0 += lHist[lB];
        }
        else
        {
          lSum1   += (double)lCenter * lHist[lB];
          lCount1 += lHist[lB];
        }
      }

      float lMean0 = lCount0 > 0 ? (float)(lSum0 / lCount0) : lMin;
      float lMean1 = lCount1 > 0 ? (float)(lSum1 / lCount1) : lMax;
      aBackground  = lMean0;
      aLo          = lMean0 + 0.33f * (lMean1 - lMean0);
      aHi          = lMean0 + 0.66f * (lMean1 - lMean0);
    }
  }
}
