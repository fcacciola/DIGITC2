using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using MathNet.Numerics.Statistics;

using NWaves.Operations;
using NWaves.Signals;

using OxyPlot.Annotations;

using LowPassFilter = NWaves.Filters.Elliptic.LowPassFilter ;


namespace DIGITC2_ENGINE
{
  public class NoiseFloorGate : WaveFilter
  {
    public class Params 
    {
      public int   WindowSize = 30 ;
      public float Percentile = 0.10f;
      public float DynamicGateAlpha      = 0.5f ;
    }

    public NoiseFloorGate() 
    { 
    }

    protected override void Process ( WaveSignal aInput, Packet aInputPacket, List<Packet> rOutput )
    {
      DContext.WriteLine("Gating Above Noise Floor");
      DContext.Indent();

      var lNewRep = Apply(aInput.Rep, mParams) ; 

      var rR = aInput.CopyWith(lNewRep);

      string lLabel = "NoiseFloorGate";

      if ( DContext.Session.Args.GetBool("Plot") )
        rR.SaveTo( DContext.Session.OutputFile( $"{lLabel}.wav") ) ;

      rOutput.Add( new Packet(Name, aInputPacket, rR, lLabel));
      DContext.Unindent();
    }

    public static DiscreteSignal Apply ( DiscreteSignal aInput, Params aParams)
    {
      var lLocalBaseline  = ComputeLocalBaseline(aInput.Samples, aParams.WindowSize, aParams.Percentile);
      var lGlobalBaseline = EstimateKDEBaseline(aInput.Samples);

      var lNewSamples = ApplyDynamicGate(aInput.Samples, lLocalBaseline, lGlobalBaseline, aParams.DynamicGateAlpha);

      var rR = new DiscreteSignal(SIG.SamplingRate, lNewSamples);

      rR.Sanitize();

      return rR ;
    }


    // 1. Windowed Local Percentile
    public static float[] ComputeLocalBaseline(float[] envelope, int windowSize, float percentile)
    {
      int half = windowSize / 2;
      float[] baseline = new float[envelope.Length];
      float[] window = new float[windowSize];

      int lEdge = SIG.SamplesForTime( 5000 ); // 5 seconds edge.

      int i = 0;  

      for ( i = 0 ; i < lEdge ; ++ i )
        baseline[i] = 0.99f;

      for (; i < envelope.Length - lEdge ; i++)
      {
        int start = Math.Max(0, i - half);
        int end = Math.Min(envelope.Length, i + half);

        for ( int j = start, k = 0  ; j < end ; ++ j, ++ k  )
          window[k] = envelope[j];
        float[] sorted = window.OrderBy(x => x).ToArray();
        int index = (int)(percentile * sorted.Length);
        baseline[i] = sorted[index];
      }

      for ( ; i < envelope.Length ; ++ i )
        baseline[i] = 0.99f;

      return baseline;
    }

    // 2. KDE to Estimate Global Baseline
    public static float EstimateKDEBaseline(float[] envelope, int numBins = 100, float bandwidth = 0.01f)
    {
      float min = envelope.Min();
      float max = envelope.Max();
      float step = (max - min) / numBins;

      float[] binCenters = new float[numBins];
      float[] density = new float[numBins];

      for (int i = 0; i < numBins; i++)
      {
        float x = min + i * step;
        binCenters[i] = x;

        foreach (var s in envelope)
        {
            float u = (x - s) / bandwidth;
            density[i] += (float)Math.Exp(-0.5f * u * u);
        }

        density[i] /= (envelope.Length * bandwidth * (float)Math.Sqrt(2 * Math.PI));
      }

      int peakIndex = Array.IndexOf(density, density.Max());
      return binCenters[peakIndex];
    }

    // 3. Dynamic Gating Operation
    public static float[] ApplyDynamicGate(float[] envelope, float[] localBaseline, float globalBaseline, float alpha)
    {
      float[] threshold = localBaseline.Select(b => b + alpha * globalBaseline).ToArray();
      float[] filtered = new float[envelope.Length];

      for (int i = 0; i < envelope.Length; i++)
      {
        filtered[i] = envelope[i] > threshold[i] ? envelope[i] : 0;
      }

      return filtered;
    }

    Params mParams = new Params();

    public override string Name => this.GetType().Name ;

  }



}
