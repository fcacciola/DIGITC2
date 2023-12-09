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
    }

    class Gate
    {
      internal Gate( params float[] aThresholds )
      {
        Thresholds = aThresholds;
      }

      internal float Apply( float aV )
      {
        for( int i = 1; i < Thresholds.Length ; ++ i )
        {
          if ( aV > Thresholds[i] )
            return Thresholds[i-1]; 
        }

        return 0f; 
      }

      readonly float[] Thresholds ;

      internal float Max ;
    }

    protected override Step Process ( WaveSignal aInput, Step aStep )
    {
      var lR0 = Apply(aInput, aStep.Label + "_A", new Gate(.98f,0.9f,0.8f,0.7f,0.6f,0.5f,0.4f,0.3f,0.2f,0.1f));

      var lR1 = Apply(lR0, aStep.Label + "_B", new Gate(.98f,0.7f,0.5f,0.3f));

      var lR2 = Apply(lR1, aStep.Label + "_C", new Gate(.98f,0.4f));

      mStep = aStep.Next( lR2, "AmplitudeGate", this) ;

      return mStep ;
    }

    WaveSignal Apply ( WaveSignal aInput, string aLabel, Gate aGate )
    {
      float lMax = aInput.ComputeMax();

      aGate.Max = lMax ;
      
      float[] lSrc = aInput.Samples ;
      int lLen = lSrc.Length ;

      float[] rOutput = new float[lLen];

      for ( int i = 0 ; i < lLen ; i++ )  
        rOutput[i] = aGate.Apply(lSrc[i]) ;  

      var rR = aInput.CopyWith(new DiscreteSignal(aInput.SamplingRate, rOutput));

      if ( Context.Session.Args.GetBool("Plot") )
        rR.SaveTo( Context.Session.LogFile( aLabel + "_AmplitudGate.wav") ) ;

      return rR ;
    }

    protected override string Name => "AmplitudeGate" ;

  }

}
