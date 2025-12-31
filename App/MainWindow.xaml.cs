using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using NAudio.Wave;
using NWaves.Signals;
using DIGITC2_App.Services;
using DIGITC2_ENGINE;
using System.Windows.Media;
using System.Windows.Documents;
using DIGITC2_App.Controls;

namespace DIGITC2_App
{
  public class WaveViews
  {
    public WaveViews()
    {
    }

    public void Clear() { mViews.Clear(); } 

    public void Add( WaveformView aView) { mViews.Add( aView ); }

    public void Invalidate() { mViews.ForEach(v => v.InvalidateVisual()); }

    List<WaveformView> mViews = new List<WaveformView>();
  }

  public partial class MainWindow : Window
  {
    
    Settings      mSettings  = null ;
    List<Config>  mConfigs   = null ;
    string        mInputFile = null ; 
    MainWindowGUI mMWGUI     = null; 

    WaveViews     mWaveViews = new WaveViews();

    public MainWindow()
    {
      InitializeComponent();

      mMWGUI = new MainWindowGUI(this); 

      mSettings = Settings.FromFile($"{InputFolder}\\Settings.txt");

      mSettings.Set("InputFolder" , InputFolder);  
      mSettings.Set("OutputFolder", OutputFolder);  

      mConfigs  = LoadConfigs();  
    }

    private void ViewButton_Click(object sender, RoutedEventArgs e)
    {
      ShowSessions();
    }

    void SetupZoomPanController()
    {
      var availableWidth = Math.Max(1.0, InputWaveView.ActualWidth);

      var lZPC = new ZoomPanController(){ MinSamplesPerPixel = 2.0
                                        , MaxSamplesPerPixel = InputWaveView.Signal.Length / availableWidth
                                        , Offset = 0 
                                        , ZoomSlider   = this.ZoomSlider
                                        , OffsetSlider = this.OffsetSlider  
                                        , WaveViews    = mWaveViews  
      };
          
      lZPC.SamplesPerPixel = lZPC.MaxSamplesPerPixel ; // Zoom out the entire signal

      InputWaveView.ZoomPanController = lZPC; 

      lZPC.UpdateSliders();
    }

    private void LoadButton_Click(object sender, RoutedEventArgs e)
    {
      var dlg = new OpenFileDialog();
      dlg.Filter = "WAV files (*.wav)|*.wav|All files (*.*)|*.*";
      if (dlg.ShowDialog(this) == true)
      {
        try
        {
          var signal = SignalLoader.LoadSignal(dlg.FileName);

          mInputFile = dlg.FileName ;

          // assign to view
          InputWaveView.Signal = signal;
          SetupZoomPanController();

          mWaveViews.Add( InputWaveView );  

          AddGeneralMessage(  $"Loaded {Path.GetFileName(dlg.FileName)} ({signal.Length} samples)" );

          mWaveViews.Invalidate();

        }
        catch (Exception ex)
        {
          MessageBox.Show(this, ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
          AddGeneralMessage( "Error loading file" );
        }
      }
    }

    static string InputFolder  = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Input");
    static string OutputFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");

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


    private void ProcessButton_Click(object sender, RoutedEventArgs e)
    {
      if ( InputWaveView.Signal == null)
      {
        AddGeneralMessage( "You must load an AUDIO file first." );
        return;
      }

      ResultsTabControl.Items.Clear();

      var lSessionName = Path.GetFileNameWithoutExtension(mInputFile);

      var lSession = new Session( lSessionName, mSettings, mMWGUI );

      DContext.Setup( lSession ) ;

      try
      {
        File.Copy( mInputFile, Path.Combine( lSession.CurrentOutputFolder, lSessionName + ".wav" ), true );

        var lSignal = new WaveSignal(InputWaveView.Signal) ;

        var lPipeline = PipelineFactory.FromAudioToBits_ByTapCode().Then( PipelineFactory.FromBits() ) ;

        //InvokeAction ( () => {

          var lResult = Processor.Process( lSession, mSettings, lSession.Name, lPipeline, mConfigs, lSignal);

          lResult.Save( lSession.CurrentOutputFolder )  ;
        //}) ;

        ShowSessions();
      }
      catch (Exception ex)
      {
        AddGeneralMessage( "Processing error" );
      }

      DContext.Shutdown(); 

    }

    private void ShowSessions()
    {
      ResultsTabControl.Items.Clear();
      mWaveViews.Clear();

      if (!Directory.Exists(OutputFolder))
        return;

      AddGeneralMessage( "Loading results from: " + OutputFolder);

      var lSessions = Directory.GetDirectories(OutputFolder).OrderBy(d => d);

      if ( lSessions.Count() == 0 )
        return ;

      var lFirstSession = lSessions.First(); 

      mInputFile = Directory.GetFiles(lFirstSession, "*.wav")[0];

      DContext.Assert(mInputFile != null, "Input Wave file not found.");

      InputWaveView.Signal = SignalLoader.LoadSignal(mInputFile);
      SetupZoomPanController();
      mWaveViews.Add( InputWaveView );  

      var lRootTab = new TabItem { Header = Path.GetFileNameWithoutExtension(lFirstSession) };
      try
      {
        lRootTab.Content = BuildSequenceView(lFirstSession);
      }
      catch (Exception ex)
      {
        lRootTab.Content = new TextBlock { Text = "Error building view: " + ex.Message };
      }
      ResultsTabControl.Items.Add(lRootTab);

      AddGeneralMessage( "Finished loading results.");

      //if (ResultsTabControl.Items.Count == 0)
      //{
      //  var tab2 = new TabItem { Header = "No Results" };
      //  tab2.Content = new TextBlock { Text = "No results found. Run Process to generate output.", Margin = new Thickness(8) };
      //  ResultsTabControl.Items.Add(tab2);
      //}

      ResultsTabControl.InvalidateVisual();
      mWaveViews.Invalidate();  
      this.InvalidateVisual();
    }


    // Build the UI for a sequence starting at `rootDir` following single-child chains
    private UIElement BuildSequenceView(string aRootDir)
    {
      // collect sequence of folders following single-child chains
      var seq = new List<string>();
      var cur = $"{aRootDir}\\Pipeline_0";
      while (true)
      {
        seq.Add(cur);
        var children = Directory.GetDirectories(cur).OrderBy(x => x).ToArray();
        if (children.Length == 1)
        {
          cur = children[0];
          continue;
        }
        break;
      }

      var lResultWaves = new List<string>();
      var texts = new List<string>();

      foreach (var s in seq)
      {
        var lWavResults = Directory.GetFiles(s, "*.wav");
        var lTxtResults = Directory.GetFiles(s, "*.txt");
        lResultWaves.AddRange(lWavResults);
        texts.AddRange(lTxtResults);
      }

      // main container: vertical stack: waveforms area on top, text area below
      var main = new DockPanel();

      // Waveforms area
      var wavesPanel = new StackPanel { Orientation = Orientation.Vertical };
      foreach (var lResultWave in lResultWaves)
      {
        var stagePanel = new DockPanel { Margin = new Thickness(4), LastChildFill = true };

        // waveform view on right - bind to global zoom and offset
        var lResultWaveView = new Controls.WaveformView { Height = 120, Margin = new Thickness(4), ZoomPanController =  this.InputWaveView.ZoomPanController };
        try
        {
          lResultWaveView.Signal = SignalLoader.LoadSignal(lResultWave);

          lResultWaveView.BackgroundBrush   = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFF5F0E8"));
          lResultWaveView.WaveformPenBrush  = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4169E1"));
          lResultWaveView.GridLineBrush     = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#B0C4DE"));

          mWaveViews.Add( lResultWaveView );  
        }
        catch (Exception ex)
        {
          // show error in a small textblock if signal load fails
          var err = new TextBlock { Text = "Error loading wav: " + ex.Message };
          stagePanel.Children.Add(err);
        }

        stagePanel.Children.Add(lResultWaveView);

        var paramsPanel = BuildParamsPanel(lResultWave);
        if ( paramsPanel != null)
        {
          paramsPanel.Width = 220;
          DockPanel.SetDock(paramsPanel, Dock.Right);
          stagePanel.Children.Add(paramsPanel);
        }

        wavesPanel.Children.Add(stagePanel);
      }

      //var scrollWaves = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
      //scrollWaves.Content = wavesPanel;
      //DockPanel.SetDock(scrollWaves, Dock.Top);
      //main.Children.Add(scrollWaves);
      DockPanel.SetDock(wavesPanel, Dock.Top);
      main.Children.Add(wavesPanel);

      // Text area (vertical)
      var textsHost = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
      var textsPanel = new StackPanel { Orientation = Orientation.Vertical };

      foreach (var lText in texts)
      {
        var itemBorder = new Border { BorderBrush = System.Windows.Media.Brushes.Gray, BorderThickness = new Thickness(1), Margin = new Thickness(4) };
        var itemGrid = new Grid { Height = 200 };
        itemGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) });
        itemGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var paramsPanel = BuildParamsPanel(lText);
        if ( paramsPanel != null )
        {
          paramsPanel.Margin = new Thickness(4);
          Grid.SetRow(paramsPanel, 0);
          itemGrid.Children.Add(paramsPanel);
        }

        var textBox = new TextBox { IsReadOnly = true, AcceptsReturn = true, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4) };
        try
        {
          textBox.Text = File.ReadAllText(lText);
        }
        catch (Exception ex)
        {
          textBox.Text = "Error reading text file: " + ex.Message;
        }
        Grid.SetRow(textBox, 1);
        itemGrid.Children.Add(textBox);

        itemBorder.Child = itemGrid;
        textsPanel.Children.Add(itemBorder);
      }

      textsHost.Content = textsPanel;
      DockPanel.SetDock(textsHost, Dock.Top);
      main.Children.Add(textsHost);

      //main.LastChildFill = true;

      return main;
    }

    // Build a params editor panel for the given stage folder. Looks for a params file and allows editing and saving.
    private FrameworkElement BuildParamsPanel(string stageFolder)
    {
return null ;

      var panel = new StackPanel { Margin = new Thickness(4) };

      var header = new TextBlock { Text = Path.GetFileName(stageFolder), FontWeight = FontWeights.Bold, Margin = new Thickness(2) };
      panel.Children.Add(header);

      // find params file: prefer params.txt or params.ini or *.params
      var paramsFile = Directory.GetFiles(stageFolder, "params.txt").FirstOrDefault()
                       ?? Directory.GetFiles(stageFolder, "params.ini").FirstOrDefault()
                       ?? Directory.GetFiles(stageFolder, "*.params").FirstOrDefault()
                       ?? Directory.GetFiles(stageFolder, "params.cfg").FirstOrDefault();

      if (paramsFile == null)
      {
        // try fallback: any .txt that is not result.txt
        paramsFile = Directory.GetFiles(stageFolder, "*.txt").Where(f => Path.GetFileName(f).ToLowerInvariant() != "result.txt").FirstOrDefault();
      }

      var entries = new List<KeyValuePair<string, string>>();
      if (paramsFile != null && File.Exists(paramsFile))
      {
        try
        {
          var lines = File.ReadAllLines(paramsFile);
          foreach (var line in lines)
          {
            var txt = line.Trim();
            if (string.IsNullOrEmpty(txt) || txt.StartsWith("#"))
              continue;
            var idx = txt.IndexOf('=');
            if (idx > 0)
            {
              var k = txt.Substring(0, idx).Trim();
              var v = txt.Substring(idx + 1).Trim();
              entries.Add(new KeyValuePair<string, string>(k, v));
            }
          }
        }
        catch
        {
          // ignore parse errors
        }
      }

      var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Height = 140 };
      var kvStack = new StackPanel();

      var controls = new List<(string Key, TextBox ValueBox)>();
      foreach (var kv in entries)
      {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(2) };
        var keyTb = new TextBlock { Text = kv.Key + ":", Width = 110, VerticalAlignment = VerticalAlignment.Center };
        var valBox = new TextBox { Text = kv.Value, Width = 90 };
        row.Children.Add(keyTb);
        row.Children.Add(valBox);
        kvStack.Children.Add(row);
        controls.Add((kv.Key, valBox));
      }

      scroll.Content = kvStack;
      panel.Children.Add(scroll);

      var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(2) };
      var saveBtn = new Button { Content = "Save", Margin = new Thickness(2), IsEnabled = paramsFile != null };
      btnPanel.Children.Add(saveBtn);

      if (paramsFile != null)
      {
        var openBtn = new Button { Content = "Open", Margin = new Thickness(2) };
        btnPanel.Children.Add(openBtn);
        openBtn.Click += (_, __) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(paramsFile) { UseShellExecute = true });

        saveBtn.Click += (_, __) =>
        {
          try
          {
            var lines = controls.Select(c => c.Key + "=" + c.ValueBox.Text).ToArray();
            File.WriteAllLines(paramsFile, lines);
            AddGeneralMessage( "Params saved: " + Path.GetFileName(paramsFile) ) ;
          }
          catch (Exception ex)
          {
            MessageBox.Show(this, ex.Message, "Error saving params", MessageBoxButton.OK, MessageBoxImage.Error);
          }
        };
      }

      panel.Children.Add(btnPanel);

      if (paramsFile == null)
      {
        panel.Children.Add(new TextBlock { Text = "No params file found", Margin = new Thickness(2) });
      }

      return panel;
    }

    public void InvokeAction(Action aAction)
    {
       Application.Current?.Dispatcher.InvokeAsync(() => aAction(), System.Windows.Threading.DispatcherPriority.Background);
    }

    private void AddMessage(string aMessage, FontWeight aFW, Brush aColor, bool aNewLine )
    {
      InvokeAction( () 
                    => 
                    { 
                      StatusText.Inlines.Add( new Run(aMessage + ( aNewLine ? Environment.NewLine : "" ) ) { FontSize = 12, FontWeight = aFW, Foreground = aColor }) ;
                      StatusText.InvalidateVisual();
                      (StatusText.Parent as FrameworkElement)?.InvalidateVisual();
                      StatusTextScroll.ScrollToBottom();
                      StatusTextScroll.InvalidateVisual();  
                    }
                    );
      this.InvalidateVisual();
   }

    public void AddGeneralMessage( string aMsg, bool aNewLine = true )
    {
      AddMessage( aMsg, FontWeights.Regular, Brushes.Black, aNewLine ) ;
    }

    public void AddErrorMessage( string aMsg, bool aNewLine = true )
    {
      AddMessage("ERROR: " + aMsg, FontWeights.Bold, Brushes.Red, aNewLine ) ;
    }

    public void AddLogMessage( string aMsg, bool aNewLine = true )
    {
      AddMessage( aMsg, FontWeights.Regular, Brushes.Blue, aNewLine ) ;
    }
  }

  public class MainWindowGUI : GUI
  {
    public MainWindowGUI( MainWindow aMainWindow)
    { 
      mMainWindow = aMainWindow ; 
    }

    public override void AddMessage     ( string aMsg ) => mMainWindow.AddGeneralMessage(aMsg,true);
    public override void AddErrorMessage( string aMsg ) => mMainWindow.AddErrorMessage  (aMsg,true);

    MainWindow mMainWindow; 
  }

}