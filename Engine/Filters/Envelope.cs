using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using MathNet.Numerics.Statistics;

using NWaves.Filters;
using NWaves.Operations;
using NWaves.Signals;

using OxyPlot.Annotations;

using LowPassFilter = NWaves.Filters.Elliptic.LowPassFilter ;


namespace DIGITC2_ENGINE
{
  public class Envelope : WaveFilter
  {
    public class Args 
    {
      public float AttackTime  = 0.005f;
      public float ReleaseTime = 0.01f;

      public override string ToString() => $"A_{(int)(AttackTime*100000)}_R_{(int)(ReleaseTime*100000)}";
    }

    public Envelope() 
    { 
    }

    protected override Packet Process ()
    {
      var lArgs = new Args{AttackTime=Params.GetFloat("Attack"), ReleaseTime= Params.GetFloat("Release") };

      DContext.WriteLine($"Following Envelope. AttackTime: {lArgs.AttackTime} ReleaseTime:{lArgs.ReleaseTime}");

      var lNewRep = Apply(WaveInput.Rep, lArgs); 

      Save(lNewRep, $"Envelope.wav") ;

      var rR = WaveInput.CopyWith(lNewRep);

      return CreateOutput(rR,"Envelope");
    }

    public static DiscreteSignal Apply( DiscreteSignal aSignal, Args aArgs )
    {
      EnvelopeFollower lEnvelopeFollower = new EnvelopeFollower(SIG.SamplingRate, aArgs.AttackTime, aArgs.ReleaseTime);

      var rNewRep = lEnvelopeFollower.ApplyTo( aSignal );

      rNewRep.Sanitize(); 

      return rNewRep ;
    }

    public override string Name => this.GetType().Name ;

  }

}
