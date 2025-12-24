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

namespace DIGITC2_App
{
  public partial class MainWindow : Window
  {
    
    Settings     mSettings = null ;
    List<Config> mConfigs  = null ;

    private int _samplesLength = 0;

    public MainWindow()
    {
      InitializeComponent();

      mSettings = Settings.FromFile($"{InputFolder}\\Settings.txt");

      mSettings.Set("InputFolder" , InputFolder);  
      mSettings.Set("OutputFolder", OutputFolder);  

      mConfigs  = LoadConfigs();  

      // Bind sliders to initial values
      ZoomSlider.Value = 1.0;
      OffsetSlider.Value = 0;

      ZoomSlider.ValueChanged += (s, e) => UpdateOffsetSliderMax();
      InputWaveView.SizeChanged += (s, e) => UpdateOffsetSliderMax();

      // load existing results if any
      RefreshResultsTabs();
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

          // assign to view
          InputWaveView.Signal = signal;
          InputWaveView.Offset = 0;
          
          // calculate zoom to fit entire signal in available width
          // We want: SamplesPerPixel = 1.0 (1 sample per pixel to fit the signal)
          // SamplesPerPixel = ActualWidth / Zoom, so:
          // 1.0 = ActualWidth / Zoom => Zoom = ActualWidth / 1.0 = ActualWidth
          // But we have signalLength samples, so to fit them all:
          // Zoom = signalLength (so SamplesPerPixel = ActualWidth / signalLength)
          var availableWidth = Math.Max(1.0, InputWaveView.ActualWidth);
          var fittingZoom = signal.Length / availableWidth;
          InputWaveView.Zoom = fittingZoom;
          
          ZoomSlider.Value = fittingZoom;

          // store sample length and update OffsetSlider maximum
          _samplesLength = signal.Length;
          UpdateOffsetSliderMax();
          OffsetSlider.Value = 0;

          StatusText.Text = $"Loaded {Path.GetFileName(dlg.FileName)} ({_samplesLength} samples)";
        }
        catch (Exception ex)
        {
          MessageBox.Show(this, ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
          StatusText.Text = "Error loading file";
        }
      }
    }

    static string InputFolder  = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Input");
    static string OutputFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output\\App");

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
        StatusText.Text = "You must load an AUDIO file first.";
        return;
      }

      var lSession = new Session( "App", mSettings);

      DContext.Setup( lSession ) ;

      try
      {

        var lSignal = new WaveSignal(InputWaveView.Signal) ;

        var lPipeline = PipelineFactory.FromAudioToBits_ByTapCode().Then( PipelineFactory.FromBits() ) ;

        var lResult = Processor.Process( lSession, mSettings, lSession.Name, lPipeline, mConfigs, lSignal);

        lResult.Save( lSession.CurrentOutputFolder )  ;

        StatusText.Text = "Processing started...";

        StatusText.Text = "Processing finished";

        RefreshResultsTabs();
      }
      catch (Exception ex)
      {
        MessageBox.Show(this, ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        StatusText.Text = "Processing error";
      }

      DContext.Shutdown(); 

    }

    private void RefreshResultsTabs()
    {
      ResultsTabControl.Items.Clear();

      if (!Directory.Exists(OutputFolder))
        return;

      var subdirs = Directory.GetDirectories(OutputFolder).OrderBy(d => d).Take(15);
      foreach (var d in subdirs)
      {
        var tab = new TabItem { Header = Path.GetFileName(d) };
        try
        {
          tab.Content = BuildSequenceView(d);
        }
        catch (Exception ex)
        {
          tab.Content = new TextBlock { Text = "Error building view: " + ex.Message };
        }
        ResultsTabControl.Items.Add(tab);
      }

      if (ResultsTabControl.Items.Count == 0)
      {
        var tab = new TabItem { Header = "No Results" };
        tab.Content = new TextBlock { Text = "No results found. Run Process to generate output.", Margin = new Thickness(8) };
        ResultsTabControl.Items.Add(tab);
      }
    }

    // Build the UI for a sequence starting at `rootDir` following single-child chains
    private UIElement BuildSequenceView(string rootDir)
    {
      // collect sequence of folders following single-child chains
      var seq = new List<string>();
      var cur = rootDir;
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

      var waves = new List<string>();
      var texts = new List<string>();

      foreach (var s in seq)
      {
        var lWavResults = Directory.GetFiles(s, "*.wav");
        var lTxtResults = Directory.GetFiles(s, "*.txt");
        waves.AddRange(lWavResults);
        texts.AddRange(lTxtResults);
      }

      // main container: vertical stack: waveforms area on top, text area below
      var main = new DockPanel();

      // Waveforms area
      var wavesPanel = new StackPanel { Orientation = Orientation.Vertical };
      foreach (var lWave in waves)
      {
        var stagePanel = new DockPanel { Margin = new Thickness(4), LastChildFill = true };
        // params panel on the left
        var paramsPanel = BuildParamsPanel(lWave);
        if ( paramsPanel != null)
        {
          paramsPanel.Width = 220;
          DockPanel.SetDock(paramsPanel, Dock.Left);
          stagePanel.Children.Add(paramsPanel);
        }

        // waveform view on right - bind to global zoom and offset
        var wfView = new Controls.WaveformView { Height = 140, Margin = new Thickness(4) };
        try
        {
          wfView.Signal = SignalLoader.LoadSignal(lWave);
          // Bind to the global ZoomSlider and OffsetSlider
          wfView.SetBinding(Controls.WaveformView.ZoomProperty, new System.Windows.Data.Binding { Source = ZoomSlider, Path = new PropertyPath(Slider.ValueProperty), Mode = System.Windows.Data.BindingMode.TwoWay });
          wfView.SetBinding(Controls.WaveformView.OffsetProperty, new System.Windows.Data.Binding { Source = OffsetSlider, Path = new PropertyPath(Slider.ValueProperty), Mode = System.Windows.Data.BindingMode.TwoWay });
        }
        catch (Exception ex)
        {
          // show error in a small textblock if signal load fails
          var err = new TextBlock { Text = "Error loading wav: " + ex.Message };
          stagePanel.Children.Add(err);
        }

        stagePanel.Children.Add(wfView);
        wavesPanel.Children.Add(stagePanel);
      }

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
      DockPanel.SetDock(textsHost, Dock.Bottom);
      main.Children.Add(textsHost);

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
            StatusText.Text = "Params saved: " + Path.GetFileName(paramsFile);
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

    private void UpdateOffsetSliderMax()
    {
      if (_samplesLength <= 0)
      {
        OffsetSlider.Maximum = 0;
        OffsetSlider.IsEnabled = false;
        return;
      }

      OffsetSlider.IsEnabled = true;

      // compute visible samples: ceil(width * samplesPerPixel) where samplesPerPixel = 1/Zoom
      var zoom = Math.Max(0.0001, InputWaveView.Zoom);
      var width = Math.Max(1.0, InputWaveView.ActualWidth);
      var visibleSamples = (int)Math.Ceiling(width / zoom);

      var max = Math.Max(0, _samplesLength - visibleSamples);
      // set maximum while preserving current value
      OffsetSlider.Maximum = max;
      if (OffsetSlider.Value > OffsetSlider.Maximum)
      {
        OffsetSlider.Value = OffsetSlider.Maximum;
      }
    }
  }
}