using System ;
using System.IO;
using System.Collections.Generic ;

using Newtonsoft.Json ;

namespace DIGITC2_ENGINE
{

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
  public OutcomeSlice( string aName, Signal aInput, Result aResult )
  {
    Name   = aName ; 
    Input  = aInput ;  
    Result = aResult ;  
  }

  public string Name ;

  public Signal Input ;
  public Result Result;

  public List<OutcomeBranch> Branches = new List<OutcomeBranch>(); 

  public override string ToString() => Name ;
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
  public string InputFolder  = null ;
  public string OutputFolder = null;
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

  List<Signal> Slice( Signal aInput )
  {
    return new List<Signal> { aInput };
  }

  Outcome Go( string aWaveFile )
  {
    Outcome rOutcome = null ;

    try
    {
      if ( File.Exists(aWaveFile) )
      {       
        var lSource = new WaveFileSource(aWaveFile);

        var lInput = lSource.CreateSignal();

        if ( lInput != null )
        {
          rOutcome = new Outcome();
          rOutcome.Input = lInput;  

          foreach( var lProcessor in mProcessorFactory.EnumProcessors ) 
          {  
            string lName = $"{lInput.Name}_{lProcessor.Name}";

            string lOutputFolder = Path.Combine( mSettings.OutputFolder, lName );

            if ( ! Directory.Exists( lOutputFolder ) ) 
            {  
              Directory.CreateDirectory(lOutputFolder);
            }

            DIGITC_Context.Setup(new Session(lName, mArgs, mSettings.InputFolder, lOutputFolder) );

            OutcomePipeline lPipeline = new OutcomePipeline(lInput, lProcessor);  
            rOutcome.Pipelines.Add(lPipeline);

            var lSlices = Slice(lInput);

            foreach ( var lSlice in lSlices )
            {
              var lResult = lProcessor.Process(lSlice);

              OutcomeSlice lOS = new OutcomeSlice("<FullLength>",lInput, lResult);

              lPipeline.Slices.Add(lOS);

              foreach( var lPath in lResult.Paths )
              {
                OutcomeBranch lOB = new OutcomeBranch(lPath);
                lOS.Branches.Add(lOB);
              } 
            }

            DIGITC_Context.Shutdown();  

          }
        }
      }
    }
    catch ( Exception e ) 
    {
      DIGITC_Context.Error(e.Message);
    }

    DIGITC_Context.Shutdown();
    
    return rOutcome;
  }

  Args             mArgs ;
  ProcessorFactory mProcessorFactory ;
  AnalyzerSettings mSettings ;
}

}