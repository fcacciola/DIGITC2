using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Audio;
using NWaves.Signals;

namespace DIGITC2
{
  using GatedLexicalSignal = GenericLexicalSignal<GatedSymbol>;
  using BitsSignal  = GenericLexicalSignal<BitSymbol>;
  using BytesSignal = GenericLexicalSignal<ByteSymbol>;
  using TextSignal  = GenericLexicalSignal<TextSymbol>;

  public abstract class LexicalSource<SYM> : Source
  {
    protected List<SYM> mSymbols = new List<SYM>();

  }

  public class BytesSource : LexicalSource<ByteSymbol>
  {
    public static BytesSource FromText( string aText, string aCharSet)
    {
      Encoding lEncoding = Encoding.GetEncoding(aCharSet);

      BytesSource rSource = new BytesSource();

      rSource.mBytes = lEncoding.GetBytes(aText);

      return rSource; 
    }

    public override Signal DoCreateSignal() 
    {
      mSymbols.Clear(); 
      foreach( byte lByte in mBytes )
      {
        ByteSymbol lBS = new ByteSymbol(mSymbols.Count(),lByte); 
        mSymbols.Add(lBS);

      }
      BytesSignal rSignal = new BytesSignal(mSymbols);
      return rSignal; 
    }

    byte[] mBytes ;
  }

  public class BitsSource : LexicalSource<BitSymbol>
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

    public static BitsSource FromText( string aText, string aCharSet)
    {
      Encoding lEncoding = Encoding.GetEncoding(aCharSet);

      BitsSource rSource = new BitsSource();

      byte[] lBytes = lEncoding.GetBytes(aText);
       
      return FromBytes( lBytes );
    }

    public override Signal DoCreateSignal() 
    {
      mSymbols.Clear(); 
      foreach( bool lBit in mBits )
      {
        BitSymbol lBS = new BitSymbol(mSymbols.Count(),lBit,null); 
        mSymbols.Add(lBS);

      }
      BitsSignal rSignal = new BitsSignal(mSymbols);
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

    public override Signal DoCreateSignal()
    {
      if ( mSignal == null ) 
      {
      }

      return mSignal ;
    }

    readonly Params mParams ;

  }
}
