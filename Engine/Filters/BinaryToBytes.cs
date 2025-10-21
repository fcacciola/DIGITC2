using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Operations;
using NWaves.Signals;

namespace DIGITC2_ENGINE
{
  public class BinaryToBytes : LexicalFilter
  {
    public BinaryToBytes() : base() 
    { 
    }

    public override void Setup()
    {
      mPipelineSelection = new PipelineSelection(DContext.Session.Args.Get(Name,"Pipelines"));

      mFitnessMap = new FitnessMap(DContext.Session.Args.Get(Name,"FitnessMap"));

      mQuitThreshold = DContext.Session.Args.GetOptionalInt(Name, "QuitThreshold").GetValueOrDefault(1);
    }

    protected override void Process (LexicalSignal aInput, Packet aInputPacket, List<Packet> rOutput )
    {
      DContext.WriteLine("Collecting Bits into Bytes");
      DContext.Indent();

      if ( mPipelineSelection.IsActive("5bits") )
        Process( 5, aInput, aInputPacket, rOutput);

      if ( mPipelineSelection.IsActive("8bits") )
        Process( 8, aInput, aInputPacket, rOutput);

      DContext.Unindent();
    }


    // NOTE: Windows uses LSB, so we CANNOT use something like BitArray.
    // Must do this manually.
    byte ToByte_MSB_Last( byte[] aBits )
    {
      byte rByte = 0;
      for (int i = 0; i < 8; i++)
      {
        rByte <<= 1;             // shift left by 1
        rByte |= aBits[i] ;  // add the next bit
      }

      return rByte;
    }

    void Process ( int aBitsPerByte, LexicalSignal aInput, Packet aInputPacket, List<Packet> rOutput )
    {
      DContext.WriteLine($"{aBitsPerByte} Bits per Byte");

      var lBags = aInput.GetSymbols<BitBagSymbol>() ;

      byte[] lBitValues = new byte[8];

      List<ByteSymbol> lByteSymbols = new List<ByteSymbol>();

      double lStrength = 0 ;

      foreach( var lBag in lBags)
      {
        double lBitsLikelihood = 0 ;

        int lLen = lBag.Bits.Count ;

        int i = 0 ;
        for ( ; i < lLen && i < aBitsPerByte ; i ++ )
        {
          BitSymbol lBit = lBag.Bits[i] ; 
          lBitsLikelihood += lBit.Likelihood ;
          lBitValues[i] = (byte)( lBit.Value ) ;  
        }

        // Complete to Octet if aBitsPerByte < 8

        for( int k = i ; k < 8 ; k++ ) 
        {
          lBitValues[k] = 0 ;  
          lBitsLikelihood += 1 ;
        }

        double lByteLikelihood = lBitsLikelihood / 8 ;

        lStrength += lByteLikelihood ;

        var lByte = ToByte_MSB_Last(lBitValues);

        lByteSymbols.Add( new ByteSymbol(lByteSymbols.Count, lByte, lByteLikelihood ) ) ;
      }


      DContext.WriteLine($"Bytes: {string.Join(", ", lByteSymbols.ConvertAll( b => b.Meaning) ) }" ) ;

      double lSNR = lStrength / (double)lByteSymbols.Count ;
      
      int lLikelihood = (int)Math.Ceiling(lSNR * 100) ; 

      Fitness lFitness = mFitnessMap.Map(lLikelihood) ;

      Score lScore = new Score(Name, lLikelihood, lFitness) ;

      DContext.WriteLine($"Good Bytes SNR: {lSNR}");
      DContext.WriteLine($"Score: {lScore}");
      DContext.WriteLine($"Likelihood: {lLikelihood}");
      DContext.WriteLine($"Fitness: {lFitness}");

      rOutput.Add( new Packet(Name, aInputPacket, new LexicalSignal(lByteSymbols), $"{aBitsPerByte}_BitsPerByte", lScore, lLikelihood < mQuitThreshold ) ) ;
    }

    public override string Name => this.GetType().Name ;

    PipelineSelection mPipelineSelection ;
    FitnessMap        mFitnessMap ;
    int               mQuitThreshold ;
  }

}
