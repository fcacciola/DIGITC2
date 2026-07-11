using System;
using System.Collections.Generic;
using NWaves.Audio;
using NWaves.Operations;
using NWaves.Signals;

namespace DIGITC1
{

public class Script : ScriptBase
{
  public static void Run( Pipeline aPipeline )
  {
    Script script = new Script();
    script.DoRun(aPipeline); 
  }

  public override void UserCode()
  {
    //<_USER_CODE_HERE>
  }
}

}