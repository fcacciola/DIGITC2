using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Operations;
using NWaves.Signals;

namespace DIGITC2_ENGINE
{
  public class Discretize : WaveFilter
  {
    public Discretize( int aResolution = 10 ) 
    { 
      mResolution = aResolution;
    }

    public class Gate
    {
      public Gate( string aLabel, params float[] aThresholds)
      {
        Thresholds = new List<float>();
        Thresholds.AddRange( aThresholds ); 
        Label = aLabel; 
      }

      public  Gate( string aLabel, List<float> aThresholds)
      {
        Thresholds = aThresholds;
        Label = aLabel; 
      }

      public float Apply( float aV )
      {
        for( int i = 1; i < Thresholds.Count ; ++ i )
        {
          if ( aV > Thresholds[i] )
            return Thresholds[i-1]; 
        }

        return 0f; 
      }

      readonly List<float> Thresholds ;

      internal float Max ;

      internal string Label ;

      public override string ToString()
      {
        return string.Join(",",Thresholds);
      }
    }

    static public Gate CreateGate( int aResolution )
    {
      List<float> lThresholds = new List<float>();  
      float lStep = 1.0f / aResolution; 
      for ( float lU = lStep ; lU < .98f ; lU += lStep )
        lThresholds.Add( lU );  
      lThresholds.Add(.98f);
      lThresholds.Reverse();
      return new Gate($"{aResolution}_steps", lThresholds);
    }

    protected override void Process ( WaveSignal aInput, Packet aInputPacket, List<Packet> rOuput )
    {
      Process(mResolution, aInput, aInputPacket, rOuput);
    }

    void Process ( int aResolution, WaveSignal aInput, Packet aInputPacket, List<Packet> rOuput )
    {
      var lR = Apply( aInput, CreateGate(aResolution) ) ;

      rOuput.Add( new Packet(aInputPacket, lR, $"Resolution:{aResolution}") ) ;
    }

    WaveSignal Apply ( WaveSignal aInput, Gate aGate )
    {
      var rDiscrete = Apply(aInput.Rep, aGate) ;
      
      var rR = aInput.CopyWith(rDiscrete);

      if ( DContext.Session.Args.GetBool("Plot") )
        rR.SaveTo( DContext.Session.OutputFile( $"{aInput.Name}_Gated_" + aGate.Label + ".wav") ) ;

      return rR ;
    }

    public static DiscreteSignal Apply ( DiscreteSignal aInput, Gate aGate )
    {
      float lMax = aInput.GetPeak();

      aGate.Max = lMax ;
      
      float[] lSrc = aInput.Samples ;
      int lLen = lSrc.Length ;

      float[] rOutput = new float[lLen];

      for ( int i = 0 ; i < lLen ; i++ )  
        rOutput[i] = aGate.Apply(lSrc[i]) ;  

      return new DiscreteSignal(SIG.SamplingRate, rOutput);
    }

    int mResolution = 10 ;

    public override string Name => this.GetType().Name ;

  }

}
