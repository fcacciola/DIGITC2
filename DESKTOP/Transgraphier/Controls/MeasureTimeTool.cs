using System;
using System.Windows.Forms;

namespace Transgraphier
{
  public class MeasureTimeTool
  {
    public bool IsActive { get; set; }

    private int mSelectionStartSample = -1;
    private int mSelectionEndSample   = -1;

    int mSelectionStartX = -1;
    int mSelectionEndX = -1;

    public int SelectionStartX { get => mSelectionStartX; set { mSelectionStartX = value; } }
    public int SelectionEndX   { get => mSelectionEndX;   set { mSelectionEndX   = value; } }

    public int SelectionStartSample 
    { 
      get => mSelectionStartSample;
      set 
      { 
        mSelectionStartSample = value;
        OnSelectionChanged();
      }
    }

    public int SelectionEndSample 
    { 
      get => mSelectionEndSample;
      set 
      { 
        mSelectionEndSample = value;
        OnSelectionChanged();
      }
    }

    public Action<MeasureTimeTool> OnSelectionChangedCallback { get; set; }

    public void Reset()
    {
      mSelectionStartSample = -1;
      mSelectionEndSample   = -1;
      mSelectionStartX      = -1;
      mSelectionEndX        = -1;
    }

    public bool HasSelection => mSelectionStartSample >= 0 && mSelectionEndSample >= 0;

    public double GetDurationInSeconds(int samplingRate)
    {
      if (!HasSelection)
        return 0;

      int startSample = Math.Min(mSelectionStartSample, mSelectionEndSample);
      int endSample   = Math.Max(mSelectionStartSample, mSelectionEndSample);

      int sampleCount = endSample - startSample;
      return (double)sampleCount / samplingRate;
    }

    public string GetFormattedDuration(int samplingRate)
    {
      double seconds = GetDurationInSeconds(samplingRate);
      if (seconds <= 0)
        return "0.000s";

      return $"{seconds:F3}s";
    }

    private void OnSelectionChanged()
    {
      OnSelectionChangedCallback?.Invoke(this);
    }
  }
}

