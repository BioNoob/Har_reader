using Har_reader.Properties;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;

namespace Har_reader
{
    public class CalcIteration : Proper
    {
        int step;
        double bet;
        bool isred;
        double sum;
        public int Step { get => step; set => SetProperty(ref step, value); }
        public double Sum { get => sum; set => SetProperty(ref sum, value); }
        public double Bet { get => bet; set => SetProperty(ref bet, value); }
        public bool IsRed { get => isred; set => SetProperty(ref isred, value); }
        public CalcIteration CalcNext(double multiply)
        {
            CalcIteration c = new CalcIteration();
            c.Step = Step + 1;
            c.Bet = Bet * multiply;
            c.Sum = Sum + c.Bet;
            return c;
        }
    }
    public class CalcBetsModel : Proper
    {
        private int stepS1 = 1;
        private double betS1 = 0.4;
        private double multiply = 2;
        private bool useData;
        private int maxStep = 10;
        private ObservableCollection<CalcIteration> calculations;
        public ObservableCollection<CalcIteration> Calculations { get => calculations; set => SetProperty(ref calculations, value); }

        public int MaxStep { get => maxStep; set => SetProperty(ref maxStep, value); }

        public double Multiply { get => multiply; set { SetProperty(ref multiply, value); CalcPrediction(); } }
        public bool UseData { get => useData; set { SetProperty(ref useData, value); CalcPrediction(); } }

        public double BetS1 { get => betS1; set { SetProperty(ref betS1, value); CalcPrediction(); } }
        public int StepS1 { get => stepS1; set { SetProperty(ref stepS1, value); CalcPrediction(); } }

        private double? DataBalance { get; set; }
        private int? DataStep { get; set; }
        private double? CurrBet { get; set; }
        public CalcBetsModel()
        {
            Calculations = new ObservableCollection<CalcIteration>();
            object lockobj = new object();
            BindingOperations.EnableCollectionSynchronization(Calculations, lockobj);
            BetS1 = Settings.Default.CalcBet;
            StepS1 = Settings.Default.CalcStep;
            Multiply = Settings.Default.CalcMulty;
            MaxStep = Settings.Default.CalcMaxStep;

            CalcPrediction();
        }

        public void SetData(double balance, int? currLoseStrike, double? currBetAcc)
        {
            DataBalance = balance;
            DataStep = currLoseStrike + 1;
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
            Calculations.Clear();
            int i = 1;//StepS1;
            //List<double> bets = new List<double>();
            //bets.Add(BetS1);
            //List<double> sums = new List<double>();
            //sums.Add(BetS1);
            //List<int> steps = new List<int>();
            //steps.Add(StepS1);
            //рассчитать на 25 шагов и не париться? будет выдавать 100шагов от указанного шага (по ставке)
            var c = new CalcIteration();
            c.Step = StepS1;
            c.Bet = BetS1;
            c.Sum = BetS1;
            int red_cnt = 0;
            while (true)
            {
                if (i >= MaxStep || red_cnt > 1)
                    break;
                var next_c = c.CalcNext(Multiply);
                if (DataBalance != null)
                {
                    if (next_c.Sum > DataBalance)
                    {
                        red_cnt++;
                        next_c.IsRed = true;
                    }
                }
                Calculations.Add(next_c);
                c = next_c;
                i++;

            }
            //double over = 0.0d;
            //int indx = 0;
            //if (DataBalance != null)
            //{
            //    over = sums.Where(t => t > DataBalance).First();
            //}
            //else
            //{
            //    over = sums.Last();
            //}
            //indx = sums.IndexOf(over);
            //StepS3 = steps[indx];
            //StepS2 = indx > 0 ? steps[indx - 1] : steps[0];
            //SummS3 = sums[indx];
            //SummS2 = indx > 0 ? sums[indx - 1] : sums[0];
            //BetS3 = bets[indx];
            //BetS2 = indx > 0 ? bets[indx - 1] : bets[0];
        }
        public void SaveSettings()
        {
            Settings.Default.CalcBet = BetS1;
            Settings.Default.CalcStep = StepS1;
            Settings.Default.CalcMulty = Multiply;
            Settings.Default.CalcMaxStep = MaxStep;
            Settings.Default.Save();
        }

    }
}
