using System;
using System.Collections.Generic;
using NWaves.Audio;
using NWaves.Operations;
using NWaves.Signals;

namespace DIGITC2
{

public class Script : ScriptBase
{
  public static void Run( Context aContext, string[] aCmdLineArgs )
  {
    Script script = new Script();
    script.DoRun(aContext, aCmdLineArgs); 
  }

  public override void UserCode()
  {
    //<_USER_CODE_HERE>
  }
}

}