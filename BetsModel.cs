using System;

namespace Har_reader
{
    public class BetsModel : Proper
    {
        private CommandHandler _cahngeautocashCommand;
        private CommandHandler _cahngebetvalCommand;
        private CommandHandler _cahngealertvalCommand;
        private CommandHandler _forcebetCommand;
        private bool autoBetOn;
        private Bet bet;
        private int alertVal;
        private double lowerCheckVal;
        private bool betsEnabled;

        public delegate void ReqBet(Bet tobet);
        public event ReqBet OnReqBet;
        public bool AutoBetOn { get => autoBetOn; set => SetProperty(ref autoBetOn, value); }
        public Bet Bet { get => bet; set => SetProperty(ref bet, value); }
        public int AlertValCounter { get => alertVal; set => SetProperty(ref alertVal, value); }
        public double LowerCheckVal { get => lowerCheckVal; set => SetProperty(ref lowerCheckVal, Math.Round(value, 2)); }
        public bool BetsEnabled { get => betsEnabled; set => SetProperty(ref betsEnabled, value); }
        public BetsModel()
        {
            Bet = new Bet();
            BetsEnabled = false;
            Bet.BetVal = 0.1;
            Bet.CashOut = 1;
        }
        public void TimeToBet()
        {
            OnReqBet?.Invoke(Bet);
        }
        public CommandHandler ForceBetCommand
        {
            get
            {
                return _forcebetCommand ??= new CommandHandler(obj =>
                {
                    OnReqBet?.Invoke(Bet);
                },
                obj => true
                );
            }
        }
        public CommandHandler ChangeAutoCasheVal
        {
            get
            {
                return _cahngeautocashCommand ??= new CommandHandler(obj =>
                {
                    switch (obj as string)//parametr;
                    {
                        case "+":
                            Bet.CashOut += 0.1;
                            break;
                        case "-":
                            if (Bet.CashOut <= 1)
                                Bet.CashOut = 1;
                            else
                                Bet.CashOut -= 0.1;
                            break;
                    }
                },
                (obj) => true
                );
            }
        }
        public CommandHandler ChangeBetVal
        {
            get
            {
                return _cahngebetvalCommand ??= new CommandHandler(obj =>
                {
                    switch (obj as string)//parametr;
                    {
                        case "+":
                            Bet.BetVal *= 2;
                            break;
                        case "-":
                            Bet.BetVal /= 2;
                            break;
                    }
                },
                (obj) => true
                );
            }
        }
        public CommandHandler ChangeAlertVal
        {
            get
            {
                return _cahngealertvalCommand ??= new CommandHandler(obj =>
                {
                    switch (obj as string)//parametr;
                    {
                        case "+":
                            AlertValCounter += 1;
                            break;
                        case "-":
                            AlertValCounter -= 1;
                            break;
                    }
                },
                (obj) => true
                );
            }
        }
    }
}
