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

    protected override void OnSetup()
    {
      mBitSize       = Params.GetInt("BitSize");
      mQuitThreshold = Params.GetInt("QuitThreshold");
      mFitnessMap    = new FitnessMap(Params.Get("FitnessMap"));
    }

    protected override Packet Process()
    {
      WriteLine($"{mBitSize} Bits per Byte");

      var lBags = LexicalInput.GetSymbols<BitBagSymbol>() ;

      byte[] lBitValues = new byte[8];

      List<ByteSymbol> lByteSymbols = new List<ByteSymbol>();

      double lStrength = 0 ;

      foreach( var lBag in lBags )
      {
        double lBitsLikelihood = 0 ;

        int lLen = lBag.Bits.Count ;

        int i = 0 ;
        for ( ; i < lLen && i < mBitSize ; i ++ )
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

      WriteLine($"Bytes: {string.Join(", ", lByteSymbols.ConvertAll( b => b.Meaning) ) }" ) ;

      double lSNR = lStrength / (double)lByteSymbols.Count ;
      
      int lLikelihood = (int)Math.Ceiling(lSNR * 100) ; 

      Fitness lFitness = mFitnessMap.Map(lLikelihood) ;

      Score lScore = new Score(Name, lLikelihood, lFitness) ;

      WriteLine($"Good Bytes SNR: {lSNR}");
      WriteLine($"Score: {lScore}");
      WriteLine($"Likelihood: {lLikelihood}");
      WriteLine($"Fitness: {lFitness}");

      return CreateOutput( new LexicalSignal(lByteSymbols), $"{mBitSize}_BitsPerByte", lScore, lLikelihood < mQuitThreshold ) ;
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
    }

    public override string Name => this.GetType().Name ;

    int        mBitSize ;
    FitnessMap mFitnessMap ;
    int        mQuitThreshold ;
  }

}
