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
      mPipelineSelection = new PipelineSelection(DContext.Session.Args.Get("BinaryToBytes_Pipelines"));
    }

    protected override void Process (LexicalSignal aInput, Packet aInputPacket, List<Packet> rOutput )
    {
      if ( mPipelineSelection.IsActive("5") )
        Process(5, aInput, aInputPacket, rOutput);

      if ( mPipelineSelection.IsActive("8") )
        Process( 8, aInput, aInputPacket, rOutput);
    }


    // NOTE: Windows uses LSB, so we CANNOT use something like BitArray.
    // Must do this manually.
    byte ToByte_MSB( byte[] aBits )
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
      var lSymbols = aInput.GetSymbols<BitSymbol>() ;

      int lLen = lSymbols.Count ;

      List<byte> lBytes = new List<byte>(); 

      int i = 0;

      byte[] lBitValues = new byte[8];

      do
      { 
        int lRem = lLen - i ;

        int j = 0 ;
        for ( int k = aBitsPerByte ; k < 8 ; k ++ , j ++ )
          lBitValues[j] = 0 ;  

        for ( ; i < lLen && j < aBitsPerByte ; j++, i ++ )
          lBitValues[j] = (byte)( lSymbols[i].One ? 1 : 0 ) ;  

        if ( lRem < aBitsPerByte )
        {
          for( int k = lRem ; k < aBitsPerByte ; k++ ) 
            lBitValues[j] = 0 ;  
        }

        lBytes.Add( ToByte_MSB(lBitValues) ) ;
      }
      while ( i < lLen ) ;

      List<ByteSymbol> lByteSymbols = new List<ByteSymbol>();

      foreach( byte lByte in lBytes )
        lByteSymbols.Add( new ByteSymbol(lByteSymbols.Count, lByte ) ) ;

      rOutput.Add( new Packet(aInputPacket, new LexicalSignal(lByteSymbols), $"{aBitsPerByte}_BitsPerByte") ) ;
    }

    public override string Name => this.GetType().Name ;

    PipelineSelection mPipelineSelection ;
  }

}
