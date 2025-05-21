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
  public class Envelope_Following : WaveFilter
  {
    public class Params
    {
      public Params( float aAT, float aRT )
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

    public Envelope_Following() 
    { 
    }

    protected override void Process ( WaveSignal aInput, Branch aInputBranch, List<Branch> rOutput )
    {
      aInput.Rep.NormalizeMaxWithPeak();
      aInput.Rep.SquareRectify();

      List<Params> lIterationsA = new List<Params>
      {
        new Params(0.001f, .001f),
        new Params(0.001f, .001f),
        new Params(0.001f, .001f),
        new Params(0.001f, .001f),
        new Params(0.001f, .001f),
        new Params(0.005f, .005f),
        new Params(0.005f, .005f),
        new Params(0.005f, .005f),
        new Params(0.005f, .01f)
      };

      Process(lIterationsA, "10-steps", aInput, aInputBranch, rOutput ) ;
    }

    void Process ( List<Params> aIterations, string aLabel, WaveSignal aInput, Branch aInputBranch, List<Branch> rOutput )
    {
      WaveSignal rR = aInput;

      aIterations.ForEach( lI => rR = Apply(rR,lI) ) ;

      if ( DContext.Session.Args.GetBool("Plot") )
        rR.SaveTo( DContext.Session.LogFile( $"_{aLabel}_Envelope.wav") ) ;

      rOutput.Add( new Branch(aInputBranch, rR, aLabel));
    }

    WaveSignal Apply ( WaveSignal aInput, Params aParams )
    {
      var lNewRep = Apply(aInput.Rep, aParams) ; 
      lNewRep.Sanitize();

      var lES = aInput.CopyWith(lNewRep);

      if ( aParams.Plot && DContext.Session.Args.GetBool("Plot") )
        lES.SaveTo( DContext.Session.LogFile( aParams.Label + ".wav") ) ;

      return lES ;
    }

    public static DiscreteSignal Apply ( DiscreteSignal aInput, Params aIteration )
    {
      EnvelopeFollower envelopeFollower = new EnvelopeFollower(SIG.SamplingRate, aIteration.AttackTime, aIteration.ReleaseTime);

      var lNewSamples = aInput.Samples.Select(s => envelopeFollower.Process(s));

      return new DiscreteSignal(SIG.SamplingRate, lNewSamples);

    }

    protected override string Name => "Envelope" ;

  }

}
