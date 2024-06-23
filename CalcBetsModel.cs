using Har_reader.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Har_reader
{
    public interface IBalanceDataSender
    {
        public delegate void DataDelegate(double balance, int? currLoseStrike, double? currBetAcc);
        public event DataDelegate DataUpdated;
    }
    public class CalcBetsModel : Proper, IBalanceDataSender
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

        public event IBalanceDataSender.DataDelegate DataUpdated;

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

        private double? DataBalance { get; set; }
        private int? DataStep { get; set; }
        private double? CurrBet { get; set; }
        public CalcBetsModel()
        {
            CalcPrediction();
            DataUpdated += CalcBetsModel_DataUpdated;
        }

        private void CalcBetsModel_DataUpdated(double balance, int? currLoseStrike, double? currBetAcc)
        {
            DataBalance = balance;
            DataStep = currLoseStrike;
            CurrBet = currBetAcc;
            if (UseData)
            {
                StepS1 = DataStep != null ? (int)DataStep : StepS1;
                BetS1 = CurrBet != null ? (double)CurrBet : BetS1;
            }
            CalcPrediction();
        }

        public void CalcPrediction()
        {
            int i = 1;//StepS1;
            List<double> bets = new List<double>();
            bets.Add(BetS1);
            List<double> sums = new List<double>();
            sums.Add(BetS1);
            List<int> steps = new List<int>();
            steps.Add(StepS1);
            //рассчитать на 25 шагов и не париться? будет выдавать 100шагов от указанного шага (по ставке)
            while (true)
            {
                //??? BAG IF STEP NOT 1
                steps.Add(steps[i - 1] + 1);
                double b = bets[i - 1] * Multiply;
                bets.Add(b);
                sums.Add(sums[i - 1] + b);
                i++;
                if (i > 25)
                    break;
            }
            double over = 0.0d;
            int indx = 0;
            if (DataBalance != null)
            {
                over = sums.Where(t => t > DataBalance).First();
            }
            else
            {
                over = sums.Last();
            }
            indx = sums.IndexOf(over);
            StepS3 = steps[indx];
            StepS2 = indx > 0 ? steps[indx - 1] : steps[0];
            SummS3 = sums[indx];
            SummS2 = indx > 0 ? sums[indx - 1] : sums[0];
            BetS3 = bets[indx];
            BetS2 = indx > 0 ? bets[indx - 1] : bets[0];
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
