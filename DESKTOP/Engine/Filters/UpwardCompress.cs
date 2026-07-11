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


namespace ENGINE
{

  class UpwardCompressStretcher
  {
    private readonly float _gamma;

    public UpwardCompressStretcher(float aBaseFloor, float targetFloor )
    {
      // Solve: F^gamma = targetFloor
      _gamma = MathF.Log(targetFloor) / MathF.Log(aBaseFloor);
    }

    public float Process(float x)
    {
      return MathF.Pow(x, _gamma);
    }

    public void ApplyTo(DiscreteSignal signal )
    {
      for (int i = 0; i < signal.Length; i++)
      {
        signal[i] = Process(signal[i]);
      }
    }
  }

  public class UpwardCompress : WaveFilter
  {
    public class Args 
    {
      public float BaseFloor   ;
      public float TargetFloor ;

      public override string ToString() => $"B_{(int)(BaseFloor*10)}_T_{(int)(TargetFloor*10)}";
    }

    public UpwardCompress() 
    { 
    }

    protected override Packet Process ()
    {
      var lArgs = new Args{BaseFloor=Params.GetFloat("BaseFloor"), TargetFloor= Params.GetFloat("TargetFloor") };

      //AddBranch("BaseFloor",$"{(lArgs.BaseFloor * .8)}");
      //AddBranch("BaseFloor",$"{(lArgs.BaseFloor * 1.2)}");

      WriteLine2GUI($"Applying Upward Compression. BaseFloor: {lArgs.BaseFloor} TargetFloor:{lArgs.TargetFloor}");

      float lGamma = MathF.Log(lArgs.TargetFloor) / MathF.Log(lArgs.BaseFloor);

      DiscreteSignal lNewRep = new DiscreteSignal(SIG.SamplingRate,WaveInput.Rep.Length);

      for (int i = 0; i < WaveInput.Rep.Length; i++)
      {
        lNewRep[i] = MathF.Pow(WaveInput.Rep[i], lGamma);;
      }

      Save(lNewRep, $"UpwardCompress.wav") ;

      var rR = WaveInput.CopyWith(lNewRep);

      return CreateOutput(rR,"UpwardCompress");
    }

    public override string Name => this.GetType().Name ;

  }

}
