using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using MathNet.Numerics.Statistics;

using NWaves.Operations;
using NWaves.Signals;
using NWaves.Signals;
using NWaves.Transforms;
using NWaves.Filters.Fda;
using NWaves.Utils;


using OxyPlot.Annotations;

namespace DIGITC2_ENGINE
{

  public class BandSplitter
  {
    public class Band
    {
      public DiscreteSignal Signal;
      public string         Label ;
    }

    public BandSplitter( List<double> aCenterFrequencies, double aOverlapFactor = 0.0, double aSpectrumStart = 100, double aSpectrumEnd = 22000 )
    {
      mFrequencies = new (double, double, double)[aCenterFrequencies.Count];

      List<double> lExtendedCenters = new List<double> { aSpectrumStart };
      lExtendedCenters.AddRange(aCenterFrequencies);
      lExtendedCenters.Add(aSpectrumEnd);

      // Compute (low, high) pairs
      for (int i = 1; i < lExtendedCenters.Count - 1; i++)
      {
        double lCenterF = lExtendedCenters[i];

        double lDPrev_ = lExtendedCenters[i    ] - lExtendedCenters[i - 1];
        double lDNext_ = lExtendedCenters[i + 1] - lExtendedCenters[i];  

        double lDPrev = ( 1.0 + aOverlapFactor) * lDPrev_;
        double lDNext = ( 1.0 + aOverlapFactor) * lDNext_;

        double lLowF  = lCenterF - lDPrev / 2;
        double lHighF = lCenterF + lDNext / 2;

        mFrequencies[i-1] = (lLowF, lCenterF, lHighF); 
      }
    }

    public BandSplitter( (double,double)[] aFrequencies_LH )
    {
      mFrequencies = new (double, double, double)[aFrequencies_LH.Length];

      for (int i = 0; i < aFrequencies_LH.Length; i++)
      {
        var original = aFrequencies_LH[i];
        double middle = (original.Item1 + original.Item2) / 2.0;
        mFrequencies[i] = (original.Item1, middle, original.Item2);
      }
    }

    public BandSplitter( (double,double,double)[] aFrequencies_LMH )
    {
      mFrequencies = aFrequencies_LMH;
    }

    public BandSplitter( int aNumberOfBands )
    {
      mFrequencies = FilterBanks.HerzBands(aNumberOfBands, 44100, 100, 22.000, true); // 100hz -> 22khz as frequency range.
    }

    string BandFrequenecyToString( double aD ) => $"{(int)(aD)}";
    string BandFrequenecyToString((double, double, double) aD) => $"[{BandFrequenecyToString(aD.Item1)}_{BandFrequenecyToString(aD.Item2)}_{BandFrequenecyToString(aD.Item3)}]";

    public List<Band> Split(DiscreteSignal aSignal)
    {
      DContext.WriteLine($"Splitting input signal into {mFrequencies.Length} Frequency Bands...");

      List<Band> rBands = new List<Band>();

      foreach(var lBFrequencies in mFrequencies)
      {
        var lFiltered = FilterBand(aSignal, lBFrequencies.Item1, lBFrequencies.Item3);
        var rBand = new Band { Signal = lFiltered, Label = BandFrequenecyToString(lBFrequencies) };
        DContext.WriteLine($"  Band: {rBand.Label}");
        rBands.Add( rBand );
      } 
      return rBands;
    }

    DiscreteSignal FilterBand( DiscreteSignal aSignal, double aLowInHz, double aHighInHz )
    {
      double lNormalizedLow  = SIG.ToNormalizedDigitalFrequency(aLowInHz );
      double lNormalizedHigh = SIG.ToNormalizedDigitalFrequency(aHighInHz);

      int lOrder = 6;

      var lFilter = new NWaves.Filters.Butterworth.BandPassFilter(lNormalizedLow, lNormalizedHigh, lOrder);

      var rFiltered = lFilter.ApplyTo(aSignal);

      return rFiltered; 
    }

    (double,double,double)[] mFrequencies ;
  }

  public class SelectBand : WaveFilter
  {
    public SelectBand() 
    {
    }

    protected override void OnSetup()
    {
      // An Empty list means to do NO SPLITTNG

      string lFCV = Params.Get("Frequency");

      if ( !string.IsNullOrEmpty(lFCV) ) 
      {
        var lFCListStr = lFCV.Split(',');

        List<double> lFrequencyCenters = new List<double>();
        
        foreach ( var lFCStr in lFCListStr ) 
        {
          if ( double.TryParse(lFCStr, out double lFC ) )
            lFrequencyCenters.Add( lFC );
        }

        if ( lFrequencyCenters.Count > 0 ) 
          mSplitter = new BandSplitter(lFrequencyCenters, aOverlapFactor: .2);
      }

    }

    protected override Packet Process ()
    {
      // mSPlitter can be null if the settings do not specify the frequency centers
      // as a way to indcate to not split the audio in bands.

      //if ( mSplitter != null ) 
      //{  
      //  var lBands = mSplitter.Split(aInput.Rep); 

      //  foreach ( var lBand in lBands)
      //  {
      //    var lES = aInput.CopyWith(lBand.Signal);
      //    lES.Name = $"Band_{lBand.Label}";

      //    if ( DContext.Session.Settings.GetBool("Plot") )
      //      lES.SaveTo( DContext.Session.OutputFile( $"{lES.Name}.wav") ) ;

      //    rOutput.Add(new Packet(Name, aInputPacket, lES, lES.Name) ) ;
      //  }
      //}
      //else
      {
        DContext.WriteLine("Passing input signal AS-IS. No band splitting specified.");

        return CreateOutput(WaveInput.Copy(), $"{WaveInput.Name}_UNSPLIT") ;
      }
    }

    public override string Name => this.GetType().Name ;

    BandSplitter mSplitter ;

  }

}
