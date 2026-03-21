using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Runtime.CompilerServices;

using MathNet.Numerics.Statistics;

using NWaves.Operations;
using NWaves.Signals;

using OxyPlot.Annotations;

using LowPassFilter = NWaves.Filters.Elliptic.LowPassFilter ;


namespace DIGITC2_ENGINE
{


public static class ExactPercentile
{
  /// <summary>
  /// Exact percentile within a symmetrically trimmed middle segment.
  /// - trim in [0,1): fraction to cut from BOTH head and tail, after sorting (conceptually).
  /// - p in [0,1]: percentile within the remaining middle segment (type-7 interpolation).
  /// Preserves the original array (operates on a clone).
  /// </summary>
  public static float TrimmedPercentile(float[] src, double trim, double p)
  {
    if (src == null || src.Length == 0) throw new ArgumentException("empty array", nameof(src));
    if (trim < 0 || trim >= 1) throw new ArgumentOutOfRangeException(nameof(trim), "trim must be in [0,1).");
    if (p <= 0) p = 0; else if (p >= 1) p = 1;

    int n = src.Length;
    int t = (int)Math.Floor(trim * n);      // trim this many from head AND tail
    int start = t;
    int end = n - t;                        // exclusive
    int m = end - start;                    // length of trimmed window

    if (m <= 0) throw new ArgumentException("Trimmed window is empty; reduce trim.");

    // Fast exits
    if (m == 1) return SelectKOnClone(src, start, 0); // the sole element
    if (p == 0)  return SelectKOnClone(src, start, 0);
    if (p == 1)  return SelectKOnClone(src, start, m - 1);

    // Type-7 interpolation within the [start, end) window
    double h = (m - 1) * p + 1.0;          // in (1, m) for 0<p<1
    int j = (int)Math.Floor(h) - 1;        // 0-based lower index inside the window
    double gamma = h - Math.Floor(h);      // fractional part

    // Clone once; perform both selections in-place on the same clone
    var a = (float[])src.Clone();

    int k1 = start + j;
    float xj = SelectInPlace(a, start, end - 1, k1);
    if (gamma == 0) return xj;

    int k2 = k1 + 1;
    float xjp1 = SelectInPlace(a, start, end - 1, k2);
    return (float)((1.0 - gamma) * xj + gamma * xjp1);
  }

  /// <summary>
  /// Convenience: untrimmed exact percentile (type-7) without sorting the whole array.
  /// </summary>
  public static float Percentile(float[] src, double p)
  {
    if (src == null || src.Length == 0) throw new ArgumentException("empty array", nameof(src));
    if (p <= 0) return SelectKOnClone(src, 0, 0);
    if (p >= 1) return SelectKOnClone(src, 0, src.Length - 1);

    int n = src.Length;
    double h = (n - 1) * p + 1.0;
    int j = (int)Math.Floor(h) - 1;
    double g = h - Math.Floor(h);

    var a = (float[])src.Clone();
    float xj = SelectInPlace(a, 0, n - 1, j);
    if (g == 0) return xj;
    float xjp1 = SelectInPlace(a, 0, n - 1, j + 1);
    return (float)((1.0 - g) * xj + g * xjp1);
  }

  // --- internals ---

  // Select k-th (absolute index) within [left..right] inclusive, in-place on 'a'.
  static float SelectInPlace(float[] a, int left, int right, int k)
  {
    while (true)
    {
      if (right - left <= 32)
      {
        InsertionSort(a, left, right);
        return a[k];
      }

      int mid = (left + right) >> 1;
      MedianOf3(a, left, mid, right);
      float pivot = a[mid];

      int i = left - 1;
      int j = right + 1;
      while (true)
      {
        do { i++; } while (a[i] < pivot);
        do { j--; } while (a[j] > pivot);
        if (i >= j) break;
        Swap(a, i, j);
      }

      if (k <= j) right = j;
      else left = j + 1;
    }
  }

  // Helper: select kInside (0-based) inside a trimmed window starting at 'start'
  static float SelectKOnClone(float[] src, int start, int kInside)
  {
    var a = (float[])src.Clone();
    return SelectInPlace(a, start, src.Length - 1 - start, start + kInside);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  static void Swap(float[] a, int i, int j) { float t = a[i]; a[i] = a[j]; a[j] = t; }

  static void InsertionSort(float[] a, int lo, int hi)
  {
    for (int i = lo + 1; i <= hi; i++)
    {
      float x = a[i]; int j = i - 1;
      while (j >= lo && a[j] > x) { a[j + 1] = a[j]; j--; }
      a[j + 1] = x;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  static void MedianOf3(float[] a, int i, int j, int k)
  {
    if (a[j] < a[i]) Swap(a, i, j);
    if (a[k] < a[j]) Swap(a, j, k);
    if (a[j] < a[i]) Swap(a, i, j);
  }
}

  public class NoiseFloorGate : WaveFilter
  {
    public class Options 
    {
      public float  EnvelopeAttackTime;
      public float  EnvelopeReleaseTime;
      public float  FloorEstimationTrimRatio ;
      public int    FloorEstimationPercentile;
      public float  Floor;
    }

    public NoiseFloorGate() 
    { 
    }

    Options CreateOptions()
    {
      Options rOptions = new ();

      rOptions.EnvelopeAttackTime        = Params.GetFloat("EnvelopeAttack");
      rOptions.EnvelopeReleaseTime       = Params.GetFloat("EnvelopeRelease") ;
      rOptions.FloorEstimationTrimRatio  = Params.GetFloat("FloorEstimationTrimRatio");
      rOptions.FloorEstimationPercentile = Params.GetInt  ("FloorEstimationPercentile");
      rOptions.Floor                     = Params.GetFloat("Floor");

      return rOptions;

    }

    protected override Packet Process ()
    {
      mOptions = CreateOptions();

      WaveInput.Rep.Sanitize() ;

      var lEnvelopeParams = new Envelope.Args{ AttackTime = mOptions.EnvelopeAttackTime, ReleaseTime = mOptions.EnvelopeReleaseTime};
      var lEnvelope = Envelope.Apply(WaveInput.Rep, lEnvelopeParams);

      string lEnvelopeLabel = $"Envelope_{lEnvelopeParams}";
      
      Save( lEnvelope, $"{lEnvelopeLabel}.wav" ) ;

      var (lFloor,lNewSamples) = EstimateFloor(lEnvelope.Samples);

      if ( lNewSamples == null )
        lNewSamples = RawApplyGate(lEnvelope.Samples, lFloor);

      var lGated = new DiscreteSignal(SIG.SamplingRate, lNewSamples);

      lGated.Sanitize();

      string lLabel = $"NoiseGate_{(int)(lFloor*100000)}";

      Save(lGated, $"{lLabel}.wav") ;

      var lES = WaveInput.CopyWith(lGated);
      lES.Name = lLabel;

      return CreateOutput(lES, lLabel);
    }

    (float,float[]) EstimateFloor( float[] aEnvelope )
    {
      float rFloor = mOptions.Floor ;

      float[] rNewSamples = null ;

      if ( rFloor == -1 )
      {
        rFloor = EstimateBaseline(aEnvelope,mOptions);

        int lGates = 0 ;

        const int cTries = 5; 
        const int cMinGates = ( ( 2 * 8 ) + 5 ) * 5 ; // At least 5 letters

//        rFloor *= 1f / 1.05f ; // To counter the very first scale up in the loop

        for ( int i = 0 ; i < cTries && lGates < cMinGates ; ++ i )
        {
          rFloor *= 1.05f ;
          WriteLine2GUI($"Applying Noise Gate at: {rFloor}");
          rNewSamples = RawApplyGate(aEnvelope, rFloor);
          lGates = CountGates(rNewSamples);
        }

        if ( rFloor > 0 && lGates < cMinGates )
        {
          AddBranch("Floor",$"{(0.2)}");
          AddBranch("Floor",$"{(0.4)}");
          AddBranch("Floor",$"{(0.6)}");
        }
      }

      return (rFloor,rNewSamples) ;
    }

    static float[] Trim( float[] aSamples, float aTrimRatio )
    {
      int lMargin = (int)(aSamples.Length * aTrimRatio);

      int lNewLen = aSamples.Length - lMargin - lMargin ;

      float[] rR = new float[lNewLen];

      Array.ConstrainedCopy(aSamples, lMargin, rR, 0, lNewLen);

      return rR ; 
    }


    public static float EstimateBaseline(float[] aSamples, Options aParams )
    {
      float[] lCopy = new float[aSamples.Length];
      Array.ConstrainedCopy(aSamples,0, lCopy, 0, lCopy.Length);

      //Array.Sort(lSorted);

      var lTrimmed = Trim(lCopy, aParams.FloorEstimationTrimRatio);

      //float rR = ArrayStatistics.PercentileInplace   lTrimmed.Percentile(aParams.FloorEstimationPercentile);

      float rR = ArrayStatistics.PercentileInplace(lTrimmed,aParams.FloorEstimationPercentile);

      return rR ;
    }

    static float[] RawApplyGate(float[] envelope, float aBaseline)
    {
      float[] filtered = new float[envelope.Length];

      for (int i = 0; i < envelope.Length; i++)
      {
        float lN = envelope[i] - aBaseline ;

        filtered[i] = lN < 0 ? 0 : lN ;
      }

      return filtered;
    }

    int CountGates(float[] aSamples)
    {
      int rR = 0 ;
      bool lInGate = false ;
      for( int i = 0 ; i < aSamples.Length ; ++ i )
      {
        if ( aSamples[i] > 0 )
        {
          if ( ! lInGate )
          {
            lInGate = true ;
            ++ rR ;
          }
        }
        else
        {
          lInGate = false ;
        }
      }

      WriteLine($"Detected {rR} gates.") ;

      return rR ;
    }

    Options mOptions = new Options();

    public override string Name => this.GetType().Name ;

  }



}

