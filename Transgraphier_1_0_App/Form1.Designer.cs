namespace Transgraphier_1_0_App
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            buttonPanel = new Panel();
            loadButton = new Button();
            processButton = new Button();
            showButton = new Button();
            statusTextBox = new TextBox();
            statusPanel = new Panel();
            resultsTextBox = new RichTextBox();
            resultsPanel = new Panel();
            mInputWave = new WaveView();
            buttonPanel.SuspendLayout();
            SuspendLayout();
            // 
            // buttonPanel
            // 
            buttonPanel.Controls.Add(loadButton);
            buttonPanel.Controls.Add(processButton);
            buttonPanel.Controls.Add(showButton);
            buttonPanel.Dock = DockStyle.Top;
            buttonPanel.Location = new Point(0, 0);
            buttonPanel.Name = "buttonPanel";
            buttonPanel.Size = new Size(1920, 50);
            buttonPanel.TabIndex = 1;
            // 
            // loadButton
            // 
            loadButton.Location = new Point(10, 10);
            loadButton.Name = "loadButton";
            loadButton.Size = new Size(100, 30);
            loadButton.TabIndex = 0;
            loadButton.Text = "LOAD";
            // 
            // processButton
            // 
            processButton.Location = new Point(120, 10);
            processButton.Name = "processButton";
            processButton.Size = new Size(100, 30);
            processButton.TabIndex = 1;
            processButton.Text = "PROCESS";
            // 
            // showButton
            // 
            showButton.Location = new Point(230, 10);
            showButton.Name = "showButton";
            showButton.Size = new Size(100, 30);
            showButton.TabIndex = 2;
            showButton.Text = "SHOW";
            showButton.Click += ShowButton_Click;
            // 
            // resultsPanel
            // 
            resultsPanel.BackColor = Color.Black;
            resultsPanel.Controls.Add(resultsTextBox);
            resultsPanel.Dock = DockStyle.Top;
            resultsPanel.Location = new Point(0, 50);
            resultsPanel.Name = "resultsPanel";
            resultsPanel.Padding = new Padding(2);
            resultsPanel.Size = new Size(1920, 300);
            resultsPanel.TabIndex = 3;
            // 
            // resultsTextBox
            // 
            resultsTextBox.Dock = DockStyle.Fill;
            resultsTextBox.Location = new Point(2, 2);
            resultsTextBox.Name = "resultsTextBox";
            resultsTextBox.ReadOnly = true;
            resultsTextBox.ScrollBars = RichTextBoxScrollBars.Vertical;
            resultsTextBox.Size = new Size(1916, 296);
            resultsTextBox.TabIndex = 1;
            resultsTextBox.Text = "";
            // 
            // inputWave
            // 
            mInputWave.Dock = DockStyle.Top;
            mInputWave.Location = new Point(8, 350);
            mInputWave.Name = "inputWave";
            mInputWave.Size = new Size(1920, 150);
            mInputWave.TabIndex = 4;
            mInputWave.Title = "Input Wave";
            // 
            // statusPanel
            // 
            statusPanel.BackColor = Color.Black;
            statusPanel.Controls.Add(statusTextBox);
            statusPanel.Dock = DockStyle.Bottom;
            statusPanel.Location = new Point(0, 980);
            statusPanel.Name = "statusPanel";
            statusPanel.Padding = new Padding(2);
            statusPanel.Size = new Size(1920, 100);
            statusPanel.TabIndex = 2;
            // 
            // statusTextBox
            // 
            statusTextBox.Dock = DockStyle.Fill;
            statusTextBox.Location = new Point(2, 2);
            statusTextBox.Multiline = true;
            statusTextBox.Name = "statusTextBox";
            statusTextBox.ReadOnly = true;
            statusTextBox.ScrollBars = ScrollBars.Vertical;
            statusTextBox.Size = new Size(1916, 96);
            statusTextBox.TabIndex = 0;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1920, 1080);
            Controls.Add(statusPanel);
            Controls.Add(mInputWave);
            Controls.Add(resultsPanel);
            Controls.Add(buttonPanel);
            Name = "Form1";
            Text = "Transgraphier 1.0";
            WindowState = FormWindowState.Maximized;
            buttonPanel.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel buttonPanel;
        private Button loadButton;
        private Button processButton;
        private Button showButton;
        private Panel statusPanel;
        private TextBox statusTextBox;
        private Panel resultsPanel;
        private RichTextBox resultsTextBox;
        private WaveView mInputWave;
    }
}
