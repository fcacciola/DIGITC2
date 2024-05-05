public static List<Peak> FindLocalPeaks(int[] data, double prominence = 0, double threshold = Double.MinValue)
{
    List<Peak> peaks = new List<Peak>();
    int lastPeakIndex = -1;

    for (int i = 1; i < data.Length - 1; i++)
    {
        // Is it a peak (local maximum)?
        if (data[i] > data[i - 1] && data[i] > data[i + 1])
        {
            // Check for prominence (difference from nearby maxima)
            bool isProminent = true;
            if (prominence > 0)
            {
                if (lastPeakIndex != -1 && data[i] - data[lastPeakIndex] < prominence)
                {
                    isProminent = false;
                }
                else if (i - data.Length >= prominence) // Check next 'prominence' elements to the right
                {
                    for (int j = 1; j <= prominence; j++)
                    {
                        if (data[i] - data[i + j] < prominence)
                        {
                            isProminent = false;
                            break;
                        }
                    }
                }
            }

            // Check for threshold (minimum peak value)
            if (isProminent && data[i] >= threshold)
            {
                peaks.Add(new Peak(i, data[i]));
                lastPeakIndex = i;
            }
        }
    }

    return peaks;
}

public class Peak
{
    public int Index { get; private set; }
    public double Value { get; private set; }

    public Peak(int index, double value)
    {
        Index = index;
        Value = value;
    }
}
