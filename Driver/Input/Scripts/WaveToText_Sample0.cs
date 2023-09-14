string lAudioSample0 = CmdLineArgs[1] ;

if ( System.IO.File.Exists( lAudioSample0 ) )
{
  var lSettings = new SimpleSettings(CmdLineArgs[2]);

  var lSource = new WaveFileSource(lAudioSample0) ;  

  Context lContext = new Context() { WindowSizeInSeconds = 250 } ;

  Context.Log("Wave To Text Sample0");

  double lEnvelop_AttackTime              = lSettings.GetDouble("Envelop_AttackTime");
  double lEnvelope_ReleaseTime            = lSettings.GetDouble("Envelope_ReleaseTime");
  double lAmplitudeGate_Threshold         = lSettings.GetDouble("AmplitudeGate_Threshold");
  double lExtractGatedlSymbols_MinDuration= lSettings.GetDouble("ExtractGatedlSymbols_MinDuration");
  double lExtractGatedlSymbols_MergeGap   = lSettings.GetDouble("ExtractGatedlSymbols_MergeGap");
  double lBinarizeByDuration_Threshold    = lSettings.GetDouble("BinarizeByDuration_Threshold");
  int    lBinaryToBytes_BitsPerByte       = lSettings.GetInt   ("BinaryToBytes_BitsPerByte");
  bool   lBinaryToBytes_LittleEndian      = lSettings.GetBool  ("BinaryToBytes_LittleEndian");
  string lBytesToText_CharSet             = lSettings.Get      ("BytesToText_CharSet");

  Context.Log("Parameters") ; 
  Context.Log("Envelop_AttackTime              =" + lEnvelop_AttackTime               ) ; 
  Context.Log("Envelope_ReleaseTime            =" + lEnvelope_ReleaseTime             ) ; 
  Context.Log("AmplitudeGate_Threshold         =" + lAmplitudeGate_Threshold          ) ; 
  Context.Log("ExtractGatedlSymbols_MinDuration=" + lExtractGatedlSymbols_MinDuration ) ; 
  Context.Log("ExtractGatedlSymbols_MergeGap   =" + lExtractGatedlSymbols_MergeGap    ) ; 
  Context.Log("BinarizeByDuration_Threshold    =" + lBinarizeByDuration_Threshold     ) ; 
  Context.Log("BinaryToBytes_BitsPerByte       =" + lBinaryToBytes_BitsPerByte        ) ; 
  Context.Log("BinaryToBytes_LittleEndian      =" + lBinaryToBytes_LittleEndian       ) ; 
  Context.Log("BytesToText_CharSet             =" + lBytesToText_CharSet              ) ; 

  var lProcessor = new Processor();

  lProcessor.Add( new Envelope(lEnvelop_AttackTime, lEnvelope_ReleaseTime) )
            .Add( new AmplitudeGate(lAmplitudeGate_Threshold) )
            .Add( new ExtractGatedlSymbols(lExtractGatedlSymbols_MinDuration, lExtractGatedlSymbols_MergeGap ) )
            .Add( new BinarizeByDuration(lBinarizeByDuration_Threshold) )
            .Add( new BinaryToBytes(lBinaryToBytes_BitsPerByte, lBinaryToBytes_LittleEndian))
            .Add( new BytesToText(lBytesToText_CharSet)) ;

  var lResult = lProcessor.Process( lSource, lContext ) ;

}

