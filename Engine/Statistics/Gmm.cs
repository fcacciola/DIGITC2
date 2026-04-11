using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OxyPlot;
using OxyPlot.Series;

namespace DIGITC2_ENGINE
{
  public class GmmComponent
  {
    public readonly double Weight;    // pi

    public readonly double LogMean;   // log-space mu
    public readonly double LogVar;    // log-space sigma^2
    public readonly double LogStdDev; // log-space sigma 

    public readonly double Mean; // linear-space mu
    public readonly double Var; // linear-space sigma^2
    public readonly double StdDev; // linear-space sigma

    public GmmComponent(double aWeight, double aMean, double aVar)
    {
      Weight    = aWeight;
      LogMean   = aMean;
      LogVar    = aVar;
      LogStdDev = Math.Sqrt(LogVar);

      Mean   = Math.Exp(LogMean + 0.5 * LogVar);
      Var    = (Math.Exp(LogVar) - 1.0) * Math.Exp(2.0 * LogMean + LogVar);
      StdDev = Math.Sqrt(Var);
    }
  }

  public class Gmm
  {
    public Gmm(List<GmmComponent> aComponents)
    {
      mComponents = aComponents;
      mComponents.Sort((a, b) => a.LogMean.CompareTo(b.LogMean));
    }

    void ComputeBellCurvePoints(GmmComponent component, List<DataPoint> rBell, int numPoints = 500, double sigmaRange = 4.0)
    {
      double mu = component.Mean;    // mean  in log space
      double sigma = component.StdDev;  // stdev in log space
      double w = component.Weight;

      // Work in log space to define the range, then exponentiate
      double logXMin = mu - sigmaRange * sigma;
      double logXMax = mu + sigmaRange * sigma;
      double logStep = (logXMax - logXMin) / (numPoints - 1);

      double normFactor = w / (sigma * Math.Sqrt(2.0 * Math.PI));

      for (int i = 0; i < numPoints; i++)
      {
        double logX = logXMin + i * logStep;
        double x = Math.Exp(logX);               // back to linear space
        double z = (logX - mu) / sigma;
        double y = normFactor * Math.Exp(-0.5 * z * z) / x;  // note the /x factor
        rBell.Add(new DataPoint(x, y));
      }
    }


    public Plotter CreatePlot()
    {
      Plotter rPlot = new Plotter();

      List<DataPoint> lBellPoints = new List<DataPoint>();

      mComponents.ForEach(c => ComputeBellCurvePoints(c, lBellPoints));

      DataPointSeries lSeries = new LineSeries() as DataPointSeries;

      lSeries.Points.AddRange(lBellPoints);

      rPlot.AddSeries(lSeries);

      return rPlot;
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

    public IReadOnlyList<GmmComponent> Components => mComponents;

    List<GmmComponent> mComponents;
  }


  public static class GmmFitter
  {
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
      double[] logData = aData.Select(d => Math.Log(d)).ToArray();
      int n = logData.Length;

      List<GmmComponent> bestModel = null;
      double bestBic = double.MaxValue;

      for (int k = 1; k <= maxComponents; k++)
      {
        List<GmmComponent> model = FitEm(logData, k, maxIterations, tolerance);
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

      return new Gmm(bestModel);
    }

    // -------------------------------------------------------------------------
    // EM algorithm
    // -------------------------------------------------------------------------

    private static List<GmmComponent> FitEm(double[] logData,
                                               int k,
                                               int maxIter,
                                               double tol)
    {
      int n = logData.Length;

      // --- Initialise components via K-means++ seeding ---
      List<GmmComponent> components = Initialise(logData, k);

      double prevLogLik = double.NegativeInfinity;

      for (int iter = 0; iter < maxIter; iter++)
      {
        // --- E-step: compute responsibilities ---
        double[,] r = EStep(logData, components);

        // --- M-step: update parameters ---
        components = MStep(logData, r, k);

        // --- Check convergence ---
        double logLik = ComputeLogLikelihood(logData, components);
        if (Math.Abs(logLik - prevLogLik) < tol) break;
        prevLogLik = logLik;
      }

      return components;
    }

    // -------------------------------------------------------------------------
    // Initialisation — K-means++ seeding for stable starts
    // -------------------------------------------------------------------------

    private static List<GmmComponent> Initialise(double[] aLogData, int k)
    {
      var rng = new Random(42);
      int n = aLogData.Length;
      double total = aLogData.Sum();
      double mean = total / n;
      double var = aLogData.Sum(x => (x - mean) * (x - mean)) / n;

      // Pick first centre at random, then spread subsequent centres
      // proportional to squared distance from nearest existing centre
      var centres = new List<double>();
      centres.Add(aLogData[rng.Next(n)]);

      for (int c = 1; c < k; c++)
      {
        double[] dists = aLogData.Select(x =>
        {
          double minD = centres.Min(ctr => (x - ctr) * (x - ctr));
          return minD;
        }).ToArray();

        double sum = dists.Sum();
        double r = rng.NextDouble() * sum;
        double acc = 0;
        int idx = 0;
        for (; idx < n - 1; idx++)
        {
          acc += dists[idx];
          if (acc >= r) break;
        }
        centres.Add(aLogData[idx]);
      }

      // Build initial components from centres
      return centres.Select(c => new GmmComponent
      (
        1.0 / k,
        c,
        var / k      // Start narrow; EM will widen as needed
      )).ToList();
    }

    // -------------------------------------------------------------------------
    // E-step: responsibility matrix  r[i, j] = P(component j | data[i])
    // -------------------------------------------------------------------------

    private static double[,] EStep(double[] data, List<GmmComponent> components)
    {
      int n = data.Length;
      int k = components.Count;
      double[,] r = new double[n, k];

      for (int i = 0; i < n; i++)
      {
        double sum = 0;
        for (int j = 0; j < k; j++)
        {
          double val = components[j].Weight * GaussianPdf(data[i], components[j]);
          r[i, j] = val;
          sum += val;
        }
        // Normalise so each row sums to 1
        if (sum > 0)
          for (int j = 0; j < k; j++)
            r[i, j] /= sum;
      }

      return r;
    }

    // -------------------------------------------------------------------------
    // M-step: recompute weight / mean / variance from responsibilities
    // -------------------------------------------------------------------------

    private static List<GmmComponent> MStep(double[] data, double[,] r, int k)
    {
      int n = data.Length;
      double minVariance = 1e-6;   // Guard against degenerate components
      var components = new List<GmmComponent>(k);

      for (int j = 0; j < k; j++)
      {
        double Nj = 0;
        for (int i = 0; i < n; i++) Nj += r[i, j];

        double weight = Nj / n;

        double mean = 0;
        for (int i = 0; i < n; i++) mean += r[i, j] * data[i];
        mean /= Nj;

        double variance = 0;
        for (int i = 0; i < n; i++)
        {
          double diff = data[i] - mean;
          variance += r[i, j] * diff * diff;
        }
        variance = Math.Max(variance / Nj, minVariance);

        components.Add(new GmmComponent
        (
          weight,
          mean,
          variance
        ));
      }

      return components;
    }

    // -------------------------------------------------------------------------
    // BIC = -2 * logL  +  numParameters * ln(n)
    //
    // For a 1D GMM each component has 3 parameters (weight, mean, variance)
    // but weights are constrained to sum to 1, so free params = 3K - 1
    // -------------------------------------------------------------------------

    private static double ComputeBic(double[] data,
                                      List<GmmComponent> model,
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

    private static double ComputeLogLikelihood(double[] data, List<GmmComponent> components)
    {
      double logLik = 0;
      foreach (double x in data)
      {
        double mix = components.Sum(c => c.Weight * GaussianPdf(x, c));
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
}
