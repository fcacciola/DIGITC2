using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using NWaves.Audio;
using NWaves.Signals;
using NWaves.Utils;


namespace DIGITC2_ENGINE
{
  public abstract class Source
  {
    public Signal CreateSignal() 
    {
      var rSignal = DoCreateSignal();
      rSignal.Source = this;  
      return rSignal;
    }

    public abstract string Name { get; }

    protected abstract Signal DoCreateSignal() ;

    public override string ToString() => Name;
  }
  
  public class DirectCopyingSource : Source
  {
    public DirectCopyingSource( Signal aSource )
    {
      mSource = aSource ;
    }

    public static DirectCopyingSource From ( Signal aSignal ) { return new DirectCopyingSource ( aSignal ) ; }
   
    protected override Signal DoCreateSignal() => mSource.Copy();

    public override  string Name => mSource.Name;

    Signal mSource ;  
  }

  public abstract class WaveSource : Source
  {
    protected Signal mSignal ; 

    public static void SaveTo( DiscreteSignal aRep, string aFilename )  
    {
      using (var lStream = new FileStream(aFilename, FileMode.OpenOrCreate, FileAccess.Write))
      {
        var lWF = new WaveFile(aRep);
        lWF.SaveTo( lStream );  
      }
    }
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
          var lRep = waveContainer[Channels.Average];
          Guard.AgainstInequality(lRep.SamplingRate, X.SamplingRate, "Sampling rate");
          mSignal = new WaveSignal(lRep);
          mSignal.Name = Path.GetFileNameWithoutExtension(mFilename); 
          mSignal.Origin = mFilename; 
        }
      }

      return mSignal ;
    }

    public override  string Name => Path.GetFileNameWithoutExtension(mFilename);

    readonly string mFilename ;
  }

  public abstract class LexicalSource : Source
  {
    protected List<Symbol> mSymbols = new List<Symbol>();
  }
}
