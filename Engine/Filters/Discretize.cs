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
    public GateThresholds( params int[] aThresholds ) 
    { 
      foreach( int lThreshold in aThresholds ) 
        Values.Add( lThreshold / 10.0f );
    }

    public List<float> Values = new List<float>();
  }

  public class Discretize : WaveFilter
  {
    public Discretize( params GateThresholds[] aGTs ) 
    { 
      foreach( var lGT in aGTs ) 
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

      public override string ToString()
      {
        return string.Join(",",Thresholds);
      }
    }


    protected override void Process ( WaveSignal aInput, Packet aInputPacket, List<Packet> rOuput )
    {
      DContext.WriteLine($"Discretizing Input Signal.");
      DContext.Indent();
      WaveSignal lSignal = aInput ;
      foreach ( var lGate in mGates )
      {
        DContext.WriteLine($"Gate: {lGate}");
        lSignal = Apply( lSignal, lGate ) ;

      }
      rOuput.Add( new Packet(Name, aInputPacket, lSignal, $"Discretized.") ) ;

      DContext.Unindent();
    }

    WaveSignal Apply ( WaveSignal aInput, Gate aGate )
    {
      var rDiscrete = Apply(aInput.Rep, aGate) ;
      
      var rR = aInput.CopyWith(rDiscrete);

      if ( DContext.Session.Args.GetBool("Plot") )
        rR.SaveTo( DContext.Session.OutputFile( $"Gated_{aGate.Label}.wav") ) ;

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
