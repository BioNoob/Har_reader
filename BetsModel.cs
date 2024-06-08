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

        public bool AutoBetOn { get => autoBetOn; set => SetProperty(ref autoBetOn, value); }
        public Bet Bet { get => bet; set => SetProperty(ref bet, value); }
        public int AlertVal { get => alertVal; set => SetProperty(ref alertVal, value); }
        public BetsModel()
        {

        }

        public CommandHandler ChangeAutoCasheVal
        {
            get
            {
                return _cahngeautocashCommand ??= new CommandHandler(obj =>
                {

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
                    var q = obj as string;//parametr;
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

                },
                (obj) => true
                );
            }
        }
    }
}
