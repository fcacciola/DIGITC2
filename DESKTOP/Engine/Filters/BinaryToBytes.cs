using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Operations;
using NWaves.Signals;

namespace ENGINE
{
  public class BinaryToBytes : LexicalFilter
  {
    public BinaryToBytes() : base() 
    { 
    }

    protected override void OnSetup()
    {
      mBitSize       = Params.GetInt("BitSize");
      mQuitThreshold = Params.GetDouble("QuitThreshold");
    }

    protected override Packet Process()
    {
      WriteLine2GUI("Converting Bits to Bytes...");
      Indent();

      WriteLine($"{mBitSize} Bits per Byte");

      var lBags = LexicalInput.GetSymbols<BitBagSymbol>() ;

      byte[] lBitValues = new byte[8];

      List<ByteSymbol> lByteSymbols = new List<ByteSymbol>();

      double lStrength = 0 ;

      foreach( var lBag in lBags )
      {
        double lBitsCoverage = 0 ;

        int lLen = lBag.Bits.Count ;

        int i = 0 ;
        for ( ; i < lLen && i < mBitSize ; i ++ )
        {
          BitSymbol lBit = lBag.Bits[i] ; 
          lBitsCoverage += lBit.Coverage ;
          lBitValues[i] = (byte)( lBit.Value ) ;  
        }

        // Complete to Octet if aBitsPerByte < 8

        for( int k = i ; k < 8 ; k++ ) 
        {
          lBitValues[k] = 0 ;  
          lBitsCoverage += 1 ;
        }

        double lByteCoverage   = lBitsCoverage / mBitSize ;

        lStrength += lByteCoverage ;

        var lByte = ToByte_MSB_Last(lBitValues);

        lByteSymbols.Add( new ByteSymbol(lByteSymbols.Count, lByte, lByteCoverage, lBag.SamplePos ) ) ;
      }

      WriteLine($"Bytes: {string.Join(", ", lByteSymbols.ConvertAll( b => b.ToString()) ) }" ) ;

      double lCoverage = lByteSymbols.Count > 0 ?  lStrength / (double)lByteSymbols.Count : 0 ;
      
      Score lScore = new Score(Name, lCoverage, true) ;

      WriteDetailLine($"Good Bytes Coverage: {lCoverage}");
      WriteDetailLine($"Score: {lScore}");
      Unindent();

      return CreateOutput( new LexicalSignal(lByteSymbols), $"{mBitSize}_BitsPerByte", lScore, lCoverage < mQuitThreshold ) ;
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
     
    public override string Name => this.GetType().Name ;

    int mBitSize ;
    double mQuitThreshold ;
  }

}
