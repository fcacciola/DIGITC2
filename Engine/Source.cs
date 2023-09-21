using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using NWaves.Audio;
using NWaves.Signals;


namespace DIGITC2
{
  using GatedLexicalSignal = LexicalSignal<GatedSymbol>;
  using BitsSignal         = LexicalSignal<BitSymbol>;
  using BytesSignal        = LexicalSignal<ByteSymbol>;
  using TextSignal         = LexicalSignal<WordSymbol>;

  public abstract class Source
  {
    public Signal CreateSignal() 
    {
      var rSignal = DoCreateSignal();
      rSignal.Source = this;  
      return rSignal;
    }

    protected abstract Signal DoCreateSignal() ;
  }
  
  public class DirectCopyingSource : Source
  {
    public DirectCopyingSource( Signal aSource )
    {
      mSource = aSource ;
    }

    public static DirectCopyingSource From ( Signal aSignal ) { return new DirectCopyingSource ( aSignal ) ; }

    public static DirectCopyingSource From ( Result aResult ) { return new DirectCopyingSource ( aResult.Steps.Last() ) ; }
    
    protected override Signal DoCreateSignal() => mSource.Copy();

    Signal mSource ;  
  }

  public abstract class WaveSource : Source
  {
    protected Signal mSignal ; 
  }

  public class WaveFileSource : WaveSource
  {
    public WaveFileSource( string aFilename ) 
    {
      mFilename = aFilename;
    }

    protected override Signal DoCreateSignal()
    {
      if ( mSignal == null ) 
      {
        using (var stream = new FileStream(mFilename, FileMode.Open))
        {
          var waveContainer = new WaveFile(stream);
          mSignal = new WaveSignal(waveContainer[Channels.Average]);
          mSignal.Name = $"({mFilename})"; 
        }
      }

      return mSignal ;
    }

    readonly string mFilename ;
  }

}
