using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DocumentFormat.OpenXml.Drawing;

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

        mBits.Add( new BitSymbol( mBits.Count, aIsOne, lView )) ;
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
        internal Cluster( double aCenter, double aSize )
        {
          mCenter = aCenter ;
          mSize   = aSize ; 
        }

        internal double GetDistance( double aV )
        {
          double lDist = Math.Abs(aV - mCenter) ; 
          double lScaled = lDist / mSize ;  

          return lScaled ;  
        }

        double mCenter ;
        double mSize   ;
      }

      internal (Cluster,Cluster) GetClusters( List<double> aDurations )
      {
        double lMinD = aDurations.Min();
        double lMaxD = aDurations.Max();

        double lRange = lMaxD - lMinD ; 

        double lZeroCenter = lMinD + lRange * .20  ; 
        double lOneCenter  = lMinD + lRange * .80  ; 

        double lSize = lRange * .40 ;

        Cluster rClusterZero = new Cluster( lZeroCenter, lSize );
        Cluster rClusterOne  = new Cluster( lOneCenter , lSize );

        return ( rClusterZero, rClusterOne );
      }


      internal List<ClassifiedPulse> Estimate()
      {
         var lPulses = mInput.GetSymbols<PulseSymbol>() ;
         var lDurations = lPulses.ConvertAll( p => p.Duration ) ;

         (Cluster lClusterZero, Cluster lClusterOne) = GetClusters( lDurations ) ;  

         foreach( var lPulse in lPulses )
         {  
           double lDuration = lPulse.Duration ;

           double lDistToZero = lClusterZero.GetDistance(lDuration) ; 
           double lDistToOne  = lClusterOne .GetDistance(lDuration) ; 

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

           mClassifiedPulses.Add( new ClassifiedPulse(){ Pulse = lPulse, Classification = lClassification } ) ;
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
    }

    protected override string Name => "BinarizeByDuration" ;

  }

}
