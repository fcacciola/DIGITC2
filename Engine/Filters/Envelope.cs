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
      public float FollowerAttackTime  = 0.005f;
      public float FollowerReleaseTime = 0.01f;

      public override string ToString() => $"A_{(int)(FollowerAttackTime*1000)}_R_{(int)(FollowerReleaseTime*1000)}";
    }

    public Envelope() 
    { 
    }

    protected override Packet Process ( WaveSignal aInput, Config aConfig, Packet aInputPacket, List<Config> rBranches )
    {
      DContext.WriteLine("Extracting Envelope from input signal");
      DContext.Indent();

      if ( DContext.Session.Settings.GetBool("Plot") )
        aInput.SaveTo( DContext.Session.OutputFile( $"Envelope_Input.wav") ) ;

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

    public static DiscreteSignal Apply ( DiscreteSignal aInput, Params aParams = null )
    {
      var lParams = aParams ?? new Params() ;

      DContext.WriteLine($"Following Envelope. AttackTime: {lParams.FollowerAttackTime} ReleaseTime:{lParams.FollowerReleaseTime}");

      EnvelopeFollower envelopeFollower = new EnvelopeFollower(SIG.SamplingRate, lParams.FollowerAttackTime, lParams.FollowerReleaseTime);

      var rR = envelopeFollower.ApplyTo( aInput );
      rR.Sanitize(); 

      if ( DContext.Session.Settings.GetBool("Plot") )
        rR.SaveTo( DContext.Session.OutputFile( $"Envelope.wav") ) ;

      return rR ;
    }

    Params mParams = new Params();

    public override string Name => this.GetType().Name ;

  }

}
