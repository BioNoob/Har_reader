using System.Collections.Generic;

namespace Har_reader.Models
{
    public class MedianModel : Proper
    {
        private int counter;
        private double calcMedian;
        private double medianT;
        private double medianH;
        private double medianFH;
        public delegate void RequestNewMedian();
        public event RequestNewMedian RequestMedianEvent;
        public MedianModel()
        {
            CalcMedian = MedianT = MedianH = MedianFH = 0.0;
            Counter = Properties.Settings.Default.MedianCount;
        }
        public int Counter
        {
            get => counter;
            set
            {
                SetProperty(ref counter, value);
                RequestMedianEvent?.Invoke();
            }
        }
        public double CalcMedian { get => calcMedian; set => SetProperty(ref calcMedian, value); }
        public double MedianT { get => medianT; set => SetProperty(ref medianT, value); }
        public double MedianH { get => medianH; set => SetProperty(ref medianH, value); }
        public double MedianFH { get => medianFH; set => SetProperty(ref medianFH, value); }

        public void SetMedians(Dictionary<GoogleApi.CalcMedType, double> vals)
        {
            CalcMedian = vals[GoogleApi.CalcMedType.Calculated];
            MedianT = vals[GoogleApi.CalcMedType.Thous];
            MedianH = vals[GoogleApi.CalcMedType.Hundr];
            MedianFH = vals[GoogleApi.CalcMedType.FiveHundr];
        }
        public void SaveSettings()
        {
            Properties.Settings.Default.MedianCount = Counter;
            Properties.Settings.Default.Save();
        }
    }
}
