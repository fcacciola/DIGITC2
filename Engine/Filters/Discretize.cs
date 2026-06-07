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
      mGate = new Gate( Params.GetFloat("GateCut"), Params.GetFloat("GateOutLevel") ) ;
    }

    public class Gate
    {
      public Gate( float aCut, float aOutLevel )
      {
        Cut      = aCut;
        OutLevel = aOutLevel;
      }

      public float Apply( float aV )
      {
        if ( aV >= Cut )
          return OutLevel; 
        return 0f; 
      }

      readonly internal float Cut ;
      readonly internal float OutLevel ;

      public override string ToString() => $"C_{(int)Cut*100}_O_{(int)OutLevel*100}";
    }


    protected override Packet Process ()
    {
      WaveSignal lSignal = WaveInput ;
      WriteLine2GUI($"Applying Discretization Gate: Cut Level={mGate.Cut} Output Level={mGate.OutLevel}");

      AddBranch("GateCut",$"{(mGate.Cut *  .8)}");
      AddBranch("GateCut",$"{(mGate.Cut * 1.2)}");

      lSignal = Apply( lSignal, mGate ) ;
      return CreateOutput( lSignal, $"Discretized.") ;
    }

    WaveSignal Apply ( WaveSignal aInput, Gate aGate )
    {
      var rDiscrete = Apply(aInput.Rep, aGate) ;
      
      var rR = aInput.CopyWith(rDiscrete);

      Save(rR, $"Discretized.wav") ;

      return rR ;
    }

    public static DiscreteSignal Apply ( DiscreteSignal aInput, Gate aGate )
    {
      float lMax = aInput.GetPeak();

      float[] lSrc = aInput.Samples ;
      int lLen = lSrc.Length ;

      float[] rOutput = new float[lLen];

      for ( int i = 0 ; i < lLen ; i++ )  
        rOutput[i] = aGate.Apply(lSrc[i]) ;  

      return new DiscreteSignal(SIG.SamplingRate, rOutput);
    }

    Gate mGate = null ;

    public override string Name => this.GetType().Name ;

  }

}
