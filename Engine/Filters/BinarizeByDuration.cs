using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    enum LayerClass {  ClassA, ClassB ,ClassC } ;

    class Layer
    {
      internal Layer ( LayerClass aClass )
      {
        mClass = aClass ;
      }

      internal void Add( ClassifiedPulse aCPulse )
      {
        var lBitType = aCPulse.Classification.GetBitType(mClass);

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

      internal string Label => mClass.ToString();

      LayerClass      mClass ;
      List<BitSymbol> mBits = new List<BitSymbol> ();
    }
    
    class Classification
    {
      internal BitType GetBitType( LayerClass aLayer )
      {
        return aLayer == LayerClass.ClassA ? A : ( aLayer == LayerClass.ClassB ? B : C ) ;
      }

      internal BitType A ;
      internal BitType B ;
      internal BitType C ;
    }

    class ClassifiedPulse
    {
      internal PulseSymbol    Pulse ;
      internal Classification Classification ;
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

      internal List<ClassifiedPulse> Estimate()
      {
         var lPulses = mInput.GetSymbols<PulseSymbol>() ;
         (DTable lHistogram, DTable lRankSize) = PulseFilterHelper.GetHistogramAndRankSize(lPulses) ;

         lPulses.ForEach( p => mClasses.Add( Estimate(p) ) );

         return mClasses ;
      }

      internal ClassifiedPulse Estimate( PulseSymbol aPulse )  
      {
        ClassifiedPulse rClass = new ClassifiedPulse(){Pulse = aPulse};
        return rClass ;
      }

      LexicalSignal mInput ;
      List<ClassifiedPulse> mClasses = new List<ClassifiedPulse> ();  
    }

    protected override void Process (LexicalSignal aInput, Branch aInputBranch, List<Branch> rOutput )
    {
       var lClassifiedPulses = Classifier.Run(aInput);

       Layer lLayerA = new Layer( LayerClass.ClassA );   
       Layer lLayerB = new Layer( LayerClass.ClassB );   
       Layer lLayerC = new Layer( LayerClass.ClassC );   

       foreach( var lCPulse in lClassifiedPulses ) 
       {
         lLayerA.Add( lCPulse ) ;
         lLayerB.Add( lCPulse ) ;
         lLayerC.Add( lCPulse ) ;
       }

       rOutput.Add( new Branch(lLayerA.GetSignal(), lLayerA.Label) ) ;
       rOutput.Add( new Branch(lLayerB.GetSignal(), lLayerA.Label) ) ;
       rOutput.Add( new Branch(lLayerC.GetSignal(), lLayerA.Label) ) ;

    }

    protected override string Name => "BinarizeByDuration" ;

  }

}
