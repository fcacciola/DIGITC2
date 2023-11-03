using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Operations;
using NWaves.Signals;

namespace DIGITC2
{
  public class AmplitudeGate : WaveFilter
  {
    public AmplitudeGate() 
    { 
      mThreshold = (float)Context.Session.Args.GetDouble("AmplitudeGate_Threshold");
    }

    protected override Step Process ( WaveSignal aInput, Step aStep )
    {
      float lMax = aInput.ComputeMax();
      
      float[] lSrc = aInput.Samples ;
      int lLen = lSrc.Length ;

      float[] rOutput = new float[lLen];

      for ( int i = 0 ; i < lLen ; i++ )  
      {
        float lOut = lSrc[i] / lMax ;
        lOut = lOut > mThreshold ? 1.0f : 0 ;
        lOut = lOut * lMax ;
        rOutput[i] = lOut ;  
      }

      mStep = aStep.Next( aInput.CopyWith(new DiscreteSignal(aInput.SamplingRate, rOutput)), "AmplitudeGate", this) ;

      return mStep ;
    }

    protected override string Name => "AmplitudeGate" ;

    float mThreshold;

  }

}
