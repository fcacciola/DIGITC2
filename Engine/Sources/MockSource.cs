using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using NWaves.Audio;
using NWaves.Signals;

namespace DIGITC2
{
  public abstract class LexicalSource : Source
  {
    protected List<Symbol> mSymbols = new List<Symbol>();
  }

  public class BytesSource : LexicalSource
  {
    public static BytesSource FromText( string aText, string aCharSet)
    {
      Encoding lEncoding = Encoding.GetEncoding(aCharSet);

      BytesSource rSource = new BytesSource();

      rSource.mBytes = lEncoding.GetBytes(aText);
      return rSource; 
    }

    public static List<Symbol> GetWordSeparators()
    {
      List<Symbol> rS = new List<Symbol>();
      var lSeparatorSource = BytesSource.FromText(" ,;.:-!¡¿?()[]{}/$%&#@*=+\\\"'","us-ascii") ;
      var lSeparatorBytes = lSeparatorSource.CreateSignal();
      rS.AddRange((lSeparatorBytes as LexicalSignal).Symbols);
      return rS;
    }

    protected override Signal DoCreateSignal() 
    {
      mSymbols.Clear(); 
      foreach( byte lByte in mBytes )
      {
        ByteSymbol lBS = new ByteSymbol(mSymbols.Count(),lByte); 
        mSymbols.Add(lBS);

      }
      LexicalSignal rSignal = new LexicalSignal(mSymbols);
      return rSignal; 
    }

    byte[] mBytes ;
  }

  public class BitsSource : LexicalSource
  {
    public static BitsSource FromBytes( IEnumerable<byte> aBytes)
    {
      BitsSource rSource = new BitsSource();

      byte[] lInBuffer  = new byte[1];
      bool[] lOutBuffer = new bool[8];

      foreach( byte lByte in aBytes )  
      {
        lInBuffer[0] = lByte;
        new BitArray(lInBuffer).CopyTo(lOutBuffer, 0);  

        rSource.mBits.AddRange(lOutBuffer); 
      }

      return rSource; 
    }

    public static BitsSource FromRandom( int aLen )
    {
      var lRNG = RandomNumberGenerator.Create();
      byte[] lBytes = new byte[aLen]; 
      lRNG.GetBytes(lBytes);
      return FromBytes(lBytes); 
    }

    public static BitsSource FromText( string aText, string aCharSet = "us-ascii")
    {
      Encoding lEncoding = Encoding.GetEncoding(aCharSet);

      BitsSource rSource = new BitsSource();

      byte[] lBytes = lEncoding.GetBytes(aText);
       
      return FromBytes( lBytes );
    }

    protected override Signal DoCreateSignal() 
    {
      mSymbols.Clear(); 
      foreach( bool lBit in mBits )
      {
        BitSymbol lBS = new BitSymbol(mSymbols.Count(),lBit,null); 
        mSymbols.Add(lBS);

      }
      LexicalSignal rSignal = new LexicalSignal(mSymbols);
      rSignal.Name="Bits";
      return rSignal; 
    }

    List<bool> mBits = new List<bool>();
  }

  public class TextTo_Duration_base_Keying_WaveSource : WaveSource
  {
    public class Params
    {
      public string Text ;
      public double EnvelopeAttackTime ;
      public double EnvelopeReleaseTime ;
      public double AmplitudeGateThreshold ;
      public double ExtractGatedlSymbolsMinDuration ;
      public double ExtractGatedlSymbolsMergeGap ;
      public double BinarizeByDurationThreshold ;

      public int    BinaryToBytesBitsPerByte   = 8;
      public bool   BinaryToBytesLittleEndian = true;
      public string BytesToTextCharSet        = "us-ascii";
    }

    public TextTo_Duration_base_Keying_WaveSource( Params aParams ) 
    {
      mParams = aParams ;
    }

    protected override Signal DoCreateSignal()
    {
      if ( mSignal == null ) 
      {
      }

      return mSignal ;
    }

    readonly Params mParams ;

  }
}
