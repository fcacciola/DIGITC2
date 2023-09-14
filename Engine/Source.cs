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
  using GatedLexicalSignal = GenericLexicalSignal<GatedSymbol>;
  using BitsSignal         = GenericLexicalSignal<BitSymbol>;
  using BytesSignal        = GenericLexicalSignal<ByteSymbol>;
  using TextSignal         = GenericLexicalSignal<TextSymbol>;

  public abstract class Source
  {
    public Signal CreateSignal() 
    {
      var rSignal = DoCreateSignal();
      rSignal.Source = this;  
      return rSignal;
    }

    public abstract Signal DoCreateSignal() ;

    public virtual void Render( TextRenderer aTextRenderer, RenderOptions aOptions ) { }
  }
  
  public class DirectCopyingSource : Source
  {
    public DirectCopyingSource( Signal aSource )
    {
      mSource = aSource ;
    }

    public override Signal DoCreateSignal() => mSource.Copy();

    Signal mSource ;  
  }

  public abstract class WaveSource : Source
  {
    public override void Render( TextRenderer aTextRenderer, RenderOptions aOptions ) 
    { 
    }

    protected Signal mSignal ; 
  }

  public class WaveFileSource : WaveSource
  {
    public WaveFileSource( string aFilename ) 
    {
      mFilename = aFilename;
    }

    public override Signal DoCreateSignal()
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
