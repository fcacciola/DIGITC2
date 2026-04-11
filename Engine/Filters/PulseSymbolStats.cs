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
  public class PulseSymbolStats
  {
    static public double[] LogPositive( double[] aGaps )
    {
      var r = new double[aGaps.Length] ;

      for( int i = 0 ; i < aGaps.Length ; ++i )
      {
        var g = aGaps[i] ;
        if( !(g > 0) ) g = 1e-12 ;
        r[i] = Math.Log(g) ;
      }

      return r ;
    }

    // Optional utility if you want exp( quantile ) for reporting (not used for merge threshold now).
    static public double GaussianQuantile( double aMean, double aStdDev, double aP )
      => aMean + aStdDev * NormalQuantile(aP) ;

   // Normal quantile approximation (Acklam-ish).
    static public double NormalQuantile( double p )
    {
      if( p <= 0 || p >= 1 ) throw new ArgumentOutOfRangeException(nameof(p)) ;

      double[] a = { -3.969683028665376e+01,  2.209460984245205e+02,
                     -2.759285104469687e+02,  1.383577518672690e+02,
                     -3.066479806614716e+01,  2.506628277459239e+00 } ;

      double[] b = { -5.447609879822406e+01,  1.615858368580409e+02,
                     -1.556989798598866e+02,  6.680131188771972e+01,
                     -1.328068155288572e+01 } ;

      double[] c = { -7.784894002430293e-03, -3.223964580411365e-01,
                     -2.400758277161838e+00, -2.549732539343734e+00,
                      4.374664141464968e+00,  2.938163982698783e+00 } ;

      double[] d = {  7.784695709041462e-03,  3.224671290700398e-01,
                      2.445134137142996e+00,  3.754408661907416e+00 } ;

      const double plow = 0.02425 ;
      const double phigh = 1 - plow ;

      double q, r ;

      if( p < plow )
      {
        q = Math.Sqrt(-2 * Math.Log(p)) ;
        return (((((c[0]*q + c[1])*q + c[2])*q + c[3])*q + c[4])*q + c[5]) /
               ((((d[0]*q + d[1])*q + d[2])*q + d[3])*q + 1) ;
      }

      if( p > phigh )
      {
        q = Math.Sqrt(-2 * Math.Log(1 - p)) ;
        return -(((((c[0]*q + c[1])*q + c[2])*q + c[3])*q + c[4])*q + c[5]) /
                 ((((d[0]*q + d[1])*q + d[2])*q + d[3])*q + 1) ;
      }

      q = p - 0.5 ;
      r = q*q ;
      return (((((a[0]*r + a[1])*r + a[2])*r + a[3])*r + a[4])*r + a[5]) * q /
             (((((b[0]*r + b[1])*r + b[2])*r + b[3])*r + b[4])*r + 1) ;
    }


    string            mName ;
    List<PulseSymbol> mPulses ;
    double[]          mGapDurations ;

    Stats mStats ;
   
  }

  public class PulseSymbolStats_MergeTheshold : PulseSymbolStats
  {
    public static double Calculate( List<PulseSymbol> aPulses )
    {
      return AnalyzeGapDurations( aPulses.CalculateGapDurations() );
    }

    static double AnalyzeGapDurations_LOG( double[] aGapDurations,  int aMaxEmIters = 200, double aEmTol = 1e-6, double aVarFloor = 1e-6 )
    {
      FilterHelper.DumpValues("GapDurations", aGapDurations); 

      var lLogGaps = LogPositive(aGapDurations) ;

      var lMergeGmm = Gmm1D.Fit(lLogGaps, aK:4, aMaxIter:aMaxEmIters, aTol:aEmTol, aVarFloor:aVarFloor).SortedByMean() ;

      if ( lMergeGmm == null )
        return 0;

      var lIntraTapThresholdGaussian_Log = lMergeGmm.Components[0];

      double lMean_LogSpace = lIntraTapThresholdGaussian_Log.Mean;
      double lStdDev_LogSpace = lIntraTapThresholdGaussian_Log.StdDev;

      // Work in log space
      double rRawMergeThreshold1_Log = Math.Log(0.2) + lMean_LogSpace;
      double rRawMergeThreshold2_Log = lMean_LogSpace - 2.0 * lStdDev_LogSpace;

      double rMergeThreshold_Log = Math.Max(rRawMergeThreshold1_Log, rRawMergeThreshold2_Log);
      double rMergeThreshold = Math.Exp(rMergeThreshold_Log);  // Convert back only at the end

      return rMergeThreshold ;
    }

    static double AnalyzeGapDurations( double[] aGapDurations,  int aMaxEmIters = 200, double aEmTol = 1e-6, double aVarFloor = 1e-6 )
    {

      var lLogGaps = LogPositive(aGapDurations) ;

      var lMergeGmm = Gmm1D.Fit(lLogGaps, aK:4, aMaxIter:aMaxEmIters, aTol:aEmTol, aVarFloor:aVarFloor).SortedByMean() ;

      if ( lMergeGmm == null )
        return 0;

      var lIntraTapThresholdGaussian_Log = lMergeGmm.Components[0];

      double lMean = Math.Exp(lIntraTapThresholdGaussian_Log.Mean);
      double lStdDev_LogSpace = lIntraTapThresholdGaussian_Log.StdDev;

      // For lognormal, the standard deviation in original space is:
      double lStdDev = Math.Sqrt((Math.Exp(lStdDev_LogSpace * lStdDev_LogSpace) - 1) * lMean * lMean);

      // Or simpler: use the lognormal formula
      // σ_original = sqrt(exp(2μ + σ²) * (exp(σ²) - 1))
      double lStdDev_Alt = Math.Sqrt(Math.Exp(2 * lIntraTapThresholdGaussian_Log.Mean + lStdDev_LogSpace * lStdDev_LogSpace) 
                                     * (Math.Exp(lStdDev_LogSpace * lStdDev_LogSpace) - 1));
                               
                               
      double rRawMergeThreshold1 = 0.2 * lMean;
      double rRawMergeThreshold2 = lMean - 2.0 * lStdDev;

      double rMergeThreshold = Math.Max(rRawMergeThreshold1, rRawMergeThreshold2);

      return rMergeThreshold ;
    }
  }

  public class PulseSymbolStats_IntraTapCodeGap : PulseSymbolStats
  {
    public static double Calculate( double[] aGapDurarions )
    {
      return AnalyzeGapDurations( aGapDurarions );
    }

    static public double AnalyzeGapDurations( double[] aGapDurations,
                                              int      aMaxEmIters = 200,
                                              double   aEmTol = 1e-6,
                                              double   aVarFloor = 1e-6
                                            )
    {
      var lLogGaps = LogPositive(aGapDurations) ;

      var lTimingGmm = Gmm1D.Fit(lLogGaps, aK:4, aMaxIter:aMaxEmIters, aTol:aEmTol, aVarFloor:aVarFloor).SortedByMean() ;

      if (lTimingGmm == null || lTimingGmm.K < 2)
        return 0;

      // Work in log-space
      double lIntra_Inter_Intersection_Log = Gmm1DModel.IntersectionLogSpace(lTimingGmm.Components[0], lTimingGmm.Components[1]);
      double lIntra_Inter_Intersection = Math.Exp(lIntra_Inter_Intersection_Log);

      double lMean_LogSpace_0 = lTimingGmm.Components[0].Mean;
      double lStdDev_LogSpace_0 = lTimingGmm.Components[0].StdDev;

      double lRawIntraTapGap0 = Math.Exp(lMean_LogSpace_0);

      double lRawIntraTapGap1 = lIntra_Inter_Intersection * 0.8;

      // Work in log-space: mean + 2*stdev in log-space
      double lRawIntraTapGap2_Log = lMean_LogSpace_0 + 2.0 * lStdDev_LogSpace_0;
      double lRawIntraTapGap2 = Math.Exp(lRawIntraTapGap2_Log);

      double rRawIntraTapGap = Math.Min(lRawIntraTapGap1, lRawIntraTapGap2);

      return rRawIntraTapGap;
    }
  }

  // --------------------------------------------------------------------------
  // 1D Gaussian Mixture Model (log-space)
  // --------------------------------------------------------------------------

  public readonly struct Gmm1DComponent
  {
    public readonly double Weight ;   // pi
    public readonly double Mean ;     // mu
    public readonly double Var ;      // sigma^2

    public Gmm1DComponent( double aWeight, double aMean, double aVar )
    {
      Weight = aWeight ;
      Mean   = aMean ;
      Var    = aVar ;
    }

    public double StdDev => Math.Sqrt(Var) ;
  }

  public sealed class Gmm1DModel
  {
    public Gmm1DComponent[] Components { get ; init ; } = Array.Empty<Gmm1DComponent>() ;

    public int K => Components.Length ;

    public Gmm1DModel SortedByMean()
    {
      var r = new Gmm1DComponent[Components.Length] ;
      Array.Copy(Components, r, Components.Length) ;
      Array.Sort(r, (a,b) => a.Mean.CompareTo(b.Mean)) ;
      return new Gmm1DModel { Components = r } ;
    }

    // Intersection point in log-space between two weighted 1D Gaussians:
    // solve piA*N(t|muA,varA) = piB*N(t|muB,varB).
    // Returns a t; if two real roots exist, returns the one between means when possible.
    static public double IntersectionLogSpace( Gmm1DComponent aA, Gmm1DComponent aB )
    {
      var vA  = aA.Var ;
      var vB  = aB.Var ;
      var muA = aA.Mean ;
      var muB = aB.Mean ;

      // Safety: avoid zero variance
      if( vA <= 0 ) vA = 1e-9 ;
      if( vB <= 0 ) vB = 1e-9 ;

      var sA = Math.Sqrt(vA) ;
      var sB = Math.Sqrt(vB) ;

      // Derived from equality of weighted PDFs.
      var lnTerm = Math.Log( (aB.Weight * sA) / (aA.Weight * sB) ) ;

      var A = (1.0/vB) - (1.0/vA) ;
      var B = (-2.0*muB/vB) + (2.0*muA/vA) ;
      var C = (muB*muB/vB) - (muA*muA/vA) - 2.0*lnTerm ;

      // If A ~ 0, it's linear.
      if( Math.Abs(A) < 1e-12 )
      {
        if( Math.Abs(B) < 1e-12 )
          return 0.5*(muA + muB) ;

        return -C / B ;
      }

      var D = B*B - 4*A*C ;
      if( D < 0 )
      {
        // No real intersection due to numerical issues/overlap; fall back to midpoint.
        return 0.5*(muA + muB) ;
      }

      var sqrtD = Math.Sqrt(D) ;
      var t1 = (-B + sqrtD) / (2*A) ;
      var t2 = (-B - sqrtD) / (2*A) ;

      var lo = Math.Min(muA, muB) ;
      var hi = Math.Max(muA, muB) ;

      var t1In = (t1 >= lo && t1 <= hi) ;
      var t2In = (t2 >= lo && t2 <= hi) ;

      if( t1In && !t2In ) return t1 ;
      if( t2In && !t1In ) return t2 ;
      if( t1In && t2In )
      {
        var mid = 0.5*(muA + muB) ;
        return (Math.Abs(t1-mid) <= Math.Abs(t2-mid)) ? t1 : t2 ;
      }

      // Neither root between means: pick the one closer to midpoint between means.
      var mid2 = 0.5*(muA + muB) ;
      return (Math.Abs(t1-mid2) <= Math.Abs(t2-mid2)) ? t1 : t2 ;
    }
  }

  static public class Gmm1D
  {
    static public Gmm1DModel Fit
    (
      double[] aX,
      int aK,
      int aMaxIter = 200,
      double aTol = 1e-6,
      double aVarFloor = 1e-6
    )
    {
      if( aK < 1 ) return  null ;
      if( aX.Length < aK * 5 ) return null ;

      var n = aX.Length ;

      // Init via quantiles (deterministic, decent for 1D).
      var lX = (double[])aX.Clone() ;
      Array.Sort(lX) ;

      var lMeans = new double[aK] ;
      for( int k = 0 ; k < aK ; ++k )
      {
        var p = (k + 0.5) / aK ;
        var idx = (int)Math.Round(p * (n - 1)) ;
        idx = Math.Max(0, Math.Min(n-1, idx)) ;
        lMeans[k] = lX[idx] ;
      }

      var lWeights = new double[aK] ;
      for( int k = 0 ; k < aK ; ++k )
        lWeights[k] = 1.0 / aK ;

      // Initial variance from global variance.
      var lGlobalVar = Variance(aX) ;
      if( lGlobalVar < aVarFloor ) lGlobalVar = aVarFloor ;

      var lVars = new double[aK] ;
      for( int k = 0 ; k < aK ; ++k )
        lVars[k] = lGlobalVar ;

      var lNk = new double[aK] ;
      var lNewMeans = new double[aK] ;
      var lNewVars  = new double[aK] ;
      var lNewWeights = new double[aK] ;

      double lPrevLL = double.NegativeInfinity ;

      for( int iter = 0 ; iter < aMaxIter ; ++iter )
      {
        Array.Clear(lNk, 0, aK) ;
        Array.Clear(lNewMeans, 0, aK) ;
        Array.Clear(lNewVars, 0, aK) ;

        double lLL = 0 ;

        // E-step: accumulate Nk and sum(r*x)
        for( int i = 0 ; i < n ; ++i )
        {
          var x = aX[i] ;

          // log probabilities
          var lLogP = new double[aK] ;
          double lMax = double.NegativeInfinity ;

          for( int k = 0 ; k < aK ; ++k )
          {
            var lp = Math.Log(lWeights[k]) + LogGaussianPdf(x, lMeans[k], lVars[k]) ;
            lLogP[k] = lp ;
            if( lp > lMax ) lMax = lp ;
          }

          // log-sum-exp
          double lSum = 0 ;
          for( int k = 0 ; k < aK ; ++k )
            lSum += Math.Exp(lLogP[k] - lMax) ;

          var lLogSum = lMax + Math.Log(lSum) ;
          lLL += lLogSum ;

          // responsibilities and partial stats
          for( int k = 0 ; k < aK ; ++k )
          {
            var r = Math.Exp(lLogP[k] - lLogSum) ;
            lNk[k]       += r ;
            lNewMeans[k] += r * x ;
          }
        }

        // Update means + weights
        for( int k = 0 ; k < aK ; ++k )
        {
          var nk = lNk[k] ;
          if( nk < 1e-12 ) nk = 1e-12 ;

          lNewMeans[k]  /= nk ;
          lNewWeights[k] = nk / n ;
        }

        // Second pass: compute variances with new means (recompute responsibilities; simple & safe)
        for( int i = 0 ; i < n ; ++i )
        {
          var x = aX[i] ;

          var lLogP = new double[aK] ;
          double lMax = double.NegativeInfinity ;

          for( int k = 0 ; k < aK ; ++k )
          {
            var lp = Math.Log(lWeights[k]) + LogGaussianPdf(x, lMeans[k], lVars[k]) ;
            lLogP[k] = lp ;
            if( lp > lMax ) lMax = lp ;
          }

          double lSum = 0 ;
          for( int k = 0 ; k < aK ; ++k )
            lSum += Math.Exp(lLogP[k] - lMax) ;

          var lLogSum = lMax + Math.Log(lSum) ;

          for( int k = 0 ; k < aK ; ++k )
          {
            var r = Math.Exp(lLogP[k] - lLogSum) ;
            var d = x - lNewMeans[k] ;
            lNewVars[k] += r * d * d ;
          }
        }

        for( int k = 0 ; k < aK ; ++k )
        {
          var nk = lNk[k] ;
          if( nk < 1e-12 ) nk = 1e-12 ;

          lNewVars[k] /= nk ;
          if( lNewVars[k] < aVarFloor ) lNewVars[k] = aVarFloor ;
        }

        // Convergence check on log-likelihood
        if( iter > 0 )
        {
          var lDiff = Math.Abs(lLL - lPrevLL) ;
          if( lDiff < aTol * (1.0 + Math.Abs(lPrevLL)) )
          {
            lWeights = (double[])lNewWeights.Clone() ;
            lMeans   = (double[])lNewMeans.Clone() ;
            lVars    = (double[])lNewVars.Clone() ;
            break ;
          }
        }

        lPrevLL = lLL ;

        lWeights = (double[])lNewWeights.Clone() ;
        lMeans   = (double[])lNewMeans.Clone() ;
        lVars    = (double[])lNewVars.Clone() ;
      }

      var lComps = new Gmm1DComponent[aK] ;
      for( int k = 0 ; k < aK ; ++k )
        lComps[k] = new Gmm1DComponent(lWeights[k], lMeans[k], lVars[k]) ;

      return new Gmm1DModel { Components = lComps } ;
    }

    static double LogGaussianPdf( double x, double mu, double var )
    {
      var d = x - mu ;
      return -0.5 * (Math.Log(2.0 * Math.PI * var) + (d*d)/var) ;
    }

    static double Variance( double[] aX )
    {
      var n = aX.Length ;
      if( n <= 1 ) return 0 ;

      double mean = 0 ;
      for( int i = 0 ; i < n ; ++i )
        mean += aX[i] ;
      mean /= n ;

      double s2 = 0 ;
      for( int i = 0 ; i < n ; ++i )
      {
        var d = aX[i] - mean ;
        s2 += d*d ;
      }

      return s2 / (n - 1) ;
    }
  }

}
