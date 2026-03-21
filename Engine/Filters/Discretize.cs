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
    public GateThresholds( float aCut, float aOutLevel ) 
    {
      Cuts     .Add( aCut  );
      OutLevels.Add(aOutLevel);
    }

    public List<float> Cuts      = new List<float>();
    public List<float> OutLevels = new List<float>();

    public override string ToString() =>  Cuts.Textualize();
  }


    
  public class Discretize : WaveFilter
  {
    public Discretize( ) 
    { 
    }

    protected override void OnSetup()
    {

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
        for( int i = 0; i < Thresholds.Cuts.Count ; ++ i )
        {
          float lCut      = Thresholds.Cuts     [i] ;
          float lOutLevel = Thresholds.OutLevels[i] ;

          if ( aV >= lCut )
            return lOutLevel; 
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
      var lGT = new GateThresholds( 0.25f, 0.85f );

      mGates.Add( new Gate($"DiscretizeAt_{lGT.Cuts[0]}",lGT) ) ;

      WaveSignal lSignal = WaveInput ;
      foreach ( var lGate in mGates )
      {
        WriteLine2GUI($"Applying Discretization Gate: {lGate}");
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
