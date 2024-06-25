using Har_reader.Properties;
using System.Collections.ObjectModel;
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
            if (c.Bet > 80)
                c.Bet = 80;
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
        private ObservableCollection<CalcIteration> calculations;
        public ObservableCollection<CalcIteration> Calculations { get => calculations; set => SetProperty(ref calculations, value); }


        public double Multiply { get => multiply; set { SetProperty(ref multiply, value); CalcPrediction(); } }
        public bool UseData { get => useData; set { SetProperty(ref useData, value); CalcPrediction(); } }

        public double BetS1 { get => betS1; set { SetProperty(ref betS1, value); CalcPrediction(); } }
        public int StepS1 { get => stepS1; set { SetProperty(ref stepS1, value); CalcPrediction(); } }
        private int backupStep { get; set; }
        private double backupBet { get; set; }
        private double? DataBalance { get; set; }
        public CalcBetsModel()
        {
            Calculations = new ObservableCollection<CalcIteration>();
            object lockobj = new object();
            BindingOperations.EnableCollectionSynchronization(Calculations, lockobj);
            BetS1 = Settings.Default.CalcBet;
            StepS1 = Settings.Default.CalcStep;
            Multiply = Settings.Default.CalcMulty;

            CalcPrediction();
        }

        public void SetData(double balance, int? currLoseStrike, double? currBetAcc)
        {
            DataBalance = balance;
            if (UseData)
            {
                if (currLoseStrike.HasValue)
                {
                    backupStep = StepS1;
                    StepS1 = currLoseStrike.Value;
                }
                if (currBetAcc.HasValue)
                {
                    backupBet = BetS1;
                    BetS1 = currBetAcc.Value;
                }
            }
            else
            {
                StepS1 = backupStep;
                BetS1 = backupBet;
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
                if (red_cnt > 1)
                    break;
                var next_c = c.CalcNext(Multiply);
                if (DataBalance != null)
                {
                    if (next_c.Sum > DataBalance || next_c.Bet == 80)
                    {
                        red_cnt++;
                        next_c.IsRed = true;
                    }
                }
                else
                {
                    if (next_c.Bet == 80)
                    {
                        red_cnt++;
                        next_c.IsRed = true;
                    }

                }
                Calculations.Add(next_c);
                c = next_c;
                i++;
            }
        }
        public void SaveSettings()
        {
            Settings.Default.CalcBet = BetS1;
            Settings.Default.CalcStep = StepS1;
            Settings.Default.CalcMulty = Multiply;
            Settings.Default.Save();
        }

    }
}
