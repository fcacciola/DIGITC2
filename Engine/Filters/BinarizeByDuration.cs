﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DocumentFormat.OpenXml.Drawing;

using MathNet.Numerics.Integration;
using MathNet.Numerics.Statistics;

using Newtonsoft.Json.Serialization;

using NWaves.Operations;
using NWaves.Signals;

namespace DIGITC2
{

  public class BinarizeByDuration : LexicalFilter
  {
    public BinarizeByDuration() 
    { 
    }

    enum BitType { One, Zero, Noise } ;
    enum BranchName {  BranchA, BranchB } ;

    class FilterBranch
    {
      internal FilterBranch ( BranchName aBranchName )
      {
        mBranchName = aBranchName ;
      }

      internal void Add( ClassifiedPulse aCPulse )
      {
        var lBitType = aCPulse.Classification.GetBitType(mBranchName);

        if ( lBitType != BitType.Noise )
          AddBit( aCPulse.Pulse, lBitType == BitType.One);
      }

      void AddBit( PulseSymbol aPulse, bool aIsOne ) 
      {
        PulseSymbol lView = aIsOne ? PulseFilterHelper.CreateOnePulse(aPulse) : PulseFilterHelper.CreateZeroPulse(aPulse);

        mBits.Add( new BitSymbol( mBits.Count, aIsOne, 1.0, lView )) ;
      }

      internal LexicalSignal GetSignal()
      {
        return new LexicalSignal(mBits);
      }

      internal string Label => mBranchName.ToString();

      BranchName      mBranchName ;
      List<BitSymbol> mBits = new List<BitSymbol> ();
    }
    
    class Classification
    {
      internal BitType GetBitType( BranchName aBranchName )
      {
        return aBranchName == BranchName.BranchA ? ForBranchA : ForBranchB ;
      }

      public override string ToString() => $"[{ForBranchA}|{ForBranchB}]";
      
      internal BitType ForBranchA ;
      internal BitType ForBranchB ;
    }

    class ClassifiedPulse
    {
      internal PulseSymbol    Pulse ;
      internal Classification Classification ;

      public override string ToString() => $"[{Pulse}|{Classification}]";
    }

    class Classifier
    {
      internal static List<ClassifiedPulse> Run( LexicalSignal aInput )
      {
        Classifier rC = new Classifier(aInput);

        return rC.Estimate();
      }

      Classifier( LexicalSignal aInput )
      {
        mInput = aInput;  
      }

      internal class Cluster
      {
        internal Cluster( double aL, double aH )
        {
          mL = aL ;
          mH = aH ;
          mM = ( mL + mH ) / 2 ; 
          mD = mH - mL ;
        }

        internal double GetDistance( double aV )
        {
          double lDist = Math.Abs(aV - mM) ; 
          double lScaled = lDist / mD ;  

          return lScaled ;  
        }

        public override string ToString() => $"[{mL}|{mH}]";

        double mL ;
        double mH ;
        double mM ;
        double mD ;
      }

      internal (Cluster,Cluster) GetClusters( IEnumerable<DPoint> aDistribution )
      {
        Cluster rClusterZero = null;
        Cluster rClusterOne  = null ;

        var lPeaks = ExtremePointsFinder.Find(aDistribution);

        if ( lPeaks.Count >= 2 )
        {
          var lAllDurations = aDistribution.Select( p => p.X.Value ) ;
          var lMinDuration  = lAllDurations.Min() ;
          var lMaxDuration  = lAllDurations.Max() ;  

          var lPeakA = lPeaks[0] ;
          var lPeakB = lPeaks[1] ;
          
          var lPeakADuration = lPeakA.Value.X.Value ;
          var lPeakBDuration = lPeakB.Value.X.Value ;

          var lPeakZero = lPeakADuration < lPeakBDuration ? lPeakA : lPeakB ;
          var lPeakOne  = lPeakADuration < lPeakBDuration ? lPeakB : lPeakA ;

          double lZeroCenter = lPeakZero.Value.X.Value ;
          double lOneCenter  = lPeakOne .Value.X.Value ;

          double lMidPoint = ( lZeroCenter + lOneCenter ) / 2 ;

          double lZeroL = MathX.LERP(lMinDuration, lZeroCenter, 0.25) ;

          rClusterZero = new Cluster( lZeroL, lMidPoint );
          rClusterOne  = new Cluster( lMidPoint, lMaxDuration );

          Context.WriteLine($"Zero cluster: {rClusterZero}" ) ; 
          Context.WriteLine($"One  cluster: {rClusterOne}" ) ; 
        }

        return ( rClusterZero, rClusterOne );
      }


      internal List<ClassifiedPulse> Estimate()
      {
         var lPulses = mInput.GetSymbols<PulseSymbol>() ;
         var lSamples = lPulses.ConvertAll( s => s.ToSample() ) ;
         
         var lDist = new Distribution(lSamples) ;

         var lFullRangeHistogram = new Histogram(lDist).Table ;

         Context.WriteLine("Durations distribution...");
         Context.Indent();
         lFullRangeHistogram.Points.ToList().ForEach( p => Context.WriteLine($"{p}"));
         Context.Unindent();

         (Cluster lClusterZero, Cluster lClusterOne) = GetClusters( lFullRangeHistogram.Points ) ;  

         if ( lClusterZero != null && lClusterOne != null )
         {
           Context.WriteLine("Classifying...");
           Context.Indent();  
           foreach( var lPulse in lPulses )
           {  
             double lDuration = lPulse.Duration ;

             double lDistToZero = lClusterZero.GetDistance(lDuration) ; 
             double lDistToOne  = lClusterOne .GetDistance(lDuration) ; 

             Context.WriteLine($"Pulse duration: {lDuration}. Distance to cluster Zero:{lDistToZero}. Distance to cluster One:{lDistToOne}" ) ; 

             Classification lClassification = new Classification();

             if ( lDistToZero <= 1.0 && lDistToOne > 1.0 )
             {
               lClassification.ForBranchA = BitType.Zero ;

               if (  lDistToZero <= 0.75 )
                    lClassification.ForBranchB = BitType.Zero ;
               else lClassification.ForBranchB = BitType.Noise ;
             }
             else if ( lDistToOne <= 1.0 && lDistToZero > 1.0 )
             {
               lClassification.ForBranchA = BitType.One ;
               if (  lDistToOne <= 0.75 )
                    lClassification.ForBranchB = BitType.One ;
               else lClassification.ForBranchB = BitType.Noise ;
             }
             else if ( lDistToOne <= 1.0 && lDistToZero <= 1.0 )
             {
               lClassification.ForBranchA = BitType.One ;
               lClassification.ForBranchB = BitType.Zero ;
             }
             else if ( lDistToOne > 1.0 && lDistToZero > 1.0 )
             {
               lClassification.ForBranchA = BitType.Noise ;
               lClassification.ForBranchB = BitType.Noise ;
             }

             Context.WriteLine($"  Classification: {lClassification}" ) ; 

             mClassifiedPulses.Add( new ClassifiedPulse(){ Pulse = lPulse, Classification = lClassification } ) ;
           }
           Context.Unindent();  
         }

         return mClassifiedPulses ;
      }

      LexicalSignal         mInput ;
      List<ClassifiedPulse> mClassifiedPulses = new List<ClassifiedPulse> ();  
    }

    static public void PlotBits( LexicalSignal aSignal, string aLabel )
    {
      List<BitSymbol> lBits = aSignal.GetSymbols<BitSymbol>();  

      if ( lBits.Count > 0 ) 
      { 
        List<float> lSamples = new List<float> ();
        lBits.ForEach( b => b.View.DumpSamples(lSamples ) );
        int lSamplingRate = lBits[0].View.SamplingRate;
        DiscreteSignal lWaveRep = new DiscreteSignal(lSamplingRate, lSamples);
        WaveSignal lWave = new WaveSignal(lWaveRep);
        lWave.SaveTo( Context.Session.LogFile( "Bits_" + aLabel + ".wav") ) ;
      }
    }

    protected override void Process (LexicalSignal aInput, Branch aInputBranch, List<Branch> rOutput )
    {
       Context.WriteLine("Binarizing Pulses by Duration");
       Context.Indent();

       var lClassifiedPulses = Classifier.Run(aInput);

       FilterBranch lBranchA = new FilterBranch( BranchName.BranchA );   
       FilterBranch lBranchB = new FilterBranch( BranchName.BranchB );   

       foreach( var lCPulse in lClassifiedPulses ) 
       {
         lBranchA.Add( lCPulse ) ;
         lBranchB.Add( lCPulse ) ;
       }

       LexicalSignal lSignalA = lBranchA.GetSignal() ;
       LexicalSignal lSignalB = lBranchB.GetSignal() ;

       if ( Context.Session.Args.GetBool("Plot") )
       {
         PlotBits(lSignalA, lBranchA.Label);
         PlotBits(lSignalB, lBranchB.Label);
       }

       rOutput.Add( new Branch(aInputBranch, lSignalA, lBranchA.Label) ) ;
       rOutput.Add( new Branch(aInputBranch, lSignalB, lBranchB.Label) ) ;

       Context.Unindent();  
    }

    protected override string Name => "BinarizeByDuration" ;

  }

}
