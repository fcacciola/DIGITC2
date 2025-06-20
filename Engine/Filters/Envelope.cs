﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using MathNet.Numerics.Statistics;

using NWaves.Operations;
using NWaves.Signals;

using OxyPlot.Annotations;

using LowPassFilter = NWaves.Filters.Elliptic.LowPassFilter ;


namespace DIGITC2_ENGINE
{
  public class Envelope : WaveFilter
  {
    public class Params 
    {
      public double LowPassFreqInHerz   = 500;
      public double LowPassDeltaPass    = 0.96;
      public double LowPassDeltaStop    = 0.04;
      public int    LowPassOrder        = 5;
      public float  FollowerAttackTime  = 0.005f;
      public float  FollowerReleaseTime = 0.01f;
    }

    public Envelope() 
    { 
    }

    protected override void Process ( WaveSignal aInput, Packet aInputPacket, List<Packet> rOutput )
    {
      DContext.WriteLine("Extracting Envelope from input signal");
      DContext.Indent();

      var lLowPass = CreateLowPassFilter();

      var lNewRep = Apply(aInput.Rep, mParams, lLowPass) ; 

      var rR = aInput.CopyWith(lNewRep);

      string lLabel = "Envelope";

      if ( DContext.Session.Args.GetBool("Plot") )
        rR.SaveTo( DContext.Session.OutputFile( $"{lLabel}.wav") ) ;

      rOutput.Add( new Packet(Name, aInputPacket, rR, lLabel));
      DContext.Unindent();
    }

    LowPassFilter CreateLowPassFilter() 
    {
      var Freq         = SIG.ToDigitalFrequency(mParams.LowPassFreqInHerz) ;
      var RipplePassDb = NWaves.Utils.Scale.ToDecibel( 1 / mParams.LowPassDeltaPass ) ;
      var AttenuateDB  = NWaves.Utils.Scale.ToDecibel( 1 / mParams.LowPassDeltaStop ) ;

      DContext.WriteLine($"Applying Elliptic LowPassFiler. Freq:{mParams.LowPassFreqInHerz} Hz RipplePass:{RipplePassDb} Db Attenuate:{AttenuateDB} Db");

      return new LowPassFilter(Freq, mParams.LowPassOrder, RipplePassDb, AttenuateDB);
    }

    public static DiscreteSignal Apply ( DiscreteSignal aInput, Params aParams, LowPassFilter aFilter )
    {
      DContext.WriteLine("Peak Normalizing Input Signal");
      aInput.NormalizeMaxWithPeak();

      DContext.WriteLine("Square-Rectifying Input Signal");
      aInput.SquareRectify();

      var lLowPassed = aFilter.ApplyTo(aInput);

      lLowPassed.Sanitize();

      DContext.WriteLine($"Following Envelope. AttackTime: {aParams.FollowerAttackTime} ReleaseTime:{aParams.FollowerReleaseTime}");

      EnvelopeFollower envelopeFollower = new EnvelopeFollower(SIG.SamplingRate, aParams.FollowerAttackTime, aParams.FollowerReleaseTime);

      var lNewSamples = aInput.Samples.Select(s => envelopeFollower.Process(s));

      var rR = new DiscreteSignal(SIG.SamplingRate, lNewSamples);

      rR.Sanitize();

      return rR ;
    }

    Params mParams = new Params();

    public override string Name => this.GetType().Name ;

  }

}
