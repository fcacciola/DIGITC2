using System;
using System.IO;
using System.Windows.Forms;

namespace Transgraphier_1_0_App
{
    public partial class Form1 : Form
    {
        private TabControl sessionsTabControl;

        public Form1()
        {
            InitializeComponent();
        }

        public void ShowSessions()
        {
            // Create tab control if it doesn't exist
            if (sessionsTabControl == null)
            {
                sessionsTabControl = new TabControl();
                sessionsTabControl.Dock = DockStyle.Fill;
                sessionsTabControl.Name = "sessionsTabControl";

                // Insert the tab control in the middle (between results panel and input wave)
                this.Controls.Add(sessionsTabControl);
                this.Controls.SetChildIndex(sessionsTabControl, this.Controls.GetChildIndex(inputWave));
            }

            // Clear existing tabs
            sessionsTabControl.TabPages.Clear();

            // Get output folder path (assumes output folder in the application directory)
            string outputFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output");

            if (!Directory.Exists(outputFolder))
            {
                statusTextBox.AppendText($"Output folder not found: {outputFolder}\r\n");
                return;
            }

            // Get all .wav files in the output folder
            string[] wavFiles = Directory.GetFiles(outputFolder, "*.wav");

            if (wavFiles.Length == 0)
            {
                statusTextBox.AppendText($"No .wav files found in output folder: {outputFolder}\r\n");
                return;
            }

            // Create a tab for each .wav file
            foreach (string wavFilePath in wavFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(wavFilePath);
                
                // Create tab page
                TabPage tabPage = new TabPage();
                tabPage.Text = fileName;
                tabPage.Name = $"tab_{fileName}";

                // Create WaveFormView for this file
                WaveFormView waveformView = new WaveFormView();
                waveformView.Dock = DockStyle.Fill;
                waveformView.Title = fileName;
                waveformView.InfoText = GetWaveFileInfo(wavFilePath);

                // Add waveform view to tab page
                tabPage.Controls.Add(waveformView);

                // Add tab to tab control
                sessionsTabControl.TabPages.Add(tabPage);
            }

            statusTextBox.AppendText($"Loaded {wavFiles.Length} session(s) from {outputFolder}\r\n");
        }

        private string GetWaveFileInfo(string filePath)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(filePath);
                return $"File: {fileInfo.Name}\r\n" +
                       $"Size: {FormatBytes(fileInfo.Length)}\r\n" +
                       $"Modified: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}";
            }
            catch (Exception ex)
            {
                return $"Error reading file info:\r\n{ex.Message}";
            }
        }

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
