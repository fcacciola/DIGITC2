using System;
using System.Collections.Generic;
using System.IO;

using DIGITC2_ENGINE ;

namespace DIGITC2 {

public sealed class AnalyzerTask : DecodingTask
{ 
  public override void Run( Args aArgs  )
  {
    string lArg = aArgs.Get("Audio" ) ;

    List<string> lFiles = new List<string>() ;

    if ( lArg.Contains(",") )
    {
      lFiles.AddRange( lArg.Split(','));
    }
    else
    {
      lFiles.Add(lArg) ;
    }

    lFiles.ForEach( f => RunWithFile(aArgs,f) );  
  }

  void RunWithFile( Args aArgs, string aWaveFilename  )
  {
    if ( File.Exists( aWaveFilename ) )
    {
      AnalyzerSettings lSettings = new AnalyzerSettings{BaseFolder=BaseFolder};

      ProcessorFactory lPF = new ProcessorFactory() ;

      var lOutcome = Analyzer.Analyze(aArgs, lPF, lSettings,aWaveFilename);
    }
  }

}
}
