using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using DIGITC2;
using NWaves.Audio;
using NWaves.Signals;
using NWaves.Signals.Builders;

using DIGITC2_ENGINE ;

namespace DIGITC2
{
  public abstract class Task
  {
    public abstract void Run( Settings aSettings, List<Config> aConfigs ) ;

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

        DContext.WriteLine($"Signal saved to .WAV file:[{aFilename}]");
      }
      catch( Exception ex)
      {
        DContext.Error($"Failed to save .WAV file to:[{aFilename}]\n{ex.ToString()}");
      }
    }

    public static string BaseFolder  => Path.Combine( Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DIGITC2") ; 

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
      RegisterTask( new FromAudio_ByTapCode_Binary() ); 
      RegisterTask( new FromAudio_ByTapCode_DirectLetters() ); 
      RegisterTask( new Generate_MockAudio_WithTapCode_Synthetic() ); 
      RegisterTask( new Generate_MockAudio_WithTapCode_FromSamples() ); 
    }

    internal void Run( Params aMainParams, List<Config> aConfigs)
    {
      foreach( var lKV in mTasks )
      {
        if ( aMainParams.GetBool(lKV.Key) )
          lKV.Value.Run( aMainParams, aConfigs );
      }
    }

    void RegisterTask( Task aTask )
    {
      mTasks.Add(aTask.GetType().Name, aTask);
    }

    Dictionary<string, Task> mTasks = new Dictionary<string,Task>();
  }

  internal class Program
  {
    static List<Config> LoadConfigs(string[] aArgs)
    {
      List<Config> rR = new List<Config>();  
      for( int i = 1 ; i < aArgs.Length ; ++ i )
      {
        var lConfig = Config.FromFile(aArgs[i]); 
        if ( lConfig != null )  
          rR.Add(lConfig);  
      }
      return rR;  
    }

    [STAThread]
    static void Main(string[] args)
    {
      if ( args.Length >= 2 )
      {
        var lMainParams = Params.FromFile(args[0]); 
        var lConfigs = LoadConfigs(args);

        TaskTable lTasks = new TaskTable();

        lTasks.Run(lMainParams, lConfigs);  

      }
    }
  }
}