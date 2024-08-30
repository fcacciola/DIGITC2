using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using MathNet.Numerics.Statistics;

using NWaves.Operations;
using NWaves.Signals;

using OxyPlot.Annotations;

namespace DIGITC2_ENGINE
{
  public class Envelope : WaveFilter
  {
    class Iteration
    {
      internal Iteration( float aAT, float aRT )
      {
        AttackTime  = aAT;
        ReleaseTime = aRT;
      }

      internal string Label ;

      internal float AttackTime ;
      internal float ReleaseTime ;

      internal void SetupLabel( string aName, int i )
      {
        Label = aName + "_" + i + "_Envelope_" + ( AttackTime * 10000 ) + "_" + ( ReleaseTime * 10000 );
      }

      public override string ToString() => $"{AttackTime}|{ReleaseTime}";

      internal bool Plot => true ;
    }

    public Envelope() 
    { 
    }

    protected override void Process ( WaveSignal aInput, Branch aInputBranch, List<Branch> rOutput )
    {
      aInput.Rep.NormalizeMax();

      List<Iteration> lIterationsA = new List<Iteration>
      {
        new Iteration(0.001f, .001f),
        new Iteration(0.001f, .001f),
        new Iteration(0.001f, .001f),
        new Iteration(0.001f, .001f),
        new Iteration(0.001f, .001f),
        new Iteration(0.005f, .005f),
        new Iteration(0.005f, .005f),
        new Iteration(0.005f, .005f),
        new Iteration(0.005f, .01f)
      };

      Process(lIterationsA, "10-steps", aInput, aInputBranch, rOutput ) ;
    }

    void Process ( List<Iteration> aIterations, string aLabel, WaveSignal aInput, Branch aInputBranch, List<Branch> rOutput )
    {
      WaveSignal rR = aInput;

      aIterations.ForEach( lI => rR = Apply(rR,lI) ) ;

      if ( DContext.Session.Args.GetBool("Plot") )
        rR.SaveTo( DContext.Session.LogFile( $"_{aLabel}_Envelope.wav") ) ;

      rOutput.Add( new Branch(aInputBranch, rR, aLabel));
    }

    WaveSignal Apply ( WaveSignal aInput, Iteration aIteration )
    {
      int lSR = aInput.Rep.SamplingRate ;

      EnvelopeFollower envelopeFollower = new EnvelopeFollower(lSR, aIteration.AttackTime, aIteration.ReleaseTime);

      var lNewSamples0 = aInput.Rep.Samples.Select(s => envelopeFollower.Process(s));

      var lNewSamples1 = lNewSamples0.Where( s => s > 1e-4 && s <= 1.0 ) ;

      var lOrdered = lNewSamples1.OrderByDescending( s => s ).ToList() ;

      float lPeak1 = lOrdered[0];
      float lPeak2 = lOrdered[1];
      float lPeak3 = lOrdered[2];

      float lPeak = Math.Min(lPeak1 , Math.Min(lPeak2,lPeak3));

      float lScale = 0.95f / lPeak ;

      var lNewSamples = lNewSamples1.Select(s => s * lScale);

      var lESRep = new DiscreteSignal(lSR, lNewSamples);

      var lES = aInput.CopyWith(lESRep);

      if ( aIteration.Plot && DContext.Session.Args.GetBool("Plot") )
        lES.SaveTo( DContext.Session.LogFile( aIteration.Label + ".wav") ) ;

      return lES ;
    }

    protected override string Name => "Envelope" ;

  }

}
