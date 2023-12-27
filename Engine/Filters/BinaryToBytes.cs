using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Operations;
using NWaves.Signals;

namespace DIGITC2
{
  public class BinaryToBytes : LexicalFilter
  {
    public BinaryToBytes() : base() 
    { 
    }

    protected override void Process (LexicalSignal aInput, Branch aInputBranch, List<Branch> rOutput )
    {
      Process(true , 5, aInput, rOutput);
      Process(true , 8, aInput, rOutput);
      Process(false, 5, aInput, rOutput);
      Process(false, 8, aInput, rOutput);
    }

    void Process ( bool aLittleEndian, int aBitsPerByte, LexicalSignal aInput, List<Branch> rOutput )
    {
      mBitValues  = new List<bool>(); 

      var lSymbols = aInput.GetSymbols<BitSymbol>() ;

      int lLen = aInput.Length ;
      int lByteCount = 0; 
      int i = 0;

      do
      { 
        mDEBUG_ByteString = "" ;

        int lRem = lLen - i ;

        if ( aLittleEndian )
          AddPadding();

        for ( int j = 0 ; i < lLen && j < aBitsPerByte ; j++, i ++ )
        {
          mDEBUG_ByteString += $"[{(lSymbols[i].One ? "1" : "0")}]";
          mBitValues.Add( lSymbols[i].One ) ;  
        }
        
        AddRemainder(lRem);

        if ( !aLittleEndian )
          AddPadding();

        lByteCount ++ ;
      }
      while ( i < lLen ) ;

      BitArray lBits = new BitArray(mBitValues.ToArray());
        
      byte[] lBytes = new byte[lByteCount]; 
      lBits.CopyTo( lBytes, 0 ) ;

      List<ByteSymbol> lByteSymbols = new List<ByteSymbol>();

      foreach( byte lByte in lBytes )
        lByteSymbols.Add( new ByteSymbol(lByteSymbols.Count, lByte ) ) ;

      rOutput.Add( new Branch( new LexicalSignal(lByteSymbols), $"{(aLittleEndian ? "LittleEndian" : "BigEndian")}|{aBitsPerByte} BitsPerByte") ) ;
    }

    protected override string Name => "BinaryToBytes" ;

    void AddPadding()
    {
      if (  mBitsPerByte < 8 )
      {
        for( int k = mBitsPerByte ; k < 8 ; k++ ) 
        {
          mDEBUG_ByteString += $"[P:0]";
          mBitValues.Add( false ) ;
        }

      }
    }

    void AddRemainder( int aRem )
    {
      if ( aRem < mBitsPerByte )
      {
        for( int k = aRem ; k < mBitsPerByte ; k++ ) 
        {
          mDEBUG_ByteString += $"[R:0]";
          mBitValues.Add( false ) ;
        }
      }
    }

    List<bool> mBitValues ;

    string mDEBUG_ByteString ;

  }

}
