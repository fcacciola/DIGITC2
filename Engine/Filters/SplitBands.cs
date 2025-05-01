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

    public BandSplitter( double[] aCenterFrequencies, double aOverlapFactor = 0.0, double aSpectrumStart = 100, double aSpectrumEnd = 22000 )
    {
      mFrequencies = new (double, double, double)[aCenterFrequencies.Length];

      List<double> lExtendedCenters = new List<double> { aSpectrumStart };
      lExtendedCenters.AddRange(aCenterFrequencies);
      lExtendedCenters.Add(aSpectrumEnd);

      // Compute (low, high) pairs
      for (int i = 1; i < lExtendedCenters.Count - 1; i++)
      {
        double lCenterF = lExtendedCenters[i];

        // Geometric mean with overlap control
        double lLowF  = lCenterF * Math.Pow(lExtendedCenters[i - 1] / lCenterF, (1 + aOverlapFactor) / 2);
        double lHighF = lCenterF * Math.Pow(lExtendedCenters[i + 1] / lCenterF, (1 - aOverlapFactor) / 2);

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

    public List<Band> Split(DiscreteSignal aSignal)
    {
      List<Band> rBands = new List<Band>();

      foreach(var lBand in mFrequencies)
      {
        var lFiltered = FilterBand(aSignal, lBand.Item1, lBand.Item3);
        rBands.Add(new Band { Signal = lFiltered, Label = $"[{lBand.Item1}|{lBand.Item2}|{lBand.Item3}]" });
      } 
      return rBands;
    }

    DiscreteSignal FilterBand( DiscreteSignal aSignal, double aLowInHz, double aHighInHz )
    {
      double lNormalizedLow  = X.NormalizeFrequencyInHerz(aLowInHz);
      double lNormalizedHigh = X.NormalizeFrequencyInHerz(aHighInHz);

      int lOrder = 6;

      var lFilter = new NWaves.Filters.Butterworth.BandPassFilter(lNormalizedLow, lNormalizedHigh, lOrder);

      // Apply the filter
      var rFiltered = lFilter.ApplyTo(aSignal);

      return rFiltered; 
    }

    (double,double,double)[] mFrequencies ;
  }

  public class SplitBands : WaveFilter
  {
    public SplitBands( BandSplitter aSplitter ) 
    {
      mSplitter = aSplitter;  
    }

    protected override void Process ( WaveSignal aInput, Branch aInputBranch, List<Branch> rOutput )
    {
      var lBands = mSplitter.Split(aInput.Rep); 

      List<Branch> lBranches = new List<Branch>();

      foreach ( var lBand in lBands)
      {
        var lES = aInput.CopyWith(lBand.Signal);
        lES.Name = $"Band_{lBand.Label}";

        if ( DContext.Session.Args.GetBool("Plot") )
          lES.SaveTo( DContext.Session.LogFile( $"{lES.Name}.wav") ) ;

        lBranches.Add(new Branch(aInputBranch, lES, lES.Name) ) ;
      }


    }

    protected override string Name => "SplitBands" ;

    BandSplitter mSplitter ;

  }

}
