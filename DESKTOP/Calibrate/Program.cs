using ENGINE ;

string lWorkingFolder = Directory.GetCurrentDirectory();
List<string> lFiles = new List<string>();

for( int i = 0; i < args.Length; i++) 
{
  if ( i == 0 )
    lWorkingFolder = args[i];
  else
  {
    string lFile = $"{lWorkingFolder}\\{args[i]}.wav";
    if ( File.Exists(lFile))
      lFiles.Add(lFile);
  }
}

var lCalibrator = new Calibrator();
lCalibrator.Run(lWorkingFolder, lFiles);

file class Calibrator_DriverApp : DriverApp
{
  public Calibrator_DriverApp() {}

  public void Save( string aFile )
  {
    File.WriteAllLines(aFile, mLines);
  }

  public override void AddMessage     ( string aMsg ) => mLines.Add(aMsg);
  public override void AddErrorMessage( string aMsg ) => mLines.Add("ERROR: " + aMsg);

  List<string> mLines = new List<string>();
}

file class Calibrator
{
  public Calibrator()
  {
  }

  public void Run( string aWorkingFolder, List<string> aFiles )
  {
    Console.WriteLine("Transgraphier Calibrate");

    mDriverApp = new Calibrator_DriverApp();

    mSettings = Settings.FromFile(SettingsFile);
    mSettings.Set("CalibrateScores","true","");
    mSettings.ChangeValue("InputFolder" , InputFolder);  
    mSettings.ChangeValue("OutputFolder", OutputFolder);  

    if ( ! Directory.Exists( OutputFolder ))
      Directory.CreateDirectory( OutputFolder );  

    var lConfigFile =$"{InputFolder}\\config.txt";

    mConfig= Config.FromFile(lConfigFile); 

    AddGeneralMessage("Transgraphier 3.2  - CALIBRATOR started.");
    AddGeneralMessage($"Input Folder: {InputFolder}.");
    AddGeneralMessage($"Output Folder: {OutputFolder}.");
    AddGeneralMessage($"Input WAV Samples Folder: {mSettings.GetPath("SamplesFolder") ?? InputFolder}.");

    List<string> lFiles = aFiles.Count > 0 ? aFiles : Directory.GetFiles(aWorkingFolder, "*.wav").ToList();

    foreach ( var lFile in lFiles ) 
    {
      Process(lFile);
    }

    mDriverApp.Save($"{OutputFolder}\\Log.txt");
  }

  void Process(string aFile )
  {
    var lSigRep  = SignalLoader.LoadSignal(aFile);
    var lSession = new Session(aFile, Path.GetFileNameWithoutExtension(aFile), mSettings, mDriverApp, mConfig);

    try
    {
      AddGeneralMessage( $"Processing started. Session Folder: {lSession.CurrentOutputFolder}. ");

      MainPipeline lPipeline = PipelineFactory.FromAudioToTapCode().Then(PipelineFactory.FromTapCode()) ;

      Signal lStartSignal = new WaveSignal(lSigRep) ;

      var lResult = Processor.Process( lSession, mSettings, lSession.Name, lPipeline, lStartSignal);
        
      lResult.Save()  ;

      AddGeneralMessage("...finished.");
    }
    catch (Exception ex)
    {
      AddErrorMessage($"Processing FAILED!!!: {ex}");
    }

    lSession.Shutdown(); 
  }

  void AddGeneralMessage(string aMsg) { mDriverApp.AddMessage(aMsg); }
  void AddErrorMessage  (string aMsg) { mDriverApp.AddErrorMessage(aMsg); }

  static string InputFolder  = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Varanormal\\Transgraphier\\Input");
  static string OutputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Varanormal\\Transgraphier\\Output\\Calibration");
  static string AppInputFolder = Path.Combine(AppContext.BaseDirectory,"Input");
  static string SettingsFile = $"{InputFolder}\\Settings.txt";


  Calibrator_DriverApp mDriverApp;
  Settings   mSettings;
  Config     mConfig;
}

