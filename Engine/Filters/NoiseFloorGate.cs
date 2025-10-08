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
      public float TrimRatio  = 0.05f ;
      public int   Percentile = 10;
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
      aInput.Sanitize();

      var lEnvelope = Envelope.Apply(aInput, new Envelope.Params{FollowerAttackTime=0.0005f, FollowerReleaseTime=0.001f});
    
      var lBaseLine = EstimateBaseline(lEnvelope.Samples, aParams.TrimRatio, aParams.Percentile);

      DContext.WriteLine($"Noise Floor: {lBaseLine}");

      var lNewSamples = ApplyGate(aInput.Samples, lBaseLine);

      var rR = new DiscreteSignal(SIG.SamplingRate, lNewSamples);

      rR.Sanitize();

      return rR ;
    }

    static float[] Trim( float[] aSamples, float aTrimRatio )
    {
      int lMargin = (int)(aSamples.Length * aTrimRatio);

      int lNewLen = aSamples.Length - lMargin - lMargin ;

      float[] rR = new float[lNewLen];

      Array.ConstrainedCopy(aSamples, lMargin, rR, 0, lNewLen);

      return rR ; 
    }


    static float EstimateBaseline(float[] aSamples, float aTrimRatio = 0.05f, int aPercentile = 10)
    {
      Array.Sort(aSamples); 

      var lTrimmed = Trim(aSamples, aTrimRatio);

      float rR = lTrimmed.Percentile(aPercentile);

      return rR ;
    }

    static float[] ApplyGate(float[] envelope, float aBaseline)
    {
      float[] filtered = new float[envelope.Length];

      for (int i = 0; i < envelope.Length; i++)
      {
        float lN = envelope[i] - aBaseline ;

        filtered[i] = lN < 0 ? 0 : lN ;
      }

      return filtered;
    }

    Params mParams = new Params();

    public override string Name => this.GetType().Name ;

  }



}

