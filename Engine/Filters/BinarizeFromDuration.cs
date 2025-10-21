using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MathNet.Numerics.Integration;
using MathNet.Numerics.Statistics;

using Newtonsoft.Json.Serialization;

using NWaves.Operations;
using NWaves.Signals;

namespace DIGITC2_ENGINE
{

  public class BinarizeFromDuration : LexicalFilter
  {
    public BinarizeFromDuration() 
    { 
    }

    enum BitType { One, Zero, Noise } ;
    enum PipelineName {  PipelineA, PipelineB } ;

    class FilterPipeline
    {
      internal FilterPipeline ( PipelineName aPipelineName )
      {
        mPipelineName = aPipelineName ;
      }

      internal void Add( ClassifiedPulse aCPulse )
      {
        var lBitType = aCPulse.Classification.GetBitType(mPipelineName);

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

      internal string Label => mPipelineName.ToString();

      PipelineName      mPipelineName ;
      List<BitSymbol> mBits = new List<BitSymbol> ();
    }
    
    class Classification
    {
      internal BitType GetBitType( PipelineName aPipelineName )
      {
        return aPipelineName == PipelineName.PipelineA ? ForPipelineA : ForPipelineB ;
      }

      public override string ToString() => $"[{ForPipelineA}|{ForPipelineB}]";
      
      internal BitType ForPipelineA ;
      internal BitType ForPipelineB ;
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

          DContext.WriteLine($"Zero cluster: {rClusterZero}" ) ; 
          DContext.WriteLine($"One  cluster: {rClusterOne}" ) ; 
        }

        return ( rClusterZero, rClusterOne );
      }


      internal List<ClassifiedPulse> Estimate()
      {
         var lPulses = mInput.GetSymbols<PulseSymbol>() ;
         var lSamples = lPulses.ConvertAll( s => s.ToSample() ) ;
         
         var lDist = new Distribution(lSamples) ;

         var lFullRangeHistogram = new Histogram(lDist).Table ;

         DContext.WriteLine("Durations distribution...");
         DContext.Indent();
         lFullRangeHistogram.Points.ToList().ForEach( p => DContext.WriteLine($"{p}"));
         DContext.Unindent();

         (Cluster lClusterZero, Cluster lClusterOne) = GetClusters( lFullRangeHistogram.Points ) ;  

         if ( lClusterZero != null && lClusterOne != null )
         {
           DContext.WriteLine("Classifying...");
           DContext.Indent();  
           foreach( var lPulse in lPulses )
           {  
             double lDuration = lPulse.Duration ;

             double lDistToZero = lClusterZero.GetDistance(lDuration) ; 
             double lDistToOne  = lClusterOne .GetDistance(lDuration) ; 

             DContext.WriteLine($"Pulse duration: {lDuration}. Distance to cluster Zero:{lDistToZero}. Distance to cluster One:{lDistToOne}" ) ; 

             Classification lClassification = new Classification();

             if ( lDistToZero <= 1.0 && lDistToOne > 1.0 )
             {
               lClassification.ForPipelineA = BitType.Zero ;

               if (  lDistToZero <= 0.75 )
                    lClassification.ForPipelineB = BitType.Zero ;
               else lClassification.ForPipelineB = BitType.Noise ;
             }
             else if ( lDistToOne <= 1.0 && lDistToZero > 1.0 )
             {
               lClassification.ForPipelineA = BitType.One ;
               if (  lDistToOne <= 0.75 )
                    lClassification.ForPipelineB = BitType.One ;
               else lClassification.ForPipelineB = BitType.Noise ;
             }
             else if ( lDistToOne <= 1.0 && lDistToZero <= 1.0 )
             {
               lClassification.ForPipelineA = BitType.One ;
               lClassification.ForPipelineB = BitType.Zero ;
             }
             else if ( lDistToOne > 1.0 && lDistToZero > 1.0 )
             {
               lClassification.ForPipelineA = BitType.Noise ;
               lClassification.ForPipelineB = BitType.Noise ;
             }

             DContext.WriteLine($"  Classification: {lClassification}" ) ; 

             mClassifiedPulses.Add( new ClassifiedPulse(){ Pulse = lPulse, Classification = lClassification } ) ;
           }
           DContext.Unindent();  
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
        DiscreteSignal lWaveRep = new DiscreteSignal(SIG.SamplingRate, lSamples);
        WaveSignal lWave = new WaveSignal(lWaveRep);
        lWave.SaveTo( DContext.Session.OutputFile( "Bits_" + aLabel + ".wav") ) ;
      }
    }

    protected override void Process (LexicalSignal aInput, Packet aInputPacket, List<Packet> rOutput )
    {
       DContext.WriteLine("Binarizing Pulses by Duration");
       DContext.Indent();

       var lClassifiedPulses = Classifier.Run(aInput);

       FilterPipeline lPipelineA = new FilterPipeline( PipelineName.PipelineA );   
       FilterPipeline lPipelineB = new FilterPipeline( PipelineName.PipelineB );   

       foreach( var lCPulse in lClassifiedPulses ) 
       {
         lPipelineA.Add( lCPulse ) ;
         lPipelineB.Add( lCPulse ) ;
       }

       LexicalSignal lSignalA = lPipelineA.GetSignal() ;
       LexicalSignal lSignalB = lPipelineB.GetSignal() ;

       if ( DContext.Session.Settings.GetBool("Plot") )
       {
         PlotBits(lSignalA, lPipelineA.Label);
         PlotBits(lSignalB, lPipelineB.Label);
       }

       rOutput.Add( new Packet(Name, aInputPacket, lSignalA, lPipelineA.Label) ) ;
       rOutput.Add( new Packet(Name, aInputPacket, lSignalB, lPipelineB.Label) ) ;

       DContext.Unindent();  
    }

    public override string Name => this.GetType().Name ;

  }

}
