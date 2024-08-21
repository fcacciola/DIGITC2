using System.IO;
using DIGITC2.ViewModel;

using DIGITC2_ENGINE;

using Newtonsoft.Json ;

namespace DIGITC2;

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


public class Analyzer
{
  public Analyzer() {}  

  static public Outcome Analyze( string aRootFolder, string aWaveFile, List<string> aProcessors )
  {
    Analyzer rA = new Analyzer();
    return rA.Go(aRootFolder, aWaveFile, aProcessors );
  }

  List<Signal> Slice( Signal aInput )
  {
    return new List<Signal> { aInput };
  }

  Outcome Go( string aRootFolder, string aWaveFile, List<string> aProcessors )
  {
    Outcome rOutcome = null ;

    try
    {
      Args lArgs = new Args();

      string lInputFolder =  Path.Combine(FileSystem.AppDataDirectory,"Input") ;
      string lOutputFolder = Path.Combine(aRootFolder,"Output");

      if ( ! Directory.Exists(lOutputFolder) )
        Directory.CreateDirectory(lOutputFolder); 

      DIGITC_Context.Setup(new Session("FromAudio_" + Path.GetFileNameWithoutExtension(aWaveFile), lArgs, lInputFolder, lOutputFolder) );
      
      var lSource = new WaveFileSource(aWaveFile);

      var lInput = lSource.CreateSignal();

      rOutcome = new Outcome();
      rOutcome.Input = lInput;  

      ProcessorFactory lFactory = new ProcessorFactory();

      foreach( string lProc in aProcessors ) 
      { 
        var lProcessor = lFactory.Get(lProc);
        if ( lProcessor != null )
        {
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

}