using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Operations;
using NWaves.Signals;

namespace ENGINE
{
   
  public class Discretize : WaveFilter
  {
    public Discretize( ) 
    { 
    }

    protected override void OnSetup()
    {
      mOptions = new Options
      {
        MinWidth        = Params.GetDouble("MinWidth"),
        MergeProminence = Params.GetFloat("MergeProminence")
      };
    }


    protected override Packet Process ()
    {
      WaveSignal lSignal = WaveInput ;

      WriteLine2GUI($"Applying Discretization...");

      var lDiscretizer =  EnvelopeDiscretizer.CreateAuto(lSignal.Rep.Samples, SIG.SamplesForTime(mOptions.MinWidth),  mOptions.MergeProminence);
      var lNewSamples = lDiscretizer.Discretize(lSignal.Rep.Samples);

      var rR = lSignal.CopyWith( new DiscreteSignal( SIG.SamplingRate, lNewSamples) );

      Save(rR, $"Discretized.wav") ;

      return CreateOutput( rR, $"Discretized.") ;
    }

    class Options
    {
      internal double MinWidth = 0.001 ;
      internal float  MergeProminence = 0.5f ;
    }

    Options mOptions ;

    public override string Name => this.GetType().Name ;

  }

}
