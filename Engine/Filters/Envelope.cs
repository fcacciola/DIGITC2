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
      internal Iteration( string aLabel, WaveSignal.EnvelopeParams aEnvelopeParams )
      {
        Label = aLabel; 
        EnvelopeParams = aEnvelopeParams;
      }

      internal string Label ;

      public WaveSignal.EnvelopeParams EnvelopeParams ;

      public override string ToString() => Label;

      internal bool Plot => true ;
    }

    public Envelope() 
    { 
    }

    protected override void Process ( WaveSignal aInput, Branch aInputBranch, List<Branch> rOutput )
    {
      List<Iteration> lIterationsA = new List<Iteration>
      {
        new Iteration( "A", new WaveSignal.EnvelopeParams() )
      };

      Process(lIterationsA, aInput, aInputBranch, rOutput ) ;
    }

    void Process ( List<Iteration> aIterations, WaveSignal aInput, Branch aInputBranch, List<Branch> rOutput )
    {
      WaveSignal rR = aInput;

      aIterations.ForEach( lI => rR = Apply(rR,lI) ) ;

      if ( DContext.Session.Args.GetBool("Plot") )
        rR.SaveTo( DContext.Session.LogFile( $"_Envelope.wav") ) ;

      rOutput.Add( new Branch(aInputBranch, rR, "Envelope"));
    }

    WaveSignal Apply ( WaveSignal aInput, Iteration aIteration )
    {
      var lParams = aIteration.EnvelopeParams ;

      double lFreq = lParams.Freq / ( 0.5 * X.SamplingRate ) ;

      var ellip = new NWaves.Filters.Elliptic.LowPassFilter(lFreq, lParams.Order, lParams.RipplePassDb, lParams.AttenuateDB);

      aInput.Rep.SquareRectify(); 

      var lFiltered = ellip.ApplyTo(aInput.Rep);

      lFiltered.Sanitize();
      
      var lES = aInput.CopyWith(lFiltered);

      if ( aIteration.Plot && DContext.Session.Args.GetBool("Plot") )
        lES.SaveTo( DContext.Session.LogFile( aIteration.Label + ".wav") ) ;

      return lES ;
    }

    protected override string Name => "Envelope2" ;

  }

}
