using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;

namespace Transgraphier_1_0_App
{
  public class ConfigurationTableView : UserControl
  {
    private DataGridView mDataGridView;
    private DataGridViewTextBoxColumn keyColumn;
    private DataGridViewTextBoxColumn valueColumn;
    private Dictionary<string, string> mData;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Dictionary<string, string> Data
    {
      get
      {
        return mData;
      }
      set
      {
        mData = value;
        RefreshTable();
      }
    }

    public ConfigurationTableView()
    {
      InitializeComponent();
    }

    private void InitializeComponent()
    {
      DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
      mDataGridView = new DataGridView();
      keyColumn = new DataGridViewTextBoxColumn();
      valueColumn = new DataGridViewTextBoxColumn();
      ((ISupportInitialize)mDataGridView).BeginInit();
      SuspendLayout();
      // 
      // mDataGridView
      // 
      mDataGridView.AllowUserToAddRows = false;
      mDataGridView.AllowUserToDeleteRows = false;
      mDataGridView.BackgroundColor = Color.White;
      mDataGridView.BorderStyle = BorderStyle.Fixed3D;
      mDataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
      mDataGridView.Columns.AddRange(new DataGridViewColumn[] { keyColumn, valueColumn });
      mDataGridView.Dock = DockStyle.Fill;
      mDataGridView.Location = new Point(0, 0);
      mDataGridView.Name = "ConfigurationTableView";
      mDataGridView.RowHeadersVisible = false;
      mDataGridView.Size = new Size(300, 150);
      mDataGridView.TabIndex = 0;
      mDataGridView.CellEndEdit += DataGridView_CellEndEdit;
      // 
      // keyColumn
      // 
      keyColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
      dataGridViewCellStyle1.BackColor = Color.LightGray;
      keyColumn.DefaultCellStyle = dataGridViewCellStyle1;
      keyColumn.HeaderText = "Parameter";
      keyColumn.MinimumWidth = 8;
      keyColumn.Name = "keyColumn";
      keyColumn.ReadOnly = true;
      keyColumn.Width = 200;
      // 
      // valueColumn
      // 
      valueColumn.HeaderText = "Value";
      valueColumn.MinimumWidth = 8;
      valueColumn.Name = "valueColumn";
      // 
      // ConfigurationTableView
      // 
      Controls.Add(mDataGridView);
      Name = "ConfigurationTableView";
      Size = new Size(300, 150);
      ((ISupportInitialize)mDataGridView).EndInit();
      ResumeLayout(false);
    }

    private void RefreshTable()
    {
      mDataGridView.Rows.Clear();

      if (mData == null)
        return;

      foreach (var kvp in mData)
      {
        int rowIndex = mDataGridView.Rows.Add(kvp.Key, kvp.Value);
      }
    }

    private void DataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
    {
      if (e.ColumnIndex == 1 && e.RowIndex >= 0 && e.RowIndex < mDataGridView.Rows.Count)
      {
        string key = mDataGridView.Rows[e.RowIndex].Cells[0].Value?.ToString();
        string newValue = mDataGridView.Rows[e.RowIndex].Cells[1].Value?.ToString() ?? "";

        if (!string.IsNullOrEmpty(key) && mData != null && mData.ContainsKey(key))
        {
          mData[key] = newValue;
        }
      }
    }

    public void CommitChanges()
    {
      // This method can be called to ensure all pending edits are committed
      if (mDataGridView.CurrentCell != null)
      {
        mDataGridView.EndEdit();
      }
    }
  }
}
