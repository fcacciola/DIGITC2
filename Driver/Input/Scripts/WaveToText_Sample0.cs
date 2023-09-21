namespace DIGITC2 {

public class WaveToText_Sample0 
{
  public static void Run( Context aContext, string[] aCmdLineArgs )
  {
    string lAudioSample0 = aCmdLineArgs[1] ;

    if ( System.IO.File.Exists( lAudioSample0 ) )
    {
      var lSettings = new SimpleSettings(aCmdLineArgs[2]);

      var lSource = new WaveFileSource(lAudioSample0) ;  

      aContext.WindowSizeInSeconds = 250 ;

      aContext.Log("Wave To Text Sample0");

      double lEnvelop_AttackTime              = lSettings.GetDouble("Envelop_AttackTime");
      double lEnvelope_ReleaseTime            = lSettings.GetDouble("Envelope_ReleaseTime");
      double lAmplitudeGate_Threshold         = lSettings.GetDouble("AmplitudeGate_Threshold");
      double lExtractGatedlSymbols_MinDuration= lSettings.GetDouble("ExtractGatedlSymbols_MinDuration");
      double lExtractGatedlSymbols_MergeGap   = lSettings.GetDouble("ExtractGatedlSymbols_MergeGap");
      double lBinarizeByDuration_Threshold    = lSettings.GetDouble("BinarizeByDuration_Threshold");
      int    lBinaryToBytes_BitsPerByte       = lSettings.GetInt   ("BinaryToBytes_BitsPerByte");
      bool   lBinaryToBytes_LittleEndian      = lSettings.GetBool  ("BinaryToBytes_LittleEndian");
      string lBytesToText_CharSet             = lSettings.Get      ("BytesToText_CharSet");

      aContext.Log("Parameters") ; 
      aContext.Log("Envelop_AttackTime              =" + lEnvelop_AttackTime               ) ; 
      aContext.Log("Envelope_ReleaseTime            =" + lEnvelope_ReleaseTime             ) ; 
      aContext.Log("AmplitudeGate_Threshold         =" + lAmplitudeGate_Threshold          ) ; 
      aContext.Log("ExtractGatedlSymbols_MinDuration=" + lExtractGatedlSymbols_MinDuration ) ; 
      aContext.Log("ExtractGatedlSymbols_MergeGap   =" + lExtractGatedlSymbols_MergeGap    ) ; 
      aContext.Log("BinarizeByDuration_Threshold    =" + lBinarizeByDuration_Threshold     ) ; 
      aContext.Log("BinaryToBytes_BitsPerByte       =" + lBinaryToBytes_BitsPerByte        ) ; 
      aContext.Log("BinaryToBytes_LittleEndian      =" + lBinaryToBytes_LittleEndian       ) ; 
      aContext.Log("BytesToText_CharSet             =" + lBytesToText_CharSet              ) ; 

      var lProcessor = new Processor();

      lProcessor.Add( new Envelope(lEnvelop_AttackTime, lEnvelope_ReleaseTime) )
                .Add( new AmplitudeGate(lAmplitudeGate_Threshold) )
                .Add( new ExtractGatedlSymbols(lExtractGatedlSymbols_MinDuration, lExtractGatedlSymbols_MergeGap ) )
                .Add( new BinarizeByDuration(lBinarizeByDuration_Threshold) )
                .Add( new BinaryToBytes(lBinaryToBytes_BitsPerByte, lBinaryToBytes_LittleEndian))
                .Add( new Tokenizer())
                .Add( new WordsToText(lBytesToText_CharSet)) ;

      var lResult = lProcessor.Process( lSource.CreateSignal(), aContext ) ;

    }
  }
}
}
