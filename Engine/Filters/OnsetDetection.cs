using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using DocumentFormat.OpenXml.Presentation;

using MathNet.Numerics.Statistics;

using NWaves.Operations;
using NWaves.Signals;

using OxyPlot.Annotations;

namespace DIGITC2_ENGINE
{

  // ABSTRACT whatever 3rd party lib to use here
  public class OnsetDetector
  {
    public class Options
    {
      public Options()
      {
      }

      public float Sensitivity = 0.5f;
      public float ThresholdTimeSpan = 0.5f; 
    }

    public class Onset
    {
      public Onset(double aOnsetTime )
      {
        OnsetTime = aOnsetTime;
      }
      public double OnsetTime;
    }

    public class Result
    {
      public List<Onset> Onsets;

      public int Count => Onsets.Count;

      public Result(List<Onset> aOnsets)
      {
        Onsets = aOnsets;
      }

    }

    public OnsetDetector(Options aOptions)
    {
      mOptions = aOptions;  
    } 


    public Result Detect(WaveSignal aSignal)
    {
//      double[] lBandCenters = new double[]{2500,5000,7500,10000,12500,15000};
      double[] lBandCenters = new double[]{7500,10000};
      double lOverlap = .2 ;

      var lSplitter = new BandSplitter(lBandCenters,lOverlap);

      var lBands = lSplitter.Split(aSignal.Rep);

      foreach (var lBand in lBands)
      {
        if ( DContext.Session.Args.GetBool("Plot") )
          lBand.Signal.SaveTo( DContext.Session.LogFile( $"{aSignal.Name}_{lBand.Label}.wav") ) ;

        var lBandOnsets = Detect(lBand.Signal, $"{aSignal.Name}_{lBand.Label}");
      }

      List<Onset> lOnsets = new List<Onset>();

      return new Result(lOnsets);
    } 

    List<Onset> Detect(DiscreteSignal aSignal, string aBaseName )
    {
      List<Onset> lOnsets = new List<Onset>();

      var lELP_Params = new Envelope_LowPass.Params(500, 0.96, 0.04, 5);

      aSignal.SquareRectify();

      var lEnvelope1 = Envelope_LowPass.Apply(aSignal, lELP_Params);  

      lEnvelope1.Sanitize();  

      if ( DContext.Session.Args.GetBool("Plot") )
        lEnvelope1.SaveTo( DContext.Session.LogFile( $"{aBaseName}_Envelope1.wav") ) ;

      var lEF_Params = new Envelope_Following.Params(0.005f, .01f);

      var lEnvelope2 = Envelope_Following.Apply(lEnvelope1, lEF_Params);

      lEnvelope2.Sanitize();

      if ( DContext.Session.Args.GetBool("Plot") )
        lEnvelope2.SaveTo( DContext.Session.LogFile( $"{aBaseName}_Envelope2.wav") ) ;

      var lGate = Discretize.CreateGate(3);

      var lGated = Discretize.Apply( lEnvelope2, lGate) ;

      if ( DContext.Session.Args.GetBool("Plot") )
        lGated.SaveTo( DContext.Session.LogFile( $"{aBaseName}_Gated.wav") ) ;

      return lOnsets;
    }

    Options mOptions;
  }

  public class OnsetDetection : WaveFilter
  {
    public class Onset : IWithState
    {
      public Onset( List<double> aTimes, List<int> aPositions )
      {
        Times     = aTimes ;
        Positions = aPositions ;
      }  

      public State GetState() => State.With("Oneset", Times.ToArray());

      public List<double> Times     ;
      public List<int>    Positions ;
    }

    public OnsetDetection() 
    { 
    }

    public override void Setup() 
    { 
      mThreshold   = DContext.Session.Args.GetOptionalDouble("OnsetDetection_Threshold")  .GetValueOrDefault(0.4);
      mMinTapCount = DContext.Session.Args.GetOptionalInt   ("OnsetDetection_MinTapCount").GetValueOrDefault(16);
    }

    protected override void Process ( WaveSignal aInput, Branch aInputBranch, List<Branch> rOutput )
    {
      var lOnsetDetectorOptions = new OnsetDetector.Options();  
      var lOnsetDetector = new OnsetDetector(lOnsetDetectorOptions);

      var lResult = lOnsetDetector.Detect(aInput); 

      DContext.WriteLine($"Onset Count: {lResult.Count}");

      if ( lResult.Count >= mMinTapCount )
      {
        var lTimes = lResult.Onsets.ConvertAll( st => (double)st.OnsetTime ); 

        if ( lTimes[0] == 0.0 )
          lTimes.RemoveAt(0);

        var lPositions = lTimes.ConvertAll( t => (int)Math.Round(t * (double)SIG.SamplingRate) ) ;

        Onset lOnset = new Onset(lTimes, lPositions) ;

        int lLen = aInput.Rep.Length;

        float[] lOutSignal = new float[lLen];

        for ( int i = 0 ; i < lLen ; i++ )  
          lOutSignal[i] = 0 ;  

        foreach( int lPos in lPositions )
          lOutSignal[lPos] = 1 ;  

        var rR = aInput.CopyWith(new DiscreteSignal(SIG.SamplingRate, lOutSignal));

        if ( DContext.Session.Args.GetBool("Plot") )
          rR.SaveTo( DContext.Session.LogFile( $"_OnsetSequence.wav") ) ;

        rOutput.Add( new Branch(aInputBranch, rR, "OnsetSequence", null, false, lOnset));
      }
      else
      {
        rOutput.Add( new Branch(aInputBranch, null, "OnsetSequence-EMPTY", null, true));
      }
    }

    double mThreshold ;
    int    mMinTapCount ;

    protected override string Name => "OnsetDetection" ;

  }

}
