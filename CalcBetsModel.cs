using Har_reader.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Har_reader
{
    public class CalcBetsModel : Proper
    {
        private int stepS1 = 1;
        private int stepS2;
        private int stepS3;
        private double betS1 = 0.4;
        private double betS2;
        private double betS3;
        private double sumS2;
        private double sumS3;

        private double multiply = 2;
        private bool useData;

        public double Multiply { get => multiply; set { SetProperty(ref multiply, value); CalcPrediction(); } }
        public bool UseData { get => useData; set { SetProperty(ref useData, value); CalcPrediction(); } }

        public double BetS1 { get => betS1; set { SetProperty(ref betS1, value); CalcPrediction(); } }
        public double BetS2 { get => betS2; set => SetProperty(ref betS2, value); }
        public double BetS3 { get => betS3; set => SetProperty(ref betS3, value); }
        public double SummS2 { get => sumS2; set => SetProperty(ref sumS2, value); }
        public double SummS3 { get => sumS3; set => SetProperty(ref sumS3, value); }
        public int StepS1 { get => stepS1; set { SetProperty(ref stepS1, value); CalcPrediction(); } }
        public int StepS2 { get => stepS2; set => SetProperty(ref stepS2, value); }
        public int StepS3 { get => stepS3; set => SetProperty(ref stepS3, value); }
        public CalcBetsModel()
        {
            CalcPrediction();
        }
        public void CalcPrediction(double? balance = null)
        {
            int i = StepS1;
            List<double> bets = new List<double>();
            bets.Add(BetS1);
            List<double> sums = new List<double>();
            sums.Add(BetS1);
            List<int> steps = new List<int>();
            while (true)
            {
                ??? BAG IF STEP NOT 1
                steps.Add(i);
                double b = bets[i - 1] * Multiply;
                bets.Add(b);
                sums.Add(sums[i - 1] + b);
                i++;
                if (balance != null)
                {
                    if (sums.Last() > balance)
                        break;
                }
                else if (i > 15)
                    break;
            }
            StepS3 = steps.Last();
            StepS2 = steps[steps.IndexOf(StepS3) - 1];
            BetS3 = bets.Last();
            BetS2 = bets[bets.IndexOf(BetS3) - 1];
            SummS3 = sums.Last();
            SummS2 = sums[sums.IndexOf(SummS3) - 1];
        }
        public void SaveSettings()
        {
            //Settings.Default.AlertPathWav = AlertPath;
            //Settings.Default.LosePathWav = LosePath;
            //Settings.Default.WinPathWav = WinPath;
            //Settings.Default.AlertUseDef = DefAlertChecked;
            //Settings.Default.WinUseDef = DefWinChecked;
            //Settings.Default.LoseUseDef = DefLoseChecked;
            //Settings.Default.Save();
        }
    }
}
