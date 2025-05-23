using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;

using DIGITC2;
using NWaves.Audio;
using NWaves.Signals;
using NWaves.Signals.Builders;

using DIGITC2_ENGINE ;
using System.Runtime.CompilerServices;

namespace DIGITC2
{
  public abstract class Task
  {
    public abstract void Run( Args aArgs ) ;

    public static void Save( WaveSignal aS, string aFilename )
    {
      Save( aS.Rep, aFilename );
    }

    public static void Save( DiscreteSignal aS, string aFilename )
    {
      Save( new WaveFile(aS), aFilename );
    }

    public static void Save( WaveFile aWF, string aFilename )
    {
      try
      {
        using (var stream = new FileStream(aFilename, FileMode.Create))
        {
          aWF.SaveTo(stream);
        }
      }
      catch( Exception ex)
      {
        DContext.Error($"Failed to save .WAV file to:[{aFilename}]\n{ex.ToString()}");
      }
    }

    public static string BaseFolder  => Path.Combine( Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DIGITC2") ; 

    public string ExpandRelativeFilePath ( string aPath )
    {
      if ( aPath.StartsWith("@") )
           return BaseFolder + aPath.Substring(1);
      else return aPath ;
    }
  }

  public abstract class DecodingTask : Task
  {
  }

  public abstract class GeneratorTask : Task
  {
  }
}

namespace Driver
{

  internal class TaskTable
  {
    internal TaskTable() 
    {
      RegisterTask( new FromAudio_ByPulseDuration() ); 
      RegisterTask( new FromAudio_ByTapCode_Binary() ); 
      RegisterTask( new FromAudio_ByTapCode_DirectLetters() ); 
      RegisterTask( new Generate_MockAudio_WithTapCode() ); 
      RegisterTask( new AnalyzerTask() ) ;
    }

    internal void Run( Args aArgs)
    {
      foreach( var lKV in mTasks )
      {
        if ( aArgs.GetBool(lKV.Key) )
          lKV.Value.Run( aArgs );
      }
    }

    void RegisterTask( Task aTask )
    {
      mTasks.Add(aTask.GetType().Name, aTask);
    }

    Dictionary<string, Task> mTasks = new Dictionary<string,Task>();
  }


  public class Pipe
  {
    public string Name { get; set; }

    public override string ToString() => Name ;
  }

  public class Code
  {
    public string Name { get; set; }

    public Pipe Process ( Pipe aInput, Pipeline aPipeline, Queue<Pipeline> aQueue )
    {
      Console.WriteLine($"{aPipeline.Tab}{Name} processing {aInput}");

      string lPN = $"{aInput.Name}_from_{Name}";

      var rR = new Pipe{ Name = lPN } ; 

      var rBP = new Pipe{ Name = $"{lPN}_B1" } ; 

      var rB = aPipeline.Branch(rBP) ;
      if (  rB != null )  
        aQueue.Enqueue(rB) ;

      var rBP2 = new Pipe{ Name = $"{lPN}_B2" } ; 
      var rB2 = aPipeline.Branch(rBP2) ;
      if (  rB2 != null )  
        aQueue.Enqueue(rB2) ;

      return rR ;
    }

    public override string ToString() => Name ;
  }

  public class Pipeline
  {
    public Pipe Pipe ;
    public List<Code> Codes = new List<Code>();

    public int Level   = 0 ;
    public int CodeIdx = 0 ;  

    public string Tab => new string(' ', Level*2) ;

    public Pipeline Branch( Pipe aPipe )
    {
      var lCodes = Codes.Skip(CodeIdx).ToList() ;

      if ( lCodes.Count == 0 )
           return null ;
      else return new Pipeline{ Pipe = aPipe 
                              , CodeIdx = CodeIdx 
                              , Codes = lCodes
                              , Level = Level + 1
                              } ;
    }

    public override string ToString() => $"L={Level} C={CodeIdx} {Pipe} {Codes.Count}" ;

  }

  public class Pro
  {
    public Pro()
    {
      
    }
    
    public void Go()
    {
      var lEntry = new Pipeline{ Pipe = new Pipe{Name="Entry"}
                               , Codes = new List<Code>(){ new Code{Name="C0"}
                                                         , new Code{Name="C1"}
                                                         , new Code{Name="C2"}
                                                         } } ;
      

      Pipelines.Enqueue(lEntry);

      do
      {
        var lPipeline = Pipelines.Peek(); Pipelines.Dequeue();
        var lPipe = lPipeline.Pipe ;

        lPipeline.CodeIdx = 0  ;
          
        foreach( var lCode in lPipeline.Codes )
        {
          lPipeline.CodeIdx = lPipeline.CodeIdx + 1  ;
          lPipe = lCode.Process(lPipe, lPipeline, Pipelines);
        }

        Console.WriteLine( $"{lPipeline.Tab}End of line");
      }
      while (Pipelines.Count > 0);
    }

    public Queue<Pipeline> Pipelines = new Queue<Pipeline>();
  }


  internal class Program
  {
 
    [STAThread]
    static void Main(string[] args)
    {

Pro pl = new Pro();
pl.Go();
return ;

      Args lArgs = Args.FromCmdLine(args);

      TaskTable lTasks = new TaskTable();

      lTasks.Run( lArgs );  
    }
  }
}