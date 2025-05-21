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
using NWaves.Signals.Builders;

namespace DIGITC2_ENGINE
{

  public abstract class MockWaveSource_FromBits : WaveSource
  {
    public class BaseParams
    {
      public string Text ;
      public int    BitsPerByte  = 8;
      public bool   LittleEndian = true;
      public string CharSet      = "us-ascii";
    }

    protected MockWaveSource_FromBits( BaseParams aParams ) 
    {
      mBaseParams = aParams ;
    }
     
    protected override Signal DoCreateSignal()
    {
      if ( mSignal == null ) 
      {
        var lChars = TextToChars(mBaseParams.Text);
        var lBytes = CharsToBytes(lChars); 
        var lBits  = BytesToBits(lBytes);

        var lWave = ModulateBits(lBits);

        lWave.NormalizeMaxWithPeak();

        string lName = "MockWaveSource_FromBits" ;

        string lWaveFile = DContext.Session.LogFile($"{lName}.wav");  

        lWave.SaveTo(lWaveFile);  

        mSignal        = new WaveSignal(lWave);
        mSignal.Name   = lName;
        mSignal.Origin = lWaveFile; 
      }

      return mSignal ;
    }

    char[] TextToChars( string aText )
    {
      return aText.ToCharArray() ;  
    }

    List<byte> CharsToBytes( char[] aChars) 
    {
      Encoding lEncoding = Encoding.GetEncoding( mBaseParams.CharSet);
      List<byte> rBytes = new List<byte>();
      char[] lBuffer = new char[1];
      foreach( char lChar in aChars )
      {
        lBuffer[0]=lChar;
        rBytes.AddRange( lEncoding.GetBytes(lBuffer) ) ;
      }
      return rBytes ;
    }

    List<bool> BytesToBits( List<byte> aBytes )
    {
      List<bool> rBits = new List<bool>();

      byte[] lBuffer = new byte[1];

      foreach( byte lByte in aBytes ) 
      {
        lBuffer[0]=lByte; 

        BitArray lBA = new BitArray( lBuffer ); 

        int lC = lBA.Length;

        if ( mBaseParams.LittleEndian )
        {
          for ( int i = lC - 1 ; i >= 0 ; -- i ) 
            rBits.Add(lBA[i]);
   
        }
        else
        {
          for ( int i = 0; i < lC; ++ i ) 
            rBits.Add(lBA[i]);
        }
      }

      return rBits ;

    }

    protected abstract DiscreteSignal ModulateBits( List<bool> aBits ) ;

    readonly BaseParams mBaseParams ;

  }
}
