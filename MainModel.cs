using CsvHelper;
using CsvHelper.Configuration;
using Har_reader.Properties;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Har_reader
{

    public class MainModel : Proper
    {
        private int counter_lowers;
        private ObservableCollection<_webSocketMessages> answer = new ObservableCollection<_webSocketMessages>();
        private string token;
        private bool alertsignalon;
        private string status;
        private string path;
        private int lowerCheckValue;
        private bool is_connected = false;
        private OdysseyClient client;
        private CommandHandler _connCommand;
        private CommandHandler _exitcommand;
        private CommandHandler _savecsvcommand;
        private CommandHandler _disconnectCommand;
        private Profile profile;
        private BetsModel bM;
        private bool expEnabled;
        private Bet CurrBet = null;
        private int crashCount;

        private OdysseyClient Client { get => client; set => SetProperty(ref client, value); }
        private GoogleApi gp { get; set; }
        public Profile Profile { get => profile; set => SetProperty(ref profile, value); }
        public string Token { get => token; set => SetProperty(ref token, value); }
        public ObservableCollection<_webSocketMessages> Answer { get => answer; set => SetProperty(ref answer, value); }
        public int Counter_lowers { get => counter_lowers; set => SetProperty(ref counter_lowers, value); }
        public bool AlertSignalOn
        {
            get => alertsignalon;
            set
            {
                SetProperty(ref alertsignalon, value);
                DoAlertBlink?.Invoke(value);
            }
        }
        public string Status { get => status; set => SetProperty(ref status, value); }
        public int LowerCheckValue { get => lowerCheckValue; set => SetProperty(ref lowerCheckValue, value); }
        public bool Is_connected { get => is_connected; set => SetProperty(ref is_connected, value); }
        public delegate void AlertBlink(bool blink);
        public event AlertBlink DoAlertBlink;
        public bool ExpEnabled { get => expEnabled; set => SetProperty(ref expEnabled, value); }
        public BetsModel BM { get => bM; set => SetProperty(ref bM, value); }
        public int CrashCount { get => crashCount; set => SetProperty(ref crashCount, value); }

        public MainModel()
        {
            Answer.CollectionChanged += Answer_CollectionChanged;
            BM = new BetsModel();
            BM.OnReqBet += BM_OnReqBet;
            BM.AlertValCounter = Settings.Default.AlertVal;
            BM.LowerCheckVal = Settings.Default.LowerVal;
            BM.Bet.BetVal = Settings.Default.BetVal;
            BM.Bet.CashOut = Settings.Default.CashOutVal;
            Token = Settings.Default.Token;
            object lockObj = new object();
            BindingOperations.EnableCollectionSynchronization(Answer, lockObj);
            Client = new OdysseyClient();
            gp = new GoogleApi();
            Profile = new Profile();
            Profile.Username = "NotConn";
            Profile.Balance.Punk = 0;
            Client.StatusChanged += Client_StatusChanged;
            Client.MessageGeted += Client_MessageGeted;
            gp.StatusChanged += Gp_StatusChanged;
        }

        private void Gp_StatusChanged(string txt)
        {
            Status = txt;
        }

        private void BM_OnReqBet(Bet tobet)
        {
            Client.PlaceBet(tobet);
        }



        private void Client_MessageGeted(IncomeMessageType type, _webSocketMessages mess)
        {
            switch (type)
            {
                case IncomeMessageType.initial_data:
                    Profile = mess.GetProfileData;
                    Answer.Insert(0, mess);
                    break;
                case IncomeMessageType.game_crash:
                    if (!BM.BetsEnabled)
                        BM.BetsEnabled = true;
                    //Узнали меньше ли оно требования
                    mess.GetCrashData.Lower = mess.GetCrashData.Game_crash_normal <= BM.LowerCheckVal;
                    Answer.Insert(0, mess);
                    //счетчик, если меньше увеличили на 1, по идее если нет, то скинули в 0
                    Counter_lowers = mess.GetCrashData.Lower ? Counter_lowers + 1 : 0;
                    //если досчитали до требования то бьем тревогу, и там еще и ставку по требованию делаем
                    if (Counter_lowers >= BM.AlertValCounter)
                    {
                        if (!AlertSignalOn) AlertSignalOn = true;
                        if (BM.AutoBetOn)
                        {
                            //таким макаром будем плейсить каждый раз пока условие работает
                            //System.Windows.MessageBox.Show("BET MAY BE PLACED AT REQUEST\n" +
                            //    $"{BM.Bet.GetRequest}");
                            Client.PlaceBet(BM.Bet);
                        }
                    }
                    else
                    {
                        if (AlertSignalOn) AlertSignalOn = false;
                    }
                    break;
                case IncomeMessageType.bet_accepted:
                    CurrBet = new Bet();
                    CurrBet.BetVal = mess.GetBetAccData.BetVal;
                    CurrBet.CashOut = mess.GetBetAccData.CashOut;
                    //Debug.WriteLine($"BALANCE FROM {Profile.Balance.NormalPunk} DOWN TO {Profile.Balance.NormalPunk - CurrBet.BetVal}");
                    Profile.Balance.SetPunk(Profile.Balance.NormalPunk - CurrBet.BetVal);
                    Answer.Insert(0, mess);
                    break;
                case IncomeMessageType.tick:
                    //WIN by AutoCashOut
                    //Debug.WriteLine($"BALANCE FROM {Profile.Balance.NormalPunk} UP TO {Profile.Balance.NormalPunk + CurrBet.Profit}");
                    Profile.Balance.SetPunk(Profile.Balance.NormalPunk + CurrBet.Profit);
                    mess.ReviewData = $"Win {CurrBet.Profit.ToString(CultureInfo.InvariantCulture)}";
                    Answer.Insert(0, mess);
                    break;
                case IncomeMessageType.lose:
                    BM.AutoBetOn = false;
                    mess.ReviewData = $"Lose {CurrBet.BetVal.ToString(CultureInfo.InvariantCulture)}";
                    Answer.Insert(0, mess);
                    break;
                case IncomeMessageType.connected:
                    Is_connected = true;
                    //BM.BetsEnabled = true;
                    break;
                case IncomeMessageType.disconnected:
                    Is_connected = false;
                    BM.BetsEnabled = false;
                    break;
                default:
                    break;
            }
        }

        private void Client_StatusChanged(string txt)
        {
            Status = txt;
        }

        private void Answer_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Remove)
            //{
            //    if (e.Action == NotifyCollectionChangedAction.Add)
            //    {
            //        var crsh_data = (e.NewItems[0] as _webSocketMessages).GetCrashData;
            //        crsh_data.Lower = crsh_data.Game_crash_normal <= BM.LowerCheckVal;
            //        Counter_lowers = crsh_data.Lower ? Counter_lowers + 1 : Counter_lowers;
            //        if (Counter_lowers >= BM.AlertValCounter)
            //            AlertSignalOn = true;
            //        if (!crsh_data.Lower)
            //        {
            //            Counter_lowers = 0;
            //            AlertSignalOn = false;
            //        }
            //    }
            //}
            CrashCount = Answer.Where(t => t.MsgType == IncomeMessageType.game_crash).Count();
            ExpEnabled = CrashCount > 0 ? true : false;
        }
        public CommandHandler ConnectCommand
        {
            get
            {
                return _connCommand ??= new CommandHandler(async obj =>
                {
                    if (!string.IsNullOrWhiteSpace(Token))
                    {
                        Answer.Clear();
                        //Status = "Connecting..";
                        await Task.Run(() => connect());
                    }
                    else
                        Status = "Empty Token!";
                },
                (obj) => true
                );
            }
        }
        //private CommandHandler _selectfilecomand;
        //public CommandHandler SelectFileCommand
        //{
        //    get
        //    {
        //        return _selectfilecomand ??= new CommandHandler(obj =>
        //        {
        //            Microsoft.Win32.OpenFileDialog t = new Microsoft.Win32.OpenFileDialog();
        //            t.RestoreDirectory = true;
        //            t.Filter = "Har files (*.har)|*.har|All files (*.*)|*.*";
        //            t.DefaultExt = "har";
        //            t.Multiselect = true;
        //            if (t.ShowDialog() == true)
        //            {
        //                Answer.Clear();
        //                path = Path.GetDirectoryName(t.FileName);
        //                Status = path;
        //                foreach (var file in t.FileNames)
        //                {
        //                    string file_cont = File.ReadAllText(file);
        //                    JObject o = JObject.Parse(file_cont);
        //                    var messages = o.SelectTokens("log.entries").Select(t => t.Children()["_webSocketMessages"]).ToList();
        //                    foreach (var item in messages)
        //                    {
        //                        foreach (var token in item)
        //                        {
        //                            List<_webSocketMessages> q = JsonConvert.DeserializeObject<List<_webSocketMessages>>(token.ToString());
        //                            q.Where(t => t.Data.Contains("\"type\":\"game_crash\"")).Where(t => t.GetData.Game_crash != 0).ToList().ForEach(p => Answer.Add(p));
        //                        }
        //                    }
        //                }
        //            }
        //            //if(a)
        //            //{
        //            //    DoAlertBlink?.Invoke(false);
        //            //    a = false;
        //            //    return;
        //            //}
        //            //DoAlertBlink?.Invoke(true);
        //            //a = true;
        //        },
        //        (obj) => true
        //        );
        //    }
        //}

        public CommandHandler ExitCommand
        {
            get
            {
                return _exitcommand ??= new CommandHandler(obj =>
                {
                    Client.StopClient();
                    Settings.Default.AlertVal = BM.AlertValCounter;
                    Settings.Default.LowerVal = BM.LowerCheckVal;
                    Settings.Default.BetVal = BM.Bet.BetVal;
                    Settings.Default.CashOutVal = BM.Bet.CashOut;
                    Settings.Default.Token = Token;
                    Settings.Default.Save();
                    Environment.Exit(1);
                },
                (obj) => true
                );
            }
        }

        public CommandHandler SaveCsvCommand
        {
            get
            {
                return _savecsvcommand ??= new CommandHandler(obj =>
                {
                    gp.DoSheetSave(Profile.Username, Answer.Where(t => t.MsgType == IncomeMessageType.game_crash));
                    return;

                    Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
                    sfd.RestoreDirectory = true;
                    sfd.Filter = "Csv files (*.csv)|*.csv|All files (*.*)|*.*";
                    sfd.DefaultExt = "csv";
                    sfd.InitialDirectory = path;
                    sfd.Title = "Save export file";
                    sfd.FileName = "EXPORT.csv";
                    if (sfd.ShowDialog() == true)
                    {
                        using (var writer = new StreamWriter(sfd.FileName))
                        using (var csv = new CsvWriter(writer,
                            new CsvConfiguration(CultureInfo.CurrentCulture) { Delimiter = ";" }))
                        {
                            csv.Context.RegisterClassMap<FooMap>();
                            csv.WriteRecords(Answer.Where(t=>t.MsgType == IncomeMessageType.game_crash));
                        }
                        ProcessStartInfo psi = new ProcessStartInfo();
                        psi.FileName = sfd.FileName;
                        psi.UseShellExecute = true;
                        Process.Start(psi);
                        Status = "Saved";
                    }
                },
                (obj) => true
                );
            }
        }

        public CommandHandler DisconnectCommand
        {
            get
            {
                return _disconnectCommand ??= new CommandHandler(obj =>
                {
                    AlertSignalOn = false;
                    Counter_lowers = 0;
                    Client.StopClient();
                },
                (obj) => true
                );
            }
        }

        public void connect()
        {
            Is_connected = true;
            try
            {
                Client.Connect(Token);
            }
            catch (Exception)
            {
                return;
            }
        }
    }
}
