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
    public BinaryToBytes( int aBitsPerByte, bool aLittleEndian = true ) : base() { mLittleEndian = aLittleEndian ; mBitsPerByte = aBitsPerByte ; }

    protected override Step Process ( LexicalSignal aInput, Step aStep )
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

        if ( mLittleEndian )
          AddPadding();

        for ( int j = 0 ; i < lLen && j < mBitsPerByte ; j++, i ++ )
        {
          mDEBUG_ByteString += $"[{(lSymbols[i].One ? "1" : "0")}]";
          mBitValues.Add( lSymbols[i].One ) ;  
        }
        
        AddRemainder(lRem);

        if ( !mLittleEndian )
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

      mStep = aStep.Next( new LexicalSignal(lByteSymbols), "Bytes", this) ;

      return mStep ;
    }

    public override string ToString() => $"BinaryToBytes(BitsPerByte:{mBitsPerByte}, LittleEndian:{mLittleEndian})";

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
    bool mLittleEndian ;
    int  mBitsPerByte ;

    List<bool> mBitValues ;

    string mDEBUG_ByteString ;

  }

}
