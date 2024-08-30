using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using NWaves.Audio;
using NWaves.Signals;
using NWaves.Signals.Builders;

namespace DIGITC2_ENGINE
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

    public override string Name => "Bytes";  

    byte[] mBytes ;
  }

  public class BitsSource : LexicalSource
  {
    BitsSource( string aName )
    {
      mName = aName;
    }

    public static BitsSource FromBytes( string aName, IEnumerable<byte> aBytes)
    {
      BitsSource rSource = new BitsSource(aName);

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

    public static BitsSource FromRandom(string aName, int aLen )
    {
      var lRNG = RandomNumberGenerator.Create();
      byte[] lBytes = new byte[aLen]; 
      lRNG.GetBytes(lBytes);
      return FromBytes(aName, lBytes); 
    }

    public static BitsSource FromText( string aName, string aText, string aCharSet = "us-ascii")
    {
      Encoding lEncoding = Encoding.GetEncoding(aCharSet);

      byte[] lBytes = lEncoding.GetBytes(aText);
       
      return FromBytes( aName, lBytes );
    }

    protected override Signal DoCreateSignal() 
    {
      mSymbols.Clear(); 
      foreach( bool lBit in mBits )
      {
        BitSymbol lBS = new BitSymbol(mSymbols.Count(),lBit,1.0,null); 
        mSymbols.Add(lBS);

      }
      LexicalSignal rSignal = new LexicalSignal(mSymbols);
      rSignal.Name="Bits";
      return rSignal; 
    }

    public override string Name => mName;  

    string mName ;

    List<bool> mBits = new List<bool>();
  }

}
