using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2_ENGINE
{

  public class Session
  {
    public Session( string aName, Args aArgs, string aBaseFolder )
    {
      Name             = aName ;
      Args             = aArgs;
      BaseFolder       = aBaseFolder ;
      InputFolder      = Path.Combine(BaseFolder,"Input") ; 
      OutputRootFolder = Path.Combine(BaseFolder,"Output",Name) ; 

      Utils.SetupFolder(OutputRootFolder);

      OutputProcessorFolder = OutputRootFolder; 
      Utils.SetupFolder(LogsFolder);
    }  

    public void SetupSlice( string aSliceName)
    {
      OutputSliceFolder = Path.Combine(OutputRootFolder, aSliceName);
      Utils.SetupFolder(OutputSliceFolder);
    }

    public void SetupProcessor( string aProcessorName)
    {
      OutputProcessorFolder = Path.Combine( OutputSliceFolder ?? OutputRootFolder, aProcessorName);
      Utils.SetupFolder(OutputProcessorFolder);
      Utils.SetupFolder(LogsFolder);
      Utils.SetupFolder(ResultsFolder);
      Utils.SetupFolder($"{ResultsFolder}/Undefined");
      Utils.SetupFolder($"{ResultsFolder}/Discarded");
      Utils.SetupFolder($"{ResultsFolder}/Poor");
      Utils.SetupFolder($"{ResultsFolder}/Good");
      Utils.SetupFolder($"{ResultsFolder}/Excelent");
      Utils.SetupFolder($"{ResultsFolder}/Perfect");
    }

    public string LogsFolder    => $"{OutputProcessorFolder}/Logs";  
    public string ResultsFolder => $"{OutputProcessorFolder}/Results"; 

    public string Name ;
    public Args   Args ;
    public string BaseFolder ;
    public string InputFolder ;
    public string OutputRootFolder ;
    public string OutputSliceFolder;
    public string OutputProcessorFolder ;

    public string ReferenceFile ( string aFilename ) => $"{InputFolder}/References/{aFilename}";
    public string LogFile       ( string aFilename ) => $"{LogsFolder}/{aFilename}";

    public string TraceFile    => LogFile("Log.txt");

    public string ReportFile( ResultPath aResult ) => $"{ResultsFolder}/{aResult.Fitness}/Report.txt";




  }
}
