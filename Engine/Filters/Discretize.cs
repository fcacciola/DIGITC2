using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Operations;
using NWaves.Signals;

namespace DIGITC2_ENGINE
{
  public class GateThresholds
  {
    public GateThresholds( List<int> aThresholds ) 
    { 
      foreach( int lThreshold in aThresholds ) 
        Values.Add( lThreshold / 10.0f );
    }

    public List<float> Values = new List<float>();

    public override string ToString() =>  Values.Textualize();
  }

  public class Discretize : WaveFilter
  {
    public Discretize( ) 
    { 
    }

    protected override void OnSetup()
    {
      var lGT = new GateThresholds(Params.GetIntList("GateThresholds"));
      mGates.Add( new Gate($"{lGT.Values.Count}_steps",lGT) ) ;
    }

    public class Gate
    {
      public Gate( string aLabel, GateThresholds aThresholds )
      {
        Label      = aLabel; 
        Thresholds = aThresholds;
      }

      public float Apply( float aV )
      {
        for( int i = 0; i < Thresholds.Values.Count ; ++ i )
        {
          float lThreshold = Thresholds.Values[i] ; //* Scale ; 

          if ( aV >= lThreshold )
            return lThreshold; 
        }

        return 0f; 
      }

      readonly GateThresholds Thresholds ;

      internal float Scale ;

      internal string Label ;

      public override string ToString() => Thresholds.ToString() ;
    }


    protected override Packet Process ()
    {
      WaveSignal lSignal = WaveInput ;
      foreach ( var lGate in mGates )
      {
        WriteLine($"Applying Discretization Gate: {lGate}");
        lSignal = Apply( lSignal, lGate ) ;

      }
      return CreateOutput( lSignal, $"Discretized.") ;
    }

    WaveSignal Apply ( WaveSignal aInput, Gate aGate )
    {
      var rDiscrete = Apply(aInput.Rep, aGate) ;
      
      var rR = aInput.CopyWith(rDiscrete);

      Save(rR, $"Gated_{aGate.Label}.wav") ;

      return rR ;
    }

    public static DiscreteSignal Apply ( DiscreteSignal aInput, Gate aGate )
    {
      float lMax = aInput.GetPeak();

      aGate.Scale = lMax / 1.0f;
      
      float[] lSrc = aInput.Samples ;
      int lLen = lSrc.Length ;

      float[] rOutput = new float[lLen];

      for ( int i = 0 ; i < lLen ; i++ )  
        rOutput[i] = aGate.Apply(lSrc[i]) ;  

      return new DiscreteSignal(SIG.SamplingRate, rOutput);
    }

    List<Gate> mGates = new List<Gate>() ;

    public override string Name => this.GetType().Name ;

  }

}
