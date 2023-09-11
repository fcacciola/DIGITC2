using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Audio;
using NWaves.Signals;

namespace DIGITC2
{
  public abstract class Source
  {
    public Signal CreateSignal() 
    {
      var rSignal = DoCreateSignal();
      rSignal.Source = this;  
      return rSignal;
    }

    public virtual List<Signal> Slice( Signal aSignal, Context aContext = null  ) {  return new List<Signal>(){aSignal} ; }  

    public virtual Signal Merge( List<Signal> aList, Context aContext = null  ) { return aList[0]; }

    public abstract Signal DoCreateSignal() ;

    public virtual void Render( TextRenderer aTextRenderer, RenderOptions aOptions ) { }
  }

  public class WaveSource : Source
  {
    public WaveSource( string aFilename ) 
    {
      mFilename = aFilename;
    }

    public override void Render( TextRenderer aTextRenderer, RenderOptions aOptions ) 
    { 
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

    public override List<Signal> Slice( Signal aSignal, Context aContext = null ) 
    { 
      WaveSignal lSignal = aSignal as WaveSignal; 
      if ( lSignal == null )
         aContext.Throw( new ArgumentException("Signal cannot be null") ) ;
        
      if ( aContext ==  null ) 
         aContext.Throw( new ArgumentException("Context cannot be null") ) ;

      List<Signal> rList = new List<Signal> ();

      if ( aContext.WindowSizeInSeconds > 0 )
      {
        int lOriginalLength = lSignal.Samples.Length;
        int lSegmentLength  = (int)(lSignal.SamplingRate * aContext.WindowSizeInSeconds);

        int k = 0 ;
        do
        {
          float[] lSegmentSamples = new float[lSegmentLength];  
          for (int i = 0; i < lSegmentLength && k < lOriginalLength ; i++, k++)
            lSegmentSamples[i] = lSignal.Samples[k];  

          var lSlice = new WaveSignal( new DiscreteSignal(lSignal.SamplingRate,lSegmentSamples) );
          lSlice.Assign( lSignal );
          lSlice.SliceIdx = rList.Count;

          rList.Add (lSlice);
        }
        while ( k < lOriginalLength );  
      }
      else
      {
        rList.Add(lSignal);
      }

      return rList;    
    }  

    public override Signal Merge( List<Signal> aSlices, Context aContext = null ) 
    { 
      if ( aSlices.Count == 0 ) return null ; 

      List<float> lAllSamples = new List<float> ();
      foreach( WaveSignal aSegment in aSlices ) 
        lAllSamples.AddRange(aSegment.Samples);

      WaveSignal lFirst = aSlices[0] as WaveSignal;

      Signal rS = new WaveSignal ( new DiscreteSignal(lFirst.SamplingRate, lAllSamples) ); 

      rS.Assign( lFirst );

      return rS;  
    }

    readonly string mFilename ;

    Signal mSignal ; 
  }
}
