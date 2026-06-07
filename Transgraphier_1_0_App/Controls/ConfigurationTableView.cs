using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;

using DIGITC2_ENGINE;

namespace Transgraphier_1_0_App
{

  public partial class ConfigurationTableView : UserControl
  {
    private Form1 mMainForm = null ;

    private DataGridView _grid = new();

    private Config? mConfig;
    private List<Param> mParams;

    public ConfigurationTableView( Form1 aMainForm )
    {
      mMainForm = aMainForm;

      InitializeComponent();

      _grid.CellEndEdit += Grid_CellEndEdit;
    }

    private void InitializeComponent()
    {
      _grid = new DataGridView();

      SuspendLayout();

      _grid.Dock = DockStyle.Top;
      _grid.AllowUserToAddRows = false;
      _grid.AllowUserToDeleteRows = false;
      _grid.AllowUserToResizeRows = false;
      _grid.AllowUserToResizeColumns = true;
      _grid.RowHeadersVisible = false;
      _grid.ColumnHeadersVisible = false;
      _grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
      _grid.MultiSelect = false;

      _grid.SelectionChanged += (_, __) =>
      {
          foreach (DataGridViewCell cell in _grid.SelectedCells)
          {
              if (cell.RowIndex == 0)
                  cell.Selected = false;
          }
      };

      Controls.Add(_grid);

      Name = "ConfigurationTableView";
      ResumeLayout(false);
    }

    public void Bind(Config config)
    {
      mConfig = config;
      mParams = mConfig.GetEditableParams();

      _grid.Columns.Clear();
      _grid.Rows.Clear();

      foreach (var p in mParams)
      {
        _grid.Columns.Add(null,null);
      }

      _grid.Rows.Add(2);

      var style = _grid.Rows[0].DefaultCellStyle;
      style.BackColor = SystemColors.Control;
      style.SelectionBackColor = SystemColors.Control;
      style.SelectionForeColor = SystemColors.ControlText;
      style.Font = new Font(_grid.Font, FontStyle.Bold);

      for (int i = 0; i < mParams.Count; ++i)
      {
        var p = mParams[i];

        _grid[i, 0].Value = p.Label;
        _grid[i, 0].ReadOnly = true;
        _grid[i, 0].Style.BackColor = SystemColors.Control;

        _grid[i, 1].Value = p.Value;
      }

      _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
      _grid.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
    }

    private void Grid_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
      if (mConfig == null)
        return;

      if (e.RowIndex != 1)
        return;

      mParams[e.ColumnIndex].Value = _grid[e.ColumnIndex, 1].Value?.ToString() ?? "";

      mMainForm.ParametersChanged(mConfig);
    }
  }

}