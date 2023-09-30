namespace DIGITC2 {

public class FromAudio_ByDuration 
{
  public static void Run( string[] aCmdLineArgs )
  {
    Context.Setup( new Session("FromAudio") ) ;

    string lAudioSample0 = aCmdLineArgs[1] ;

    if ( System.IO.File.Exists( lAudioSample0 ) )
    {
      var lSettings = new SimpleSettings(aCmdLineArgs[2]);

      var lSource = new WaveFileSource(lAudioSample0) ;  

      Context.WriteLine("Wave To Text Sample");

      Context.Session.Params.WindowSizeInSeconds             = 250 ;
      Context.Session.Params.Envelop_AttackTime              = lSettings.GetDouble("Envelop_AttackTime");
      Context.Session.Params.Envelope_ReleaseTime            = lSettings.GetDouble("Envelope_ReleaseTime");
      Context.Session.Params.AmplitudeGate_Threshold         = lSettings.GetDouble("AmplitudeGate_Threshold");
      Context.Session.Params.ExtractGatedlSymbols_MinDuration= lSettings.GetDouble("ExtractGatedlSymbols_MinDuration");
      Context.Session.Params.ExtractGatedlSymbols_MergeGap   = lSettings.GetDouble("ExtractGatedlSymbols_MergeGap");
      Context.Session.Params.BinarizeByDuration_Threshold    = lSettings.GetDouble("BinarizeByDuration_Threshold");

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
