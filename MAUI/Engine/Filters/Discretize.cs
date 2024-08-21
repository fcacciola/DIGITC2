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
    public Discretize() 
    { 
    }

    class Gate
    {
      internal Gate( string aLabel,  params float[] aThresholds)
      {
        Thresholds = new List<float>();
        Thresholds.AddRange( aThresholds ); 
        Label = aLabel; 
      }

      internal Gate( string aLabel, List<float> aThresholds)
      {
        Thresholds = aThresholds;
        Label = aLabel; 
      }

      internal float Apply( float aV )
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

    Gate CreateGate( int aResolution )
    {
      List<float> lThresholds = new List<float>();  
      float lStep = 1.0f / aResolution; 
      for ( float lU = lStep ; lU < .98f ; lU += lStep )
        lThresholds.Add( lU );  
      lThresholds.Add(.98f);
      lThresholds.Reverse();
      return new Gate($"", lThresholds);
    }

    protected override void Process ( WaveSignal aInput, Branch aInputBranch, List<Branch> rOuput )
    {
      Process(10, aInput, aInputBranch, rOuput);
    }

    void Process ( int aResolution, WaveSignal aInput, Branch aInputBranch, List<Branch> rOuput )
    {
      var lR = Apply( aInput, CreateGate(aResolution) ) ;

      rOuput.Add( new Branch(aInputBranch, lR, $"Resolution:{aResolution}") ) ;
    }

    WaveSignal Apply ( WaveSignal aInput, Gate aGate )
    {
      float lMax = aInput.ComputeMax();

      aGate.Max = lMax ;
      
      float[] lSrc = aInput.Samples ;
      int lLen = lSrc.Length ;

      float[] rOutput = new float[lLen];

      for ( int i = 0 ; i < lLen ; i++ )  
        rOutput[i] = aGate.Apply(lSrc[i]) ;  

      var rR = aInput.CopyWith(new DiscreteSignal(aInput.SamplingRate, rOutput));

      if ( DIGITC_Context.Session.Args.GetBool("Plot") )
        rR.SaveTo( DIGITC_Context.Session.LogFile( "Discretize" + aGate.Label + ".wav") ) ;

      return rR ;
    }

    protected override string Name => "Discretize" ;

  }

}
