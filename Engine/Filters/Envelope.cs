using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using MathNet.Numerics.Statistics;

using NWaves.Filters;
using NWaves.Operations;
using NWaves.Signals;

using OxyPlot.Annotations;

using LowPassFilter = NWaves.Filters.Elliptic.LowPassFilter ;


namespace DIGITC2_ENGINE
{
  public class Envelope : WaveFilter
  {
    public class LowPassFilterParams 
    {
      public LowPassFilterParams( double aFreqInHerz )
      {
        FreqInHerz = aFreqInHerz; 
      }

      public double FreqInHerz = 1500;
      public double DeltaPass  = 0.96;
      public double DeltaStop  = 0.04;
      public int    Order      = 5;
    }

    public class Params 
    {
      public LowPassFilterParams LowPassA = new LowPassFilterParams(6000);
      public LowPassFilterParams LowPassB = new LowPassFilterParams(3000);
      public LowPassFilterParams LowPassC = new LowPassFilterParams(500);

      public float FollowerAttackTime  = 0.005f;
      public float FollowerReleaseTime = 0.01f;
    }

    public Envelope() 
    { 
    }

    protected override void Process ( WaveSignal aInput, Packet aInputPacket, List<Packet> rOutput )
    {
      DContext.WriteLine("Extracting Envelope from input signal");
      DContext.Indent();

      var lNewRep = Apply(aInput.Rep, mParams) ; 

      var rR = aInput.CopyWith(lNewRep);

      rOutput.Add( new Packet(Name, aInputPacket, rR, "Envelope"));
      DContext.Unindent();
    }

    static LowPassFilter CreateLowPassFilter( LowPassFilterParams aParams ) 
    {
      var Freq         = SIG.ToDigitalFrequency(aParams.FreqInHerz) ;
      var RipplePassDb = NWaves.Utils.Scale.ToDecibel( 1 / aParams.DeltaPass ) ;
      var AttenuateDB  = NWaves.Utils.Scale.ToDecibel( 1 / aParams.DeltaStop ) ;

      DContext.WriteLine($"Applying Elliptic LowPassFiler. Freq:{aParams.FreqInHerz} Hz RipplePass:{RipplePassDb} Db Attenuate:{AttenuateDB} Db");

      return new LowPassFilter(Freq, aParams.Order, RipplePassDb, AttenuateDB);
    }

    public static DiscreteSignal Apply ( DiscreteSignal aInput, Params aParams )
    {
      aInput.Sanitize();

      if ( DContext.Session.Args.GetBool("Plot") )
        aInput.SaveTo( DContext.Session.OutputFile( $"Envelope_Input.wav") ) ;

      //var lLowPass_A = CreateLowPassFilter(aParams.LowPassA);
      //var lLowPass_B = CreateLowPassFilter(aParams.LowPassB);
      //var lLowPass_C = CreateLowPassFilter(aParams.LowPassC);

      //var lFilteredA = lLowPass_A.ApplyTo(aInput);
      //var lFilteredB = lLowPass_B.ApplyTo(lFilteredA);
      //var lFilteredC = lLowPass_C.ApplyTo(lFilteredB);

      //if ( DContext.Session.Args.GetBool("Plot") )
      //{
      //  lFilteredA.SaveTo( DContext.Session.OutputFile( $"LowPass_{aParams.LowPassA.FreqInHerz}.wav") ) ;
      //  lFilteredB.SaveTo( DContext.Session.OutputFile( $"LowPass_{aParams.LowPassB.FreqInHerz}.wav") ) ;
      //  lFilteredC.SaveTo( DContext.Session.OutputFile( $"LowPass_{aParams.LowPassC.FreqInHerz}.wav") ) ;
      //}

      DContext.WriteLine($"Following Envelope. AttackTime: {aParams.FollowerAttackTime} ReleaseTime:{aParams.FollowerReleaseTime}");

      EnvelopeFollower envelopeFollower = new EnvelopeFollower(SIG.SamplingRate, aParams.FollowerAttackTime, aParams.FollowerReleaseTime);

      var rR = envelopeFollower.ApplyTo( aInput );
      rR.Sanitize(); 

      if ( DContext.Session.Args.GetBool("Plot") )
        rR.SaveTo( DContext.Session.OutputFile( $"Envelope.wav") ) ;

      //var rR2 = envelopeFollower.ApplyTo( lFilteredA );
      //rR2.Sanitize(); 
      //if ( DContext.Session.Args.GetBool("Plot") )
      //  rR2.SaveTo( DContext.Session.OutputFile( $"Envelope_FilteredA.wav") ) ;

      //var rR3 = envelopeFollower.ApplyTo( lFilteredB );
      //rR3.Sanitize(); 
      //if ( DContext.Session.Args.GetBool("Plot") )
      //  rR3.SaveTo( DContext.Session.OutputFile( $"Envelope_FilteredB.wav") ) ;

      //var rR = envelopeFollower.ApplyTo( lFilteredC );

      //var lSmoothing = new SavitzkyGolayFilter(31);  

      //var rR = lSmoothing.ApplyTo( lEnv); 

      //rR.Sanitize();

      return rR ;
    }

    Params mParams = new Params();

    public override string Name => this.GetType().Name ;

  }

}
