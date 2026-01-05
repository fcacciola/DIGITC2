using System;
using System.IO;
using System.Windows.Forms;
//using System.Windows.Controls;

using DIGITC2_ENGINE ;
using NWaves.Signals ;

namespace Transgraphier_1_0_App
{

  public class MainWindowGUI : GUI
  {
    public MainWindowGUI( Form1 aMainWindow)
    { 
      mMainWindow = aMainWindow ; 
    }

    public override void AddMessage     ( string aMsg ) => mMainWindow.AddGeneralMessage(aMsg,true);
    public override void AddErrorMessage( string aMsg ) => mMainWindow.AddErrorMessage  (aMsg,true);

    Form1 mMainWindow; 
  }

  public class ZoomPanController
  {
    public double    MinSamplesPerPixel { get ; set ; }  
    public double    MaxSamplesPerPixel { get ; set ; }
    public WaveViews WaveViews          { get ; set ; }
    
    public double    SamplesPerPixel    { get ; private set ; }
    public double    StartSample        { get ; private set ; }

    public void UpdateSPP( double aSamplePerPixel )
    {
      SamplesPerPixel = aSamplePerPixel ;
      Invalidate();
    }

    public void UpdateSS( double aStartSample)
    {
      StartSample = aStartSample ;
      Invalidate();
    }

    public void Update( double aSamplePerPixel, double aStartSample)
    {
      SamplesPerPixel = aSamplePerPixel ;
      StartSample     = aStartSample ;
      Invalidate();
    }

    public void Invalidate()
    {
      WaveViews.Invalidate();
    }

  }


  public class WaveViews
  {
    public WaveViews()
    {
    }

    public void Clear() { mViews.Clear(); } 

    public void Add( WaveView aView) { mViews.Add( aView ); }

    public void Invalidate() { mViews.ForEach(v => v.InvalidateRender()); }

    public int Count => mViews.Count;

    List<WaveView> mViews = new List<WaveView>();
  }

  public class SessionResult
  {
    public string       FilterName          { get ; set ; } 
    public string       TextResultFileName  { get ; set ; }
    public List<string> WaveResultFileNames { get ; set ; } = new List<string>();
  }

  public partial class Form1 : Form
  {
    TabControl    mSessionsTabControl;
    Settings      mSettings  = null ;
    List<Config>  mConfigs   = null ;
    string        mInputFile = null ; 
    MainWindowGUI mMWGUI     = null; 
    WaveViews     mWaveViews = new WaveViews();

    public Form1()
    {
      InitializeComponent();

      mMWGUI = new MainWindowGUI(this); 

      mSettings = Settings.FromFile($"{InputFolder}\\Settings.txt");

      mSettings.Set("InputFolder" , InputFolder);  
      mSettings.Set("OutputFolder", OutputFolder);  

      if ( ! Directory.Exists( OutputFolder ))
        Directory.CreateDirectory( OutputFolder );  

      mConfigs  = LoadConfigs();  
    }

    static string InputFolder  = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Input");
    static string OutputFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");

    void SetupZoomPanController( DiscreteSignal aSignal)
    {
      var availableWidth = Math.Max(1.0, mInputWave.Width);

      var lZPC = new ZoomPanController(){ MinSamplesPerPixel = 2.0
                                        , MaxSamplesPerPixel = aSignal.Length / availableWidth
                                        , WaveViews = mWaveViews  
      };
          
      lZPC.Update(lZPC.MaxSamplesPerPixel,0 ) ; // Zoom out the entire signal

      mInputWave.ZoomPanController = lZPC; 
    }

    static List<Config> LoadConfigs()
    {
      List<Config> rR = new List<Config>();  
      string[] lFiles = Directory.GetFiles(InputFolder, "Config_*.txt");

      foreach( var lFile in lFiles)
      {
        var lConfig = Config.FromFile(lFile); 
        if ( lConfig != null )  
          rR.Add(lConfig);  
      }
      return rR;  
    }

    Dictionary<string,string> GetParameters( string aFilter )
    {
      return mConfigs[0].GetSection(aFilter).Map;
    }

    void ShowSession( string aSession )
    {
      mSessionsTabControl.Name = Path.GetFileNameWithoutExtension(aSession);

      AddGeneralMessage( $"Loading Session: [{aSession}]");

      mInputFile = Directory.GetFiles(aSession, "*.wav")[0];

      if ( mInputFile == null )
      {
        AddErrorMessage($"Input file not found in session: [{aSession}]");
        return;
      }

      var lInputSignal = SignalLoader.LoadSignal(mInputFile);
      SetupZoomPanController(lInputSignal);
      mInputWave.Signal = lInputSignal;
      mInputWave.Parameters = GetParameters("Input");
      mInputWave.Title = "Input Signal";  
      mInputWave.Height = 300;
      mWaveViews.Add( mInputWave );

      // Create a scrollable container for the tab content
      var lRootTab = new TabPage { Name = Path.GetFileNameWithoutExtension(aSession) };
      
      Panel scrollPanel = new Panel();
      scrollPanel.Dock = DockStyle.Fill;
      scrollPanel.AutoScroll = true;
      
      Panel contentPanel = new Panel();
      contentPanel.Dock = DockStyle.Top;
      contentPanel.Width = scrollPanel.Width;
      
      scrollPanel.Controls.Add(contentPanel);
      lRootTab.Controls.Add(scrollPanel);
      
      mSessionsTabControl.TabPages.Add(lRootTab);

      var lResultFolderSequence = new List<string>();
      var lCurrFolder = $"{aSession}\\Pipeline_0";
      while (true)
      {
        lResultFolderSequence.Add(lCurrFolder);
        var children = Directory.GetDirectories(lCurrFolder).OrderBy(x => x).ToArray();
        if (children.Length == 1)
        {
          lCurrFolder = children[0];
          continue;
        }
        break;
      }

      List<SessionResult> lSessionResults = new List<SessionResult>();  

      foreach (var lResultFolder in lResultFolderSequence)
      {
        var lLocalTextResult  = Directory.GetFiles(lResultFolder, "*.txt").FirstOrDefault();
        if ( lLocalTextResult != null) 
        {  
          var lLocalWaveResults = Directory.GetFiles(lResultFolder, "*.wav");

          var lSessionResult = new SessionResult(){
            FilterName         = Path.GetFileNameWithoutExtension(lLocalTextResult),
            TextResultFileName = lLocalTextResult,
            WaveResultFileNames= lLocalWaveResults.ToList()
          };

          lSessionResults.Add( lSessionResult ); 
        }
      }

      int currentY = 0;

      foreach( var lSessionResult in lSessionResults )
      {
       string lTextResult = lSessionResult.TextResultFileName;

        // Create LexicalView for this file
        LexicalView lLexicalView = new LexicalView();
        lLexicalView.Location = new Point(0, currentY);
        lLexicalView.Width = scrollPanel.Width;
        lLexicalView.Title = lSessionResult.FilterName;
        lLexicalView.Parameters = GetParameters(lSessionResult.FilterName);

        try
        {
          string textContent = File.ReadAllText(lTextResult);
          lLexicalView.TextContent = textContent;
        }
        catch (Exception ex)
        {
          AddErrorMessage($"Error reading text file {lTextResult}: {ex.Message}");
        }

        // Add lexical view to content panel
        contentPanel.Controls.Add(lLexicalView);
        currentY += lLexicalView.Height;

        foreach( var lWaveResult in lSessionResult.WaveResultFileNames)
        {
          string lWaveFilename = Path.GetFileNameWithoutExtension(lWaveResult);

          // Create WaveFormView for this file
          WaveView lWaveView = new WaveView();
          lWaveView.Location = new Point(0, currentY);
          lWaveView.Width = scrollPanel.Width;
          lWaveView.Title = lWaveFilename;
          lWaveView.Parameters = GetParameters(lSessionResult.FilterName);

          var lResultSignal = SignalLoader.LoadSignal(lWaveResult);

          lWaveView.ZoomPanController = mInputWave.ZoomPanController ; 

          lWaveView.Signal = lResultSignal;

          // Add waveform view to content panel
          contentPanel.Controls.Add(lWaveView);
          currentY += lWaveView.Height;

          mWaveViews.Add(lWaveView);
        }

      } 

      // Set the content panel dimensions to accommodate all controls with full width
      contentPanel.Size = new Size(scrollPanel.Width, currentY);

      mWaveViews.Invalidate();
      AddGeneralMessage("Session Results loaded");

      this.Refresh();
    }

    void ShowSessions()
    {
      // Create tab control if it doesn't exist
      if (mSessionsTabControl == null)
      {
        mSessionsTabControl = new TabControl();
        mSessionsTabControl.Dock = DockStyle.Fill;

        // Insert the tab control in the middle (between results panel and input wave)
        this.Controls.Add(mSessionsTabControl);
        this.Controls.SetChildIndex(mSessionsTabControl, this.Controls.GetChildIndex(mInputWave));
      }

      // Clear existing tabs
      mSessionsTabControl.TabPages.Clear();

      if (!Directory.Exists(OutputFolder))
      {
        AddErrorMessage($"Output folder not found: [{OutputFolder}]");
        return;
      }

      AddGeneralMessage( $"Loading results from: [{OutputFolder}]");

      var lSessions = Directory.GetDirectories(OutputFolder).OrderBy(d => d);

      if ( lSessions.Count() == 0 )
      {
        AddErrorMessage($"Output folder is empty: [{OutputFolder}]");
        return;
      }

      ShowSession( lSessions.First() ); 
    }

    public void AsyncInvokeAction(Action aAction)
    {
       this.BeginInvoke( aAction );
    }

    private void AddMessage(string aMessage, Color color, FontStyle style = FontStyle.Regular, bool aNewLine = true )
    {
      AsyncInvokeAction( () 
                    => 
                    { 
                        resultsTextBox.SelectionStart = resultsTextBox.TextLength;
                        resultsTextBox.SelectionLength = 0;
                        resultsTextBox.SelectionColor = color;
                        resultsTextBox.SelectionFont = new Font(resultsTextBox.Font, style);
                        resultsTextBox.AppendText(aMessage + (aNewLine ? Environment.NewLine : ""));
                        resultsTextBox.ScrollToCaret(); // This scrolls to bottom
                        resultsTextBox.Invalidate();
                    });
      this.Invalidate();
   }

    public void AddGeneralMessage( string aMsg, bool aNewLine = true )
    {
      AddMessage( aMsg, Color.Black, FontStyle.Regular, aNewLine ) ;
    }

    public void AddErrorMessage( string aMsg, bool aNewLine = true )
    {
      AddMessage("ERROR: " + aMsg, Color.Red, FontStyle.Bold, aNewLine ) ;
    }

    public void AddLogMessage( string aMsg, bool aNewLine = true )
    {
      AddMessage( aMsg, Color.Blue, FontStyle.Regular, aNewLine ) ;
    }

    private void ShowButton_Click(object sender, EventArgs e)
    {
      ShowSessions();
    }

  }
}
