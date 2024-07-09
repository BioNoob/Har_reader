namespace Har_reader.Models
{
    public class MedianModel : Proper
    {
        private int counter;
        private double calcMedian;
        private CommandHandler _cahngeCountvalCommand;
        public delegate void RequestNewMedian();
        public event RequestNewMedian RequestMedianEvent;
        public MedianModel()
        {
            CalcMedian = 0.0;
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

        public CommandHandler ChangeCountVal
        {
            get
            {
                return _cahngeCountvalCommand ??= new CommandHandler(obj =>
                {
                    switch (obj as string)//parametr;
                    {
                        case "1000":
                            Counter = 1000;
                            break;
                        case "500":
                            Counter = 500;
                            break;
                        case "100":
                            Counter = 100;
                            break;
                    }
                },
                (obj) => true
                );
            }
        }
        public void SaveSettings()
        {
            Properties.Settings.Default.MedianCount = Counter;
            Properties.Settings.Default.Save();
        }
    }
}
