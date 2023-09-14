using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.CodeDom.Compiler ;
using Microsoft.CSharp ;

using NWaves.Audio;
using NWaves.Operations;
using NWaves.Signals;

namespace DIGITC2
{
  public class SimpleSettings
  {
    public Dictionary<string, string> Settings = new Dictionary<string, string>();

    public SimpleSettings(string file)
    {
      Settings.Clear();

      //File.ReadLines(file).ToList().ForEach( l => Trace.WriteLine("[" + l + "]"));

      Settings = File.ReadLines(file)
                      .Where(isValidLine)
                      .Select(line => line.Split('='))
                      .ToDictionary(line => line[0], line => line[1]);
    }

    private bool isValidLine(string line)
    {
        return !line.StartsWith("#") && line.Contains("=");
    }

    public string Get(string aKey) => Settings.ContainsKey(aKey) ? Settings[aKey] : null; 

    public int    GetInt   (string aKey) => Convert.ToInt32  (Get(aKey));
    public double GetDouble(string aKey) => Convert.ToDouble (Get(aKey));
    public bool   GetBool  (string aKey) => Convert.ToBoolean(Get(aKey));
  }

  public abstract class ScriptBase
  {
    public void DoRun( Context aContext, string[] aCmdLineArgs )
    {
      try
      { 
        Context     = aContext;
        CmdLineArgs = aCmdLineArgs ;

        UserCode();
      }
      catch (Exception ex)
      {
        aContext.Error(ex.ToString());
      }

    }

    public abstract void UserCode();

    protected Context  Context     = null ;
    protected string[] CmdLineArgs = null ;

  }


  public class ScriptDriver
  {
    public ScriptDriver() {}

    public void Run( string aUserCode, string[] aCmdLineArgs )
    {
      using ( CSharpCodeProvider lProvider   = new CSharpCodeProvider() )
      {
        CompilerParameters lParameters = new CompilerParameters();

        lParameters.GenerateExecutable = false ;
        lParameters.GenerateInMemory   = true  ;
        lParameters.ReferencedAssemblies.Add("DIGITC2.exe");
        lParameters.ReferencedAssemblies.Add("DIGITC2_Engine.dll");
        lParameters.ReferencedAssemblies.Add("nwaves.dll");
        lParameters.IncludeDebugInformation = true ;
        lParameters.CompilerOptions = "/unsafe /optimize /langversion:5";

  //Trace.WriteLine("User Code");
  //Trace.WriteLine(aUserCode);

        string lBaseScript = File.ReadAllText(@".\Input\Scripts\BaseScript.cs");
        string lScript     = lBaseScript.Replace("//<_USER_CODE_HERE>",aUserCode);

  //Trace.WriteLine("Full User Script");
  //Trace.WriteLine(lScript);

        var lResults = lProvider.CompileAssemblyFromSource(lParameters,lScript);

        Context lContext = new Context();

        if ( lResults.Errors.Count > 0 )
        {
          Trace.WriteLine("SCRIPT FAILED TO COMPILED.");
          foreach( var lError in lResults.Errors )
            lContext.Error(lError.ToString());
        }
        else
        {
          var lCVSType = lResults.CompiledAssembly.GetType("DIGITC2.Script",true);

          var lCVSRunMethod = lCVSType.GetMethod( "Run" ) ;

          Trace.WriteLine("SCRIPT COMPILED.");
          Trace.WriteLine("RUNNING SCRIPT.");
          Trace.WriteLine(" ");
          lCVSRunMethod.Invoke( null, new object[]{lContext, aCmdLineArgs} ) ;
        }

      }
    }
  }

}
