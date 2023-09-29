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

  public class ScriptDriver
  {
    public ScriptDriver() {}

    public void Run( string aScriptName, string aUserCode, string[] aCmdLineArgs )
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

        var lResults = lProvider.CompileAssemblyFromSource(lParameters,aUserCode);

        if ( lResults.Errors.Count > 0 )
        {
          Trace.WriteLine("SCRIPT FAILED TO COMPILED.");
          foreach( var lError in lResults.Errors )
            Context.Error(lError.ToString());
        }
        else
        {
          var lCVSType = lResults.CompiledAssembly.GetType($"DIGITC2.{aScriptName}",true);

          var lCVSRunMethod = lCVSType.GetMethod( "Run" ) ;

          Trace.WriteLine("SCRIPT COMPILED.");
          Trace.WriteLine("RUNNING SCRIPT.");
          Trace.WriteLine(" ");
          lCVSRunMethod.Invoke( null, new object[]{aCmdLineArgs} ) ;
        }

      }
    }
  }

}
