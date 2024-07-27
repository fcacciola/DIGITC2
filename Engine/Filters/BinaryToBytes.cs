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
      mBranchSelection = new Branch.Selection(Context.Session.Args.Get("BinaryToBytes_Branches"));
    }

    protected override void Process (LexicalSignal aInput, Branch aInputBranch, List<Branch> rOutput )
    {
      if ( mBranchSelection.IsActive("Little5") )
        Process(true , 5, aInput, aInputBranch, rOutput);

      if ( mBranchSelection.IsActive("Little8") )
        Process(true , 8, aInput, aInputBranch, rOutput);

      if ( mBranchSelection.IsActive("Big5") )
        Process(false, 5, aInput, aInputBranch, rOutput);

      if ( mBranchSelection.IsActive("Big85") )
        Process(false, 8, aInput, aInputBranch, rOutput);
    }

    void Process ( bool aLittleEndian, int aBitsPerByte, LexicalSignal aInput, Branch aInputBranch, List<Branch> rOutput )
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
          AddPadding(aBitsPerByte);

        for ( int j = 0 ; i < lLen && j < aBitsPerByte ; j++, i ++ )
        {
          mDEBUG_ByteString += $"[{(lSymbols[i].One ? "1" : "0")}]";
          mBitValues.Add( lSymbols[i].One ) ;  
        }
        
        AddRemainder(aBitsPerByte, lRem);

        if ( !aLittleEndian )
          AddPadding(aBitsPerByte);

        lByteCount ++ ;
      }
      while ( i < lLen ) ;

      BitArray lBits = new BitArray(mBitValues.ToArray());
        
      byte[] lBytes = new byte[lByteCount]; 
      lBits.CopyTo( lBytes, 0 ) ;

      List<ByteSymbol> lByteSymbols = new List<ByteSymbol>();

      foreach( byte lByte in lBytes )
        lByteSymbols.Add( new ByteSymbol(lByteSymbols.Count, lByte ) ) ;

      rOutput.Add( new Branch(aInputBranch, new LexicalSignal(lByteSymbols), $"{(aLittleEndian ? "LittleEndian" : "BigEndian")}|{aBitsPerByte} BitsPerByte") ) ;
    }

    protected override string Name => "BinaryToBytes" ;

    void AddPadding( int aBitsPerByte )
    {
      if ( aBitsPerByte < 8 )
      {
        for( int k = aBitsPerByte ; k < 8 ; k++ ) 
        {
          mDEBUG_ByteString += $"[P:0]";
          mBitValues.Add( false ) ;
        }
      }
    }

    void AddRemainder( int aBitsPerByte, int aRem )
    {
      if ( aRem < aBitsPerByte )
      {
        for( int k = aRem ; k < aBitsPerByte ; k++ ) 
        {
          mDEBUG_ByteString += $"[R:0]";
          mBitValues.Add( false ) ;
        }
      }
    }

    List<bool> mBitValues ;

    string mDEBUG_ByteString ;

    Branch.Selection mBranchSelection ;
  }

}
