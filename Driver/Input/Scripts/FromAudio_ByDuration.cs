using System.IO;

namespace DIGITC2 {

public class FromAudio_ByDuration 
{
  public static void Run( Args aArgs  )
  {
    string lWaveFilename = aArgs.Get("Audio" ) ;

    Context.Setup( new Session("FromAudio_" +  Path.GetFileNameWithoutExtension(lWaveFilename), aArgs) ) ;

    if ( File.Exists( lWaveFilename ) )
    {
      var lSource = new WaveFileSource(lWaveFilename) ;

      Context.WriteLine("Parameters");
      Context.WriteLine("Envelop_AttackTime              =" + aArgs.GetDouble("Envelop_AttackTime"));
      Context.WriteLine("Envelope_ReleaseTime            =" + aArgs.GetDouble("Envelope_ReleaseTime"));
      Context.WriteLine("AmplitudeGate_Threshold         =" + aArgs.GetDouble("AmplitudeGate_Threshold"));
      Context.WriteLine("ExtractGatedlSymbols_MinDuration=" + aArgs.GetDouble("ExtractGatedlSymbols_MinDuration"));
      Context.WriteLine("ExtractGatedlSymbols_MergeGap   =" + aArgs.GetDouble("ExtractGatedlSymbols_MergeGap"));
      Context.WriteLine("BinarizeByDuration_Threshold    =" + aArgs.GetDouble("BinarizeByDuration_Threshold"));

      Processor.FromAudioToBits_ByPulseDuration().Then( Processor.FromBits() ).Process( lSource.CreateSignal() ).Save() ;
    }
    else
    {
      Context.Error("Could not find audio file: [" + lWaveFilename + "]");
    }

    Context.Shutdown(); 
  }
}
}
