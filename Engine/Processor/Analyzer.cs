using System ;
using System.IO;
using System.Collections.Generic ;

using Newtonsoft.Json ;

namespace DIGITC2_ENGINE
{

public class SignalSlice
{
  public string Name ;
  public Signal Signal ;
  public Signal WholeSignal ;
}

public class OutcomeSummary
{
  public static OutcomeSummary Load( string aFile )
  {
    if ( ! File.Exists(aFile) )
      return null;

    return JsonConvert.DeserializeObject<OutcomeSummary>(File.ReadAllText(aFile));
  }

  public void Save( string aFile )
  {
    File.WriteAllText(aFile, JsonConvert.SerializeObject(this, Formatting.Indented));
  }
}

public class OutcomeBranch
{
  public OutcomeBranch( ResultPath aResult ) 
  {
    Result = aResult ;
  }

  public string Name ;

  public ResultPath Result;

  public override string ToString() => Name ;
}

public class OutcomeSlice
{
  public OutcomeSlice( SignalSlice aSlice, Result aResult )
  {
    Slice  = aSlice ;
    Result = aResult ;  
  }

  public SignalSlice Slice ;
  public Result      Result;

  public List<OutcomeBranch> Branches = new List<OutcomeBranch>(); 

  public override string ToString() => Slice.Name ;
}

public class OutcomePipeline
{
  public OutcomePipeline( Signal aInput, Processor aProcessor)
  {
    Input     = aInput ;
    Processor = aProcessor ;
  }

  public Signal    Input ;
  public Processor Processor ;

  public List<OutcomeSlice> Slices = new List<OutcomeSlice>(); 

  public override string ToString() => Processor.Name ;
}

public class Outcome
{
  public Outcome() {}

  public List<OutcomePipeline> Pipelines = new List<OutcomePipeline>();  

  public Signal Input ;

  public OutcomeSummary Summary 
  {
    get
    {
      if ( mSummary == null )
        CreateSummary();
      return mSummary ;
    }
  }

  void CreateSummary()
  {

  }

  OutcomeSummary mSummary = null ;
}

public class AnalyzerSettings
{
  public string BaseFolder = null ;
}

public class Analyzer
{
  public Analyzer( Args Args, ProcessorFactory aProcessorFactory, AnalyzerSettings aSettings ) 
  {
    mArgs             = Args ;
    mProcessorFactory = aProcessorFactory ;
    mSettings         = aSettings ; 
  }  

  static public Outcome Analyze( Args aArgs, ProcessorFactory aProcessorFactory, AnalyzerSettings aSettings, string aWaveFile )
  {
    Analyzer rA = new Analyzer(aArgs, aProcessorFactory, aSettings);
    return rA.Go(aWaveFile);
  }

  List<SignalSlice> Slice( Signal aInput )
  {
    return new List<SignalSlice> { new SignalSlice{Name ="WholeSlice", Signal = aInput, WholeSignal = aInput } };
  }

  Outcome Go( string aWaveFile )
  {
    Outcome rOutcome = null ;

    if ( File.Exists(aWaveFile) )
    {       
      var lSource = new WaveFileSource(aWaveFile);

      DContext.Setup(new Session( lSource.Name, mArgs, mSettings.BaseFolder) );

      try
      {
        var lInput = lSource.CreateSignal();

        if ( lInput != null )
        {
          rOutcome = new Outcome();
          rOutcome.Input = lInput;  

          var lSlices = Slice(lInput);

          foreach ( var lSlice in lSlices )
          {
            if ( lSlices.Count > 1 )
              DContext.Session.PushFolder(lSlice.Name);

            foreach( var lProcessor in mProcessorFactory.EnumProcessors ) 
            {  
              OutcomePipeline lPipeline = new OutcomePipeline(lInput, lProcessor);  
              rOutcome.Pipelines.Add(lPipeline);

              var lResult = lProcessor.Process(lSlice.Signal);

              var lReports = lResult.Save();

              OutcomeSlice lOS = new OutcomeSlice(lSlice, lResult);

              lPipeline.Slices.Add(lOS);

              foreach( var lPath in lResult.Paths )
              {
                OutcomeBranch lOB = new OutcomeBranch(lPath);
                lOS.Branches.Add(lOB);
              } 
            }

            if ( lSlices.Count > 1 )
              DContext.Session.PopFolder();

          }
        }
      }
      catch ( Exception e ) 
      {
        DContext.Error(e.Message);
      }

      DContext.Shutdown();
    }
    
    return rOutcome;
  }

  Args             mArgs ;
  ProcessorFactory mProcessorFactory ;
  AnalyzerSettings mSettings ;
}

}