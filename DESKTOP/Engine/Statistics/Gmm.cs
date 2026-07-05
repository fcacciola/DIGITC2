using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OxyPlot;
using OxyPlot.Series;

namespace ENGINE
{
  public class CorrelationCalculator
  { 
    public CorrelationCalculator( List<double> aData ) { mData = aData ; }

    double FindX( double aX )
    {
      return mData.Find( d => d == aX );
    }

    double ComputeWeight( double aX, Func<double,double,double> aWeightFunction = null )
    {
      var lMatch = FindX( aX );
      return aWeightFunction != null ? aWeightFunction(lMatch,aX) : 1.0 ; 
    }

    double ComputeWeight ( IEnumerable<double> aXs, Func<double,double,double> aWeightFunction = null )
    {
      double lW = 0 ;
      foreach( var aX in aXs ) 
        lW += ComputeWeight( aX, aWeightFunction );
      return lW ;
    }

    public double Calculate( IList<double> aXs, Func<double,double,double> aWeightFunction = null ) 
    { 
      if ( aXs.Count == 0)
        return 0.0;

      double lW = ComputeWeight( aXs, aWeightFunction  ); 
      double rC = lW / (double)aXs.Count;
      return rC ; 
    }

    List<double> mData ;

  }

  public class GmmComponent
  {
    public readonly double Weight;    // pi

    public readonly double LogMean;   // log-space mu
    public readonly double LogVar;    // log-space sigma^2
    public readonly double LogStdDev; // log-space sigma 

    public readonly double Mean; // linear-space mu
    public readonly double Var; // linear-space sigma^2
    public readonly double StdDev; // linear-space sigma

    public GmmComponent(double aWeight, double aLogMean, double aLogVar)
    {
      Weight    = aWeight;
      LogMean   = aLogMean;
      LogVar    = aLogVar;
      LogStdDev = Math.Sqrt(LogVar);

      Mean   = Math.Exp(LogMean + 0.5 * LogVar);
      Var    = (Math.Exp(LogVar) - 1.0) * Math.Exp(2.0 * LogMean + LogVar);
      StdDev = Math.Sqrt(Var);
    }

    public override string ToString() => $"Linear Mean={Mean} Linear StdDev={StdDev} Linear Variance={Var} Log Mean={LogMean} Log StdDev={LogStdDev} Log Variance={LogVar} Weight={Weight}";

    public double N_Sigma( double aN ) => Mean + aN * StdDev;
  }

  public class Gmm
  {
    public Gmm(List<GmmComponent> aComponents)
    {
      Components = aComponents;
      Components.Sort((a, b) => a.Mean.CompareTo(b.Mean));
    }

   void ComputeBellCurvePoints(GmmComponent component, List<DataPoint> rBell, int numPoints = 500, double sigmaRange = 4.0)
    {
      double mu     = component.Mean;
      double sigma  = component.StdDev;
      double pi_w   = component.Weight;

      double xMin = mu - sigmaRange * sigma;
      double xMax = mu + sigmaRange * sigma;
      double step = (xMax - xMin) / (numPoints - 1);

      // Precompute constants for the Gaussian PDF:
      //   y = (weight / (sigma * sqrt(2π))) * exp(-0.5 * ((x - mu) / sigma)^2)
      double normFactor = pi_w / (sigma * Math.Sqrt(2.0 * Math.PI));

      for (int i = 0; i < numPoints; i++)
      {
        double x = xMin + i * step;
        double z = (x - mu) / sigma;
        double y = normFactor * Math.Exp(-0.5 * z * z);

        rBell.Add(new DataPoint(x, y));
      }
    }

    public Plotter CreatePlot( Session aSession, string aName )
    {
      Plotter rPlot = new Plotter( aSession, new Plotter.Options{Title=aName} );

      foreach( var lComponent in Components )
      {
        List<DataPoint> lBellPoints = new List<DataPoint>();

        ComputeBellCurvePoints(lComponent, lBellPoints);

        DataPointSeries lSeries = new LineSeries() as DataPointSeries;

        lSeries.Points.AddRange(lBellPoints);

        rPlot.AddSeries(lSeries);
      }

      return rPlot;
    }

    public void Plot( Session aSession, string aName )
    {
      if (Components.Count == 0)
        return;

      if ( aSession.Settings.GetBool("OutputDetails") )
      { 
        var lPlot = CreatePlot(aSession, aName);
        lPlot.SavePNG(aSession.OutputFile($"{aName}.png"));
        lPlot.SaveSVG(aSession.OutputFile($"{aName}.svg"));
      }
    }

    public void Save( Session aSession, string aName )
    {
      if (Components.Count == 0)
        return;

      if ( aSession.Settings.GetBool("OutputDetails") )
      { 
        File.WriteAllLines(aSession.OutputFile($"{aName}.txt"), Components.Select(c => c.ToString()));
      }
    }

    /// <summary>
    /// Computes the intersection point of two weighted Gaussian PDFs in log space.
    /// Solves w_A * N(x|muA,vA) = w_B * N(x|muB,vB) for x, returning the root
    /// that lies between the two means (i.e. the meaningful crossing point).
    /// </summary>
    static public double IntersectionLogSpace(GmmComponent aA, GmmComponent aB)
    {
      var vA = aA.LogVar;
      var vB = aB.LogVar;
      var muA = aA.LogMean;
      var muB = aB.LogMean;

      // Safety guards
      if (vA <= 0) vA = 1e-9;
      if (vB <= 0) vB = 1e-9;
      if (aA.Weight <= 0 || aB.Weight <= 0)
        return 0.5 * (muA + muB);

      var sA = Math.Sqrt(vA);
      var sB = Math.Sqrt(vB);

      // Derived from equality of weighted PDFs in log space:
      //   ln(wA) - ln(sA) - (x-muA)^2 / (2*vA)
      // = ln(wB) - ln(sB) - (x-muB)^2 / (2*vB)
      // Rearranged into: A*x^2 + B*x + C = 0
      var lnTerm = Math.Log((aB.Weight * sA) / (aA.Weight * sB));

      var A = 0.5 * ((1.0 / vB) - (1.0 / vA));
      var B = (muA / vA) - (muB / vB);
      var C = (muB * muB / (2.0 * vB)) - (muA * muA / (2.0 * vA)) - lnTerm;

      // If A ~ 0 the components have equal variance — equation is linear
      if (Math.Abs(A) < 1e-12)
      {
        if (Math.Abs(B) < 1e-12)
          return 0.5 * (muA + muB);   // Identical variances and means
        return -C / B;
      }

      var D = B * B - 4.0 * A * C;
      if (D < 0)
      {
        // No real intersection — numerically degenerate, fall back to midpoint
        return 0.5 * (muA + muB);
      }

      var sqrtD = Math.Sqrt(D);
      var t1 = (-B + sqrtD) / (2.0 * A);
      var t2 = (-B - sqrtD) / (2.0 * A);

      var lo = Math.Min(muA, muB);
      var hi = Math.Max(muA, muB);

      var t1In = (t1 >= lo && t1 <= hi);
      var t2In = (t2 >= lo && t2 <= hi);

      // Exactly one root between the means — the ideal case
      if (t1In && !t2In) return t1;
      if (t2In && !t1In) return t2;

      // Both roots between means: prefer the one closest to the sharper
      // (lower-variance) component's mean, since the sharper peak stays
      // dominant longer and the meaningful crossing is on its far side
      if (t1In && t2In)
      {
        var sharpMu = (vA < vB) ? muA : muB;
        return (Math.Abs(t1 - sharpMu) <= Math.Abs(t2 - sharpMu)) ? t1 : t2;
      }

      // Neither root between means — pick the one closest to midpoint
      var mid = 0.5 * (muA + muB);
      return (Math.Abs(t1 - mid) <= Math.Abs(t2 - mid)) ? t1 : t2;
    }

    /// <summary>
    /// Returns the intersection of two components in linear space (seconds),
    /// by computing the intersection in log space and exponentiating.
    /// </summary>
    static public double Intersection(GmmComponent aA, GmmComponent aB)
    {
      return Math.Exp(IntersectionLogSpace(aA, aB));
    }

    public double Intersection( int aIdxA, int aIdxB ) => Intersection( Components[aIdxA], Components[aIdxB] );

    static public double InterpolateMean( GmmComponent aA, GmmComponent aB, double aX = 0.5 ) => MathX.LERP(aA.Mean, aB.Mean, aX);

    public double InterpolateMean( int aIdxA,int aIdxB, double aX = 0.5 ) => InterpolateMean( Components[aIdxA], Components[aIdxB], aX );

    public override string ToString() => $"{Components.Count} Components";

    public List<GmmComponent> Components ;
  }

  
  public class GmmFit
  {
    public static Gmm Fit(double[] logData, int nComponents, int maxIterations = 200)
    {
      Gmm bestModel = null;
      double bestLL = double.NegativeInfinity;

      for (int restart = 0; restart < 10; restart++)
      {
        var model = FitSingle(logData, nComponents, maxIterations, 1e-6, 1e-6, restart);
        if (model == null) continue;

        double ll = ComputeLogLikelihood(logData, model);

        if (ll > bestLL)
        {
          bestLL = ll;
          bestModel = model;
        }
      }

      return bestModel;
    }

    static Gmm FitSingle(double[] x, int k, int maxIter, double tol, double varFloor, int seed)
    {
      int n = x.Length;

      var sorted = (double[])x.Clone();
      Array.Sort(sorted);

      var means = new double[k];
      var vars = new double[k];
      var weights = new double[k];

      for (int i = 0; i < k; i++)
      {
        double p = (i + 0.5 + 0.15 * (seed % 7)) / k;
        p = Math.Max(0.0, Math.Min(1.0, p));
        int idx = (int)Math.Round(p * (n - 1));

        means[i] = sorted[idx];
        weights[i] = 1.0 / k;
      }

      double globalVar = Variance(x);
      for (int i = 0; i < k; i++)
        vars[i] = Math.Max(globalVar, varFloor);

      double[,] resp = new double[n, k];
      double prevLL = double.NegativeInfinity;

      for (int iter = 0; iter < maxIter; iter++)
      {
        double ll = 0;

        for (int i = 0; i < n; i++)
        {
          double maxLog = double.NegativeInfinity;

          for (int c = 0; c < k; c++)
          {
            double lp = Math.Log(weights[c]) + LogGaussianPdf(x[i], means[c], vars[c]);
            resp[i, c] = lp;
            if (lp > maxLog) maxLog = lp;
          }

          double sum = 0;
          for (int c = 0; c < k; c++)
          {
            resp[i, c] = Math.Exp(resp[i, c] - maxLog);
            sum += resp[i, c];
          }

          ll += maxLog + Math.Log(sum);

          for (int c = 0; c < k; c++)
            resp[i, c] /= sum;
        }

        if (iter > 0 && Math.Abs(ll - prevLL) < tol * (1.0 + Math.Abs(prevLL)))
          break;

        prevLL = ll;

        for (int c = 0; c < k; c++)
        {
          double Nk = 0;
          for (int i = 0; i < n; i++) Nk += resp[i, c];
          Nk = Math.Max(Nk, 1e-12);

          weights[c] = Nk / n;

          double mean = 0;
          for (int i = 0; i < n; i++) mean += resp[i, c] * x[i];
          mean /= Nk;
          means[c] = mean;

          double var = 0;
          for (int i = 0; i < n; i++)
          {
            double d = x[i] - mean;
            var += resp[i, c] * d * d;
          }

          vars[c] = Math.Max(var / Nk, varFloor);
        }
      }

      var comps = new List<GmmComponent>();
      for (int i = 0; i < k; i++)
        comps.Add(new GmmComponent(weights[i], means[i], vars[i]));

      return new Gmm(comps);
    }

    static double LogGaussianPdf(double x, double mean, double var)
    {
      double d = x - mean;
      return -0.5 * (Math.Log(2.0 * Math.PI * var) + d * d / var);
    }

    static double Variance(double[] x)
    {
      double mean = x.Average();
      double s = 0;
      foreach (var v in x) { double d = v - mean; s += d * d; }
      return s / Math.Max(1, x.Length - 1);
    }

    static double ComputeLogLikelihood(double[] data, Gmm model)
    {
      double ll = 0;
      foreach (double x in data)
      {
        double mix = 0;
        foreach (var c in model.Components)
        {
          double sigma = c.LogStdDev;
          double z = (x - c.LogMean) / sigma;
          mix += c.Weight * Math.Exp(-0.5 * z * z) / (sigma * Math.Sqrt(2.0 * Math.PI));
        }
        ll += Math.Log(Math.Max(mix, double.Epsilon));
      }
      return ll;
    }
  }


  public static class GmmFitter
  {
    static public double[] LogPositive(IReadOnlyList<double> aData )
    {
      var r = new double[aData.Count] ;

      for( int i = 0 ; i < aData.Count ; ++i )
      {
        var g = aData[i] ;
        if( !(g > 0) ) g = 1e-12 ;
        r[i] = Math.Log(g) ;
      }

      return r ;
    }

    // -------------------------------------------------------------------------
    // Public entry point
    // -------------------------------------------------------------------------

    /// <summary>
    /// Fits a Log-space GMM to a list of time durations (in seconds),
    /// automatically selecting the best number of components via BIC.
    /// </summary>
    /// <param name="aData">Values to fit (must be > 0).</param>
    /// <param name="maxComponents">Maximum K to try.</param>
    /// <param name="maxIterations">EM iteration cap per fit.</param>
    /// <param name="tolerance">EM convergence threshold on log-likelihood.</param>
    /// <returns>The best-scoring GMM as a list of components.</returns>
    public static Gmm Fit(IReadOnlyList<double> aData,
                          int maxComponents = 10,
                          int maxIterations = 200,
                          double tolerance = 1e-6)
    {
      // Work in log space throughout
      double[] logData = LogPositive(aData);
      int n = logData.Length;

      Gmm bestModel = null;
      double bestBic = double.MaxValue;

      for (int k = 1; k <= maxComponents; k++)
      {
        var model = GmmFit.Fit(logData, k, maxIterations);
        if ( model.Components.Count == 0 )
          continue; // Skip degenerate fits that lost all components

        double bic = ComputeBic(logData, model, k);

        if (bic < bestBic)
        {
          bestBic = bic;
          bestModel = model;
        }
        else
        {
          // BIC got worse — stop early, adding more components won't help
          break;
        }
      }

      return bestModel;
    }

    public static Gmm FilterComponents(this Gmm model, Func<GmmComponent, bool> predicate)
    {
      var filtered = model.Components.Where(predicate).ToList();
      return filtered.Count > 0 ? new Gmm(filtered) : null;
    }

    static bool IsMeaningless(GmmComponent                aComponent,
                              IReadOnlyList<GmmComponent> aAll,
                              double                      aLogSigmaCeiling = 0.67,
                              double                      aWidthRatioFloor = 5.0)
    {
      // 1. Absolute width: spans more than ~×2 around its median -> background, not a mode.
      if (aComponent.LogStdDev > aLogSigmaCeiling)
        return true;

      // 2. Engulfment: much wider than a sibling AND its ±1σ interval swallows that sibling's mean.
      foreach (GmmComponent lOther in aAll)
      {
        if (ReferenceEquals(lOther, aComponent))
          continue;

        bool lContains  = Math.Abs(lOther.LogMean - aComponent.LogMean) < aComponent.LogStdDev;
        bool lMuchWider = aComponent.LogStdDev > aWidthRatioFloor * lOther.LogStdDev;

        if (lContains && lMuchWider)
          return true;
      }

      return false;
    }


    public static Gmm DiscardMeaningless(this Gmm model) 
    {
      return model.FilterComponents(c => ! IsMeaningless(c, model.Components));
    }

    public static Gmm ChooseBest(this Gmm model, int maxComponents)
    {
      var lByWeight = model.Components.OrderByDescending(c => c.Weight).ToList();
      var lPick = lByWeight.Take(maxComponents).ToList();
      var lByMean = lPick.OrderBy(c => c.Mean).ToList();
      return new Gmm(lByMean);
    }

    // -------------------------------------------------------------------------
    // BIC = -2 * logL  +  numParameters * ln(n)
    //
    // For a 1D GMM each component has 3 parameters (weight, mean, variance)
    // but weights are constrained to sum to 1, so free params = 3K - 1
    // -------------------------------------------------------------------------

    private static double ComputeBic(double[] data,
                                      Gmm model,
                                      int k)
    {
      int n = data.Length;
      int numParameters = 3 * k - 1;
      double logLik = ComputeLogLikelihood(data, model);

      return -2.0 * logLik + numParameters * Math.Log(n);
    }

    // -------------------------------------------------------------------------
    // Log-likelihood of the whole dataset under the model
    // -------------------------------------------------------------------------

    private static double ComputeLogLikelihood(double[] data, Gmm model)
    {
      double logLik = 0;
      foreach (double x in data)
      {
        double mix = model.Components.Sum(c => c.Weight * GaussianPdf(x, c));
        // Guard against log(0) from numerical underflow
        logLik += Math.Log(Math.Max(mix, double.Epsilon));
      }
      return logLik;
    }

    // -------------------------------------------------------------------------
    // Standard 1D Gaussian PDF  (data is already in log space)
    // -------------------------------------------------------------------------

    private static double GaussianPdf(double x, GmmComponent c)
    {
      double sigma = c.LogStdDev;
      double z = (x - c.LogMean) / sigma;
      return Math.Exp(-0.5 * z * z) / (sigma * Math.Sqrt(2.0 * Math.PI));
    }
  }


  //static public class Gmm1D_ChatGPT
  //{
  //  static public Gmm Fit
  //  (
  //    double[] aX,
  //    int aK,
  //    int aMaxIter = 200,
  //    double aTol = 1e-6,
  //    double aVarFloor = 1e-6
  //  )
  //  {
  //    if( aK < 1 ) return  null ;
  //    if( aX.Length < aK * 5 ) return null ;

  //    var n = aX.Length ;

  //    // Init via quantiles (deterministic, decent for 1D).
  //    var lX = (double[])aX.Clone() ;
  //    Array.Sort(lX) ;

  //    var lMeans = new double[aK] ;
  //    for( int k = 0 ; k < aK ; ++k )
  //    {
  //      var p = (k + 0.5) / aK ;
  //      var idx = (int)Math.Round(p * (n - 1)) ;
  //      idx = Math.Max(0, Math.Min(n-1, idx)) ;
  //      lMeans[k] = lX[idx] ;
  //    }

  //    var lWeights = new double[aK] ;
  //    for( int k = 0 ; k < aK ; ++k )
  //      lWeights[k] = 1.0 / aK ;

  //    // Initial variance from global variance.
  //    var lGlobalVar = Variance(aX) ;
  //    if( lGlobalVar < aVarFloor ) lGlobalVar = aVarFloor ;

  //    var lVars = new double[aK] ;
  //    for( int k = 0 ; k < aK ; ++k )
  //      lVars[k] = lGlobalVar ;

  //    var lNk = new double[aK] ;
  //    var lNewMeans = new double[aK] ;
  //    var lNewVars  = new double[aK] ;
  //    var lNewWeights = new double[aK] ;

  //    double lPrevLL = double.NegativeInfinity ;

  //    for( int iter = 0 ; iter < aMaxIter ; ++iter )
  //    {
  //      Array.Clear(lNk, 0, aK) ;
  //      Array.Clear(lNewMeans, 0, aK) ;
  //      Array.Clear(lNewVars, 0, aK) ;

  //      double lLL = 0 ;

  //      // E-step: accumulate Nk and sum(r*x)
  //      for( int i = 0 ; i < n ; ++i )
  //      {
  //        var x = aX[i] ;

  //        // log probabilities
  //        var lLogP = new double[aK] ;
  //        double lMax = double.NegativeInfinity ;

  //        for( int k = 0 ; k < aK ; ++k )
  //        {
  //          var lp = Math.Log(lWeights[k]) + LogGaussianPdf(x, lMeans[k], lVars[k]) ;
  //          lLogP[k] = lp ;
  //          if( lp > lMax ) lMax = lp ;
  //        }

  //        // log-sum-exp
  //        double lSum = 0 ;
  //        for( int k = 0 ; k < aK ; ++k )
  //          lSum += Math.Exp(lLogP[k] - lMax) ;

  //        var lLogSum = lMax + Math.Log(lSum) ;
  //        lLL += lLogSum ;

  //        // responsibilities and partial stats
  //        for( int k = 0 ; k < aK ; ++k )
  //        {
  //          var r = Math.Exp(lLogP[k] - lLogSum) ;
  //          lNk[k]       += r ;
  //          lNewMeans[k] += r * x ;
  //        }
  //      }

  //      // Update means + weights
  //      for( int k = 0 ; k < aK ; ++k )
  //      {
  //        var nk = lNk[k] ;
  //        if( nk < 1e-12 ) nk = 1e-12 ;

  //        lNewMeans[k]  /= nk ;
  //        lNewWeights[k] = nk / n ;
  //      }

  //      // Second pass: compute variances with new means (recompute responsibilities; simple & safe)
  //      for( int i = 0 ; i < n ; ++i )
  //      {
  //        var x = aX[i] ;

  //        var lLogP = new double[aK] ;
  //        double lMax = double.NegativeInfinity ;

  //        for( int k = 0 ; k < aK ; ++k )
  //        {
  //          var lp = Math.Log(lWeights[k]) + LogGaussianPdf(x, lMeans[k], lVars[k]) ;
  //          lLogP[k] = lp ;
  //          if( lp > lMax ) lMax = lp ;
  //        }

  //        double lSum = 0 ;
  //        for( int k = 0 ; k < aK ; ++k )
  //          lSum += Math.Exp(lLogP[k] - lMax) ;

  //        var lLogSum = lMax + Math.Log(lSum) ;

  //        for( int k = 0 ; k < aK ; ++k )
  //        {
  //          var r = Math.Exp(lLogP[k] - lLogSum) ;
  //          var d = x - lNewMeans[k] ;
  //          lNewVars[k] += r * d * d ;
  //        }
  //      }

  //      for( int k = 0 ; k < aK ; ++k )
  //      {
  //        var nk = lNk[k] ;
  //        if( nk < 1e-12 ) nk = 1e-12 ;

  //        lNewVars[k] /= nk ;
  //        if( lNewVars[k] < aVarFloor ) lNewVars[k] = aVarFloor ;
  //      }

  //      // Convergence check on log-likelihood
  //      if( iter > 0 )
  //      {
  //        var lDiff = Math.Abs(lLL - lPrevLL) ;
  //        if( lDiff < aTol * (1.0 + Math.Abs(lPrevLL)) )
  //        {
  //          lWeights = (double[])lNewWeights.Clone() ;
  //          lMeans   = (double[])lNewMeans.Clone() ;
  //          lVars    = (double[])lNewVars.Clone() ;
  //          break ;
  //        }
  //      }

  //      lPrevLL = lLL ;

  //      lWeights = (double[])lNewWeights.Clone() ;
  //      lMeans   = (double[])lNewMeans.Clone() ;
  //      lVars    = (double[])lNewVars.Clone() ;
  //    }

  //    var lComps = new List<GmmComponent>() ;
  //    for( int k = 0 ; k < aK ; ++k )
  //      lComps.Add( new GmmComponent(lWeights[k], lMeans[k], lVars[k]) ) ;

  //    return new Gmm(lComps) ;
  //  }

  //  static double LogGaussianPdf( double x, double mu, double var )
  //  {
  //    var d = x - mu ;
  //    return -0.5 * (Math.Log(2.0 * Math.PI * var) + (d*d)/var) ;
  //  }

  //  static double Variance( double[] aX )
  //  {
  //    var n = aX.Length ;
  //    if( n <= 1 ) return 0 ;

  //    double mean = 0 ;
  //    for( int i = 0 ; i < n ; ++i )
  //      mean += aX[i] ;
  //    mean /= n ;

  //    double s2 = 0 ;
  //    for( int i = 0 ; i < n ; ++i )
  //    {
  //      var d = aX[i] - mean ;
  //      s2 += d*d ;
  //    }

  //    return s2 / (n - 1) ;
  //  }
  //}

}
