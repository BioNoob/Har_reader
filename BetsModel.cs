using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Har_reader
{
    public class BetsModel : Proper
    {
        private CommandHandler _cahngeautocashCommand;
        private CommandHandler _cahngebetvalCommand;
        private CommandHandler _cahngealertvalCommand;
        private bool autoBetOn;
        private Bet bet;
        private int alertVal;
        private double lowerCheckVal;

        public bool AutoBetOn { get => autoBetOn; set => SetProperty(ref autoBetOn, value); }
        public Bet Bet { get => bet; set => SetProperty(ref bet, value); }
        public int AlertVal { get => alertVal; set => SetProperty(ref alertVal, value); }
        public double LowerCheckVal { get => lowerCheckVal; set => SetProperty(ref lowerCheckVal, value); }
        public BetsModel()
        {
            Bet = new Bet(); 
            Bet.BetVal = 0.1;
            Bet.CashOut = 0.1;
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
                            AlertVal += 1;
                            break;
                        case "-":
                            AlertVal -= 1;
                            break;
                    }
                },
                (obj) => true
                );
            }
        }
    }
}
