using System;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;

namespace Transgraphier_1_0_App
{
    public class WaveFormView : UserControl
    {
        private Panel containerPanel;
        private TextBox infoTextBox;
        private WaveFormPanel waveformPanel;
        private Label titleLabel;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Title
        {
            get { return titleLabel.Text; }
            set { titleLabel.Text = value; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string InfoText
        {
            get { return infoTextBox.Text; }
            set { infoTextBox.Text = value; }
        }

        public WaveFormView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // Main container panel with black border
            containerPanel = new Panel();
            containerPanel.BackColor = Color.Black;
            containerPanel.Dock = DockStyle.Fill;
            containerPanel.Padding = new Padding(2);

            // Title label
            titleLabel = new Label();
            titleLabel.Text = "Title";
            titleLabel.Dock = DockStyle.Top;
            titleLabel.Height = 25;
            titleLabel.BackColor = Color.LightGray;
            titleLabel.ForeColor = Color.Black;
            titleLabel.TextAlign = ContentAlignment.MiddleLeft;
            titleLabel.Padding = new Padding(5, 0, 0, 0);

            // Inner container (white background for content)
            Panel innerContainer = new Panel();
            innerContainer.BackColor = Color.White;
            innerContainer.Dock = DockStyle.Fill;

            // Left info text box (100 width)
            infoTextBox = new TextBox();
            infoTextBox.Dock = DockStyle.Left;
            infoTextBox.Width = 100;
            infoTextBox.ReadOnly = true;
            infoTextBox.Multiline = true;
            infoTextBox.ScrollBars = ScrollBars.None;
            infoTextBox.BorderStyle = BorderStyle.Fixed3D;

            // Right waveform panel
            waveformPanel = new WaveFormPanel();
            waveformPanel.Dock = DockStyle.Fill;

            // Add controls
            innerContainer.Controls.Add(waveformPanel);
            innerContainer.Controls.Add(infoTextBox);

            containerPanel.Controls.Add(innerContainer);
            containerPanel.Controls.Add(titleLabel);

            this.Controls.Add(containerPanel);
            this.Height = 100;
        }
    }

    public class WaveFormPanel : Control
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Sample: Draw a simple polyline (waveform)
            using (Pen pen = new Pen(Color.Blue, 2))
            {
                Point[] points = new Point[10];
                int height = this.Height;
                int width = this.Width;
                
                for (int i = 0; i < points.Length; i++)
                {
                    float x = (float)i / (points.Length - 1) * width;
                    float y = height / 2 + (float)Math.Sin(i * Math.PI / (points.Length - 1)) * height / 4;
                    points[i] = new Point((int)x, (int)y);
                }

                if (points.Length > 1)
                {
                    e.Graphics.DrawLines(pen, points);
                }
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(Color.White);
        }
    }
}
