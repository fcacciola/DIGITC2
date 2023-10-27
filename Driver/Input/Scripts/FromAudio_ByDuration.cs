namespace DIGITC2 {

public class FromAudio_ByDuration 
{
  public static void Run( Args aArgs  )
  {
    Context.Setup( new Session("FromAudio") ) ;

    string lAudioSample0 = aArgs.Get("Audio" ) ;

    if ( System.IO.File.Exists( lAudioSample0 ) )
    {
      var lSource = new WaveFileSource(lAudioSample0) ;  

      Context.WriteLine("Wave To Text Sample");

      Context.Session.Params.WindowSizeInSeconds             = aArgs.GetOptionalInt("WindowSizeInSeconds") ?? 250;
      Context.Session.Params.Envelop_AttackTime              = aArgs.GetOptionalDouble("Envelop_AttackTime") ?? 0.0;
      Context.Session.Params.Envelope_ReleaseTime            = aArgs.GetOptionalDouble("Envelope_ReleaseTime") ?? 0.0 ;
      Context.Session.Params.AmplitudeGate_Threshold         = aArgs.GetOptionalDouble("AmplitudeGate_Threshold") ?? 0.0;
      Context.Session.Params.ExtractGatedlSymbols_MinDuration= aArgs.GetOptionalDouble("ExtractGatedlSymbols_MinDuration") ?? 0.0;
      Context.Session.Params.ExtractGatedlSymbols_MergeGap   = aArgs.GetOptionalDouble("ExtractGatedlSymbols_MergeGap") ?? 0.0;  
      Context.Session.Params.BinarizeByDuration_Threshold    = aArgs.GetOptionalDouble("BinarizeByDuration_Threshold") ?? 0.0;

      Context.WriteLine("Parameters") ; 
      Context.WriteLine("Envelop_AttackTime              =" + Context.Session.Params.Envelop_AttackTime               ) ; 
      Context.WriteLine("Envelope_ReleaseTime            =" + Context.Session.Params.Envelope_ReleaseTime             ) ; 
      Context.WriteLine("AmplitudeGate_Threshold         =" + Context.Session.Params.AmplitudeGate_Threshold          ) ; 
      Context.WriteLine("ExtractGatedlSymbols_MinDuration=" + Context.Session.Params.ExtractGatedlSymbols_MinDuration ) ; 
      Context.WriteLine("ExtractGatedlSymbols_MergeGap   =" + Context.Session.Params.ExtractGatedlSymbols_MergeGap    ) ; 
      Context.WriteLine("BinarizeByDuration_Threshold    =" + Context.Session.Params.BinarizeByDuration_Threshold     ) ; 

      var lResult = Processor.FromAudioToBits_ByPulseDuration().Then( Processor.FromBits() ).Process( lSource.CreateSignal() ) ;

    }

    Context.Shutdown(); 
  }
}
}
