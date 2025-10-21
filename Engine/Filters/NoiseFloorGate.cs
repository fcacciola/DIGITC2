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
    public class NoiseFloorEstimationParams 
    {
      public float TrimRatio  = 0.05f ;
      public int   Percentile = 10;
    }

    public NoiseFloorGate() 
    { 
    }

    protected override Packet Process ( WaveSignal aInput, Config aConfig, Packet aInputPacket, List<Config> rBranches )
    {
      DContext.WriteLine("Gating Above Noise Floor");
      DContext.Indent();

      aInput.Rep.Sanitize() ;

      ApplyEnvelopeThenGate(aInput, aInputPacket, new Envelope.Params{FollowerAttackTime=0.0005f, FollowerReleaseTime=0.001f}, rOutput );
      ApplyEnvelopeThenGate(aInput, aInputPacket, new Envelope.Params{FollowerAttackTime=0.0010f, FollowerReleaseTime=0.002f}, rOutput );

      DContext.Unindent();
    }

    void ApplyEnvelopeThenGate( WaveSignal aInput, Packet aInputPacket, Envelope.Params aEnvelopeParams, List<Packet> rOutput ) 
    {
      var lEnvelope = Envelope.Apply(aInput.Rep, aEnvelopeParams);

      string lEnvelopeLabel = $"Envelope_{aEnvelopeParams}";
      
      if ( DContext.Session.Settings.GetBool("Plot") )
        lEnvelope.SaveTo( DContext.Session.OutputFile( $"{lEnvelopeLabel}.wav") ) ;

      var lBaseLine = EstimateBaseline(lEnvelope.Samples, new NoiseFloorEstimationParams() );

      ApplyGate(aInput, aInputPacket, lEnvelope, lBaseLine       , lEnvelopeLabel, rOutput );
      ApplyGate(aInput, aInputPacket, lEnvelope, lBaseLine * 0.5f, lEnvelopeLabel, rOutput );
      ApplyGate(aInput, aInputPacket, lEnvelope, lBaseLine * 2.0f, lEnvelopeLabel, rOutput );
    }

    void ApplyGate( WaveSignal aInput, Packet aInputPacket, DiscreteSignal aSmoothed, float aFloor, string aEnvelopeLabel, List<Packet> rOutput ) 
    {
      DContext.WriteLine($"Applying Noise Gate at: {aFloor}");

      var lNewSamples = RawApplyGate(aSmoothed.Samples, aFloor);

      var lGated = new DiscreteSignal(SIG.SamplingRate, lNewSamples);

      lGated.Sanitize();

      string lLabel = $"NoiseGate_{aEnvelopeLabel}_{(int)(aFloor*100)}]";

      if ( DContext.Session.Settings.GetBool("Plot") )
        lGated.SaveTo( DContext.Session.OutputFile( $"{lLabel}.wav") ) ;

      var lES = aInput.CopyWith(lGated);
      lES.Name = lLabel;

      rOutput.Add( new Packet(Name, aInputPacket, lES, lLabel) );
    }

    static float[] Trim( float[] aSamples, float aTrimRatio )
    {
      int lMargin = (int)(aSamples.Length * aTrimRatio);

      int lNewLen = aSamples.Length - lMargin - lMargin ;

      float[] rR = new float[lNewLen];

      Array.ConstrainedCopy(aSamples, lMargin, rR, 0, lNewLen);

      return rR ; 
    }


    static float EstimateBaseline(float[] aSamples, NoiseFloorEstimationParams aParams )
    {
      Array.Sort(aSamples); 

      var lTrimmed = Trim(aSamples, aParams.TrimRatio);

      float rR = lTrimmed.Percentile(aParams.Percentile);

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

    NoiseFloorEstimationParams mParams = new NoiseFloorEstimationParams();

    public override string Name => this.GetType().Name ;

  }



}

