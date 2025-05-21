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
  public class Envelope_LowPass : WaveFilter
  {
    public class Params
    {
      public Params( double aFreqInHerz = 100, double aDeltaPass = 0.96, double aDeltaStop = 0.04, int aOrder = 5 )
      {
        Freq         = SIG.ToDigitalFrequency(aFreqInHerz) ;
        RipplePassDb = NWaves.Utils.Scale.ToDecibel( 1 / aDeltaPass ) ;
        AttenuateDB  = NWaves.Utils.Scale.ToDecibel( 1 / aDeltaStop ) ;
        Order        = aOrder ;
      }

      public NWaves.Filters.Elliptic.LowPassFilter CreateFilter() => new NWaves.Filters.Elliptic.LowPassFilter(Freq, Order, RipplePassDb, AttenuateDB);
      
      public double Freq; 
      public double RipplePassDb  ;
      public double AttenuateDB  ;
      public int    Order ;
    }

    class Iteration
    {
      internal Iteration( string aLabel, Params aParams )
      {
        Label  = aLabel; 
        Params = aParams;
      }

      internal string Label ;

      public Params Params ;

      public override string ToString() => Label;

      internal bool Plot => true ;
    }

    public Envelope_LowPass() 
    { 
    }

    protected override void Process ( WaveSignal aInput, Branch aInputBranch, List<Branch> rOutput )
    {
      List<Iteration> lIterationsA = new List<Iteration>
      {
        new Iteration( "A", new Params(1000, 0.96, 0.04, 5) )
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
      var lParams = aIteration.Params ;

      var lFiltered = Apply(aInput.Rep, lParams);

      var lES = aInput.CopyWith(lFiltered);

      if ( aIteration.Plot && DContext.Session.Args.GetBool("Plot") )
        lES.SaveTo( DContext.Session.LogFile( aIteration.Label + ".wav") ) ;

      return lES ;
    }

    public static DiscreteSignal Apply ( DiscreteSignal aSignal, Params aParams)
    {
      var lLowPass = aParams.CreateFilter();

      var rFiltered = lLowPass.ApplyTo(aSignal);

      return rFiltered;
    } 

    protected override string Name => "Envelope" ;

  }

}
