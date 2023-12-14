using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Operations;
using NWaves.Signals;

using MathNet.Numerics.Statistics;

namespace DIGITC2
{
  public class ExtractGatedlSymbols : WaveFilter
  {
    public ExtractGatedlSymbols() 
    { 
    }

    protected override Step Process ( WaveSignal aInput, Step aStep )
    {
      mInput = aInput ; 

      CreateSymbols();

      MergeSymbols();

      RemoveShortSymbols();

      PlotSymbolsAsWave(mFinal, "Final");

      mStep = aStep.Next( new LexicalSignal(mFinal), "Gated Symbols", this) ;

      return mStep ;
    }
    
    protected override string Name => "ExtractGatedSymbols" ;

    void CreateSymbols()
    {
      mRawSymbols = new List<GatedSymbol>();

      mCurrCount = 0; 
      mPos       = 0 ;
      foreach( float lV in mInput.Samples)
      {
        if( mCurrCount == 0 || mCurrLevel != lV ) 
        {
          AddSymbol();

          mCurrLevel = lV;
          mCurrCount = 1 ;
        }
        else
        { 
          mCurrCount ++ ;
        }

        mPos ++ ;
      }

      AddSymbol();

      PlotSymbolsAsWave(mRawSymbols,"Raw");

      List<double> lDurations = GetDurations(mRawSymbols);

      var lMergedHistogram = GetHistogram(lDurations) ;  

      if ( Context.Session.Args.GetBool("Plot") )
      { 
        lMergedHistogram.CreatePlot(Plot.Options.Bars).SavePNG(Context.Session.LogFile("GatedSymbols_RawSymbols_Durations_Histogram.png"));
      }

    }

    void AddSymbol()
    {
      if ( mCurrCount > 0 )
        mRawSymbols.Add( new GatedSymbol(mRawSymbols.Count, mCurrLevel, mInput.SamplingRate, mPos - mCurrCount, mCurrCount ) );
    }


    List<double> GetGaps()
    {
      List<double> rGaps = new List<double>() ;

      for( int i = 0; i < mRawSymbols.Count ; )  
      {
        GatedSymbol lA = mRawSymbols[i];

        if ( lA.IsSymbol )
        {
          double lGapLen = 0 ;

          int j = i + 1 ;

          bool lNextSymbolFound = false ;

          for( ; j < mRawSymbols.Count && ! lNextSymbolFound; j++ )  
          {
            GatedSymbol lB = mRawSymbols[j];

            if ( lB.IsSymbol )
            {
              lNextSymbolFound = true ; 

              rGaps.Add(lGapLen) ;
            }
            else
            {
              lGapLen += lB.Length ;
            }
          }
          i = j ;
        }
        else
        {
          ++ i;
        }
      }

      rGaps.Sort();

      if ( Context.Session.Args.GetBool("Plot") )
      { 
        var lGapsHistogram = GetHistogram(rGaps) ;  
        lGapsHistogram.CreatePlot(Plot.Options.Bars).SavePNG(Context.Session.LogFile("GatedSymbols_Gaps_Histogram.png"));
      }

      return rGaps ;
        
    }

    DTable GetHistogram( List<double> aValues )
    {
      var lDist = new Distribution(aValues) ;

      var lRawHistogram = new Histogram(lDist) ;

      var lFullRangeHistogram = lRawHistogram.Table ;

      var rHistogram = lFullRangeHistogram.Normalized();

      return rHistogram;
    }

    void MergeSymbols()
    {
      int lIteration  = 0 ;

      var lSrcSymbols = mRawSymbols ;

      List<GatedSymbol> lTgtSymbols ;

      int lMergeCount ;

      var lGaps = GetGaps() ;

      do
      {
        lMergeCount = 0 ;

        double lGapTheshold = lGaps.Percentile(5 + ( lIteration << 2 ) ); 

        lTgtSymbols = new List<GatedSymbol>();

        for( int i = 0; i < lSrcSymbols.Count ; )  
        {
          GatedSymbol lA = lSrcSymbols[i];

          if ( lA.IsSymbol )
          {
            double lGapLen = 0 ;

            int j = i + 1 ;

            bool lNextSymbolFound = false ;
            bool lMerged          = false ;

            for( ; j < lSrcSymbols.Count && ! lNextSymbolFound; j++ )  
            {
              GatedSymbol lB = lSrcSymbols[j];

              if ( lB.IsSymbol )
              {
                lNextSymbolFound = true ; 

                lMerged = lA.Amplitude == lB.Amplitude && lGapLen < lGapTheshold ;  

                if ( lMerged )
                {
                  MergeSymbols(lSrcSymbols, lTgtSymbols, i,j);
                  ++ lMergeCount ;
                }
              }
              else
              {
                lGapLen += lB.Length ;
              }
            }

            if ( ! lMerged )
            {
              for ( int k = i ; k < j ; ++ k )
                lTgtSymbols.Add( lSrcSymbols[k] );
            }

            i = j ;
          }
          else
          {
            lTgtSymbols.Add(lA);
            ++ i;
          }
        }

        PlotSymbolsAsWave(lTgtSymbols,"Merged_" + lIteration );

        ++ lIteration ;

        lSrcSymbols = lTgtSymbols; 
      }
      while ( lMergeCount > 0 && lIteration < 15 )  ;

      mMerged = lTgtSymbols ;
    }

    void MergeSymbols(List<GatedSymbol> aSrcSymbols, List<GatedSymbol> aMergedSymbols, int aFrom, int aTo )
    {
      int lMergedLen = 0 ;

      for ( int i = aFrom ; i <= aTo ; i++ ) 
        lMergedLen += aSrcSymbols[i].Length ; 

        aMergedSymbols.Add( new GatedSymbol(aMergedSymbols.Count, aSrcSymbols[aFrom].Amplitude, mInput.SamplingRate, aSrcSymbols[aFrom].Pos, lMergedLen));
    }

    List<double> GetDurations( List<GatedSymbol> aSymbols )  
    {
      List<double> rDurations = new List<double>() ;

      aSymbols.ForEach( s => { if ( s.IsSymbol ) rDurations.Add(s.Duration) ; } );

      rDurations.Sort(); 

      return rDurations ; 

    }

    void RemoveShortSymbols()
    {
      List<double> lDurations = GetDurations(mMerged);

      var lMergedHistogram = GetHistogram(lDurations) ;  

      if ( Context.Session.Args.GetBool("Plot") )
      { 
        lMergedHistogram.CreatePlot(Plot.Options.Bars).SavePNG(Context.Session.LogFile("GatedSymbols_Merged_Durations_Histogram.png"));
      }

      double lMinDuration = lDurations.Percentile(25); 

      mFinal = new List<GatedSymbol>();

      mMerged.ForEach( s => { if ( s.Duration >= lMinDuration ) mFinal.Add(s); } );
    }

    void PlotSymbolsAsWave( List<GatedSymbol> aSymbols, string aLabel )
    {
      if ( Context.Session.Args.GetBool("Plot") )
      {
        List<float> lSamples = new List<float> ();
        aSymbols.ForEach( s => s.DumpSamples(lSamples ) );
        DiscreteSignal lWaveRep = new DiscreteSignal(mInput.SamplingRate, lSamples);
        WaveSignal lWave = new WaveSignal(lWaveRep);
        lWave.SaveTo( Context.Session.LogFile( "GatedSymbols_" + aLabel + ".wav") ) ;
      }
    }

    WaveSignal        mInput ;
    int               mPos ;
    float             mCurrLevel ;
    int               mCurrCount ;
    List<GatedSymbol> mRawSymbols ;
    List<GatedSymbol> mMerged ;
    List<GatedSymbol> mFinal ;
  }

}
