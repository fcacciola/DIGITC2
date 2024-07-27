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
  public class Envelope2 : WaveFilter
  {
    class Iteration
    {
      internal Iteration( double aFreq, double aDeltaPass, double aDeltaStop )
      {
        Freq = aFreq ;

        RipplePassDb = NWaves.Utils.Scale.ToDecibel( 1 / aDeltaPass ) ;
        AttenuateDB  = NWaves.Utils.Scale.ToDecibel( 1 / aDeltaStop ) ;

        Order = 2 ;
      }

      internal string Label ;

      internal double Freq ;
      internal double RipplePassDb  ;
      internal double AttenuateDB  ;
      internal int    Order ;

      internal void SetupLabel( string aName, int i )
      {
        Label = aName + "_" + i + "_Envelope2_" + ( Freq) ;
      }

      public override string ToString() => $"{Freq}";

      internal bool Plot => true ;
    }

    public Envelope2() 
    { 
    }

    protected override void Process ( WaveSignal aInput, Branch aInputBranch, List<Branch> rOutput )
    {
      aInput.Rep.NormalizeMax();

      List<Iteration> lIterationsA = new List<Iteration>
      {
        new Iteration(10, 0.96, 0.04),
      };

      Process(lIterationsA, "A", aInput, aInputBranch, rOutput ) ;
    }

    void Process ( List<Iteration> aIterations, string aLabel, WaveSignal aInput, Branch aInputBranch, List<Branch> rOutput )
    {
      WaveSignal rR = aInput;

      aIterations.ForEach( lI => rR = Apply(rR,lI) ) ;

      if ( Context.Session.Args.GetBool("Plot") )
        rR.SaveTo( Context.Session.LogFile( $"_{aLabel}_Envelope.wav") ) ;

      rOutput.Add( new Branch(aInputBranch, rR, aLabel));
    }

    WaveSignal Apply ( WaveSignal aInput, Iteration aIteration )
    {
      int lSR = aInput.Rep.SamplingRate ;

      double lFreq = aIteration.Freq / ( 0.5 * lSR ) ;

      var ellip = new NWaves.Filters.Elliptic.LowPassFilter(lFreq, aIteration.Order, aIteration.RipplePassDb, aIteration.AttenuateDB);

      var lNewSamples0 = ellip.ApplyTo(aInput.Rep).Samples;

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

    protected override string Name => "Envelope2" ;

  }

}
