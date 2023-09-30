namespace DIGITC2 {

public class WaveToText_Sample0 
{
  public static void Run( string[] aCmdLineArgs )
  {
    Context.Setup( new Session("WaveToText_Sample0") ) ;

    string lAudioSample0 = aCmdLineArgs[1] ;

    if ( System.IO.File.Exists( lAudioSample0 ) )
    {
      var lSettings = new SimpleSettings(aCmdLineArgs[2]);

      var lSource = new WaveFileSource(lAudioSample0) ;  

      Context.Session.Params.WindowSizeInSeconds = 250 ;

      Context.WriteLine("Wave To Text Sample0");

      double lEnvelop_AttackTime              = lSettings.GetDouble("Envelop_AttackTime");
      double lEnvelope_ReleaseTime            = lSettings.GetDouble("Envelope_ReleaseTime");
      double lAmplitudeGate_Threshold         = lSettings.GetDouble("AmplitudeGate_Threshold");
      double lExtractGatedlSymbols_MinDuration= lSettings.GetDouble("ExtractGatedlSymbols_MinDuration");
      double lExtractGatedlSymbols_MergeGap   = lSettings.GetDouble("ExtractGatedlSymbols_MergeGap");
      double lBinarizeByDuration_Threshold    = lSettings.GetDouble("BinarizeByDuration_Threshold");
      int    lBinaryToBytes_BitsPerByte       = lSettings.GetInt   ("BinaryToBytes_BitsPerByte");
      bool   lBinaryToBytes_LittleEndian      = lSettings.GetBool  ("BinaryToBytes_LittleEndian");
      string lBytesToText_CharSet             = lSettings.Get      ("BytesToText_CharSet");

      Context.WriteLine("Parameters") ; 
      Context.WriteLine("Envelop_AttackTime              =" + lEnvelop_AttackTime               ) ; 
      Context.WriteLine("Envelope_ReleaseTime            =" + lEnvelope_ReleaseTime             ) ; 
      Context.WriteLine("AmplitudeGate_Threshold         =" + lAmplitudeGate_Threshold          ) ; 
      Context.WriteLine("ExtractGatedlSymbols_MinDuration=" + lExtractGatedlSymbols_MinDuration ) ; 
      Context.WriteLine("ExtractGatedlSymbols_MergeGap   =" + lExtractGatedlSymbols_MergeGap    ) ; 
      Context.WriteLine("BinarizeByDuration_Threshold    =" + lBinarizeByDuration_Threshold     ) ; 
      Context.WriteLine("BinaryToBytes_BitsPerByte       =" + lBinaryToBytes_BitsPerByte        ) ; 
      Context.WriteLine("BinaryToBytes_LittleEndian      =" + lBinaryToBytes_LittleEndian       ) ; 
      Context.WriteLine("BytesToText_CharSet             =" + lBytesToText_CharSet              ) ; 

      var lProcessor = new Processor();

      lProcessor.Add( new Envelope(lEnvelop_AttackTime, lEnvelope_ReleaseTime) )
                .Add( new AmplitudeGate(lAmplitudeGate_Threshold) )
                .Add( new ExtractGatedlSymbols(lExtractGatedlSymbols_MinDuration, lExtractGatedlSymbols_MergeGap ) )
                .Add( new BinarizeByDuration(lBinarizeByDuration_Threshold) )
                .Add( new BinaryToBytes(lBinaryToBytes_BitsPerByte, lBinaryToBytes_LittleEndian))
                .Add( new Tokenizer())
                .Add( new TokensToWords(lBytesToText_CharSet)) ;

      var lResult = lProcessor.Process( lSource.CreateSignal() ) ;

    }

    Context.Shutdown(); 
  }
}
}
