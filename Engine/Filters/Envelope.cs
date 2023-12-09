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

namespace DIGITC2
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

      internal bool Plot => false ;
    }

    public Envelope() 
    { 
    }

    protected override Step Process ( WaveSignal aInput, Step aStep )
    {
      WaveSignal rR = aInput;

      List<Iteration> lIterations = new List<Iteration>();

      lIterations.Add( new Iteration(0.001f,.001f) );
      lIterations.Add( new Iteration(0.001f,.001f) );
      lIterations.Add( new Iteration(0.001f,.001f) );
      lIterations.Add( new Iteration(0.001f,.001f) );
      lIterations.Add( new Iteration(0.001f,.001f) );
      lIterations.Add( new Iteration(0.005f,.005f) );
      lIterations.Add( new Iteration(0.005f,.005f) );
      lIterations.Add( new Iteration(0.005f,.005f) );
      lIterations.Add( new Iteration(0.005f,.01f) );

      //lIterations.ForEach( lI => lI.SetupLabel(aStep.Label, lIterations.IndexOf(lI)));  

      lIterations.ForEach( lI => rR = Apply(rR,lI) ) ;

      if ( Context.Session.Args.GetBool("Plot") )
        rR.SaveTo( Context.Session.LogFile( aStep.Label + "_Envelope.wav") ) ;

      mStep = aStep.Next( rR, "Envelope", this) ;

      return mStep ;
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

      if ( aIteration.Plot && Context.Session.Args.GetBool("Plot") )
        lES.SaveTo( Context.Session.LogFile( aIteration.Label + ".wav") ) ;

      return lES ;
    }

    protected override string Name => "Envelope" ;

  }

}
