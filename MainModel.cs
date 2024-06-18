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
using System.Media;
using System.Threading.Tasks;
using System.Timers;
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
        private int autoSaveTime;
        SoundPlayer p_win = new SoundPlayer(Resources.win);
        SoundPlayer p_lose = new SoundPlayer(Resources.lose);
        private string saveStatus;

        public string SaveStatus { get => saveStatus; set => SetProperty(ref saveStatus, value); }
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
        public int AutoSaveTime
        {
            get => autoSaveTime; set
            {
                SaveTimer.Stop();
                SaveTimer.Interval = value * 1000 * 60;
                SetProperty(ref autoSaveTime, value);
                if (value != 0)
                    SaveTimer.Start();
            }
        }
        private Timer SaveTimer;
        public MainModel()
        {
            Answer.CollectionChanged += Answer_CollectionChanged;
            BM = new BetsModel();
            BM.OnReqBet += BM_OnReqBet;
            SaveTimer = new Timer();
            SaveTimer.AutoReset = true;
            SaveTimer.Elapsed += SaveTimer_Elapsed;
            BM.AlertValCounter = Settings.Default.AlertVal;
            BM.LowerCheckVal = Settings.Default.LowerVal;
            BM.Bet.BetVal = Settings.Default.BetVal;
            BM.Bet.CashOut = Settings.Default.CashOutVal;
            AutoSaveTime = Settings.Default.AutoSaveTime;
            Token = Settings.Default.Token;
            SaveStatus = "Hello, I'm save status";
            Status = "Hello, Im'a status";
            object lockObj = new object();
            BindingOperations.EnableCollectionSynchronization(Answer, lockObj);
            Client = new OdysseyClient();
            Profile = new Profile();
            Profile.Username = "NULL";
            Profile.Balance.Punk = 0;
            Client.StatusChanged += Client_StatusChanged;
            Client.MessageGeted += Client_MessageGeted;
            gp = new GoogleApi();
            gp.StatusChanged += Gp_StatusChanged;
        }


        private void SaveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            gp.DoSheetSave(Answer);
        }

        private void Gp_StatusChanged(string txt)
        {
            SaveStatus = txt;
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
                    gp.SetProfile(Profile);
                    SaveTimer.Start();
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
                case IncomeMessageType.bets:
                    CurrBet = new Bet();
                    CurrBet.BetVal = mess.GetBetAccData.BetVal;
                    CurrBet.CashOut = mess.GetBetAccData.CashOut;
                    Profile.Balance.SetPunk(Profile.Balance.NormalPunk - CurrBet.BetVal);
                    Answer.Insert(0, mess);
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
                    p_win.Play();
                    mess.ProfitData = mess.GetTickdData.MyCashOut.NValue * CurrBet.BetVal;
                    Profile.Balance.SetPunk(Profile.Balance.NormalPunk + mess.ProfitData);
                    mess.ReviewData = $"Win {mess.ProfitData.ToString(CultureInfo.InvariantCulture)}";
                    Answer.Insert(0, mess);
                    break;
                case IncomeMessageType.lose:
                    BM.AutoBetOn = false;
                    p_lose.Play();
                    mess.ProfitData = -1 * CurrBet.BetVal;
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
                    SaveTimer.Stop();
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
                    Settings.Default.AutoSaveTime = AutoSaveTime;
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
                    gp.DoSheetSave(Answer);
                    //Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
                    //sfd.RestoreDirectory = true;
                    //sfd.Filter = "Csv files (*.csv)|*.csv|All files (*.*)|*.*";
                    //sfd.DefaultExt = "csv";
                    //sfd.InitialDirectory = path;
                    //sfd.Title = "Save export file";
                    //sfd.FileName = "EXPORT.csv";
                    //if (sfd.ShowDialog() == true)
                    //{
                    //    using (var writer = new StreamWriter(sfd.FileName))
                    //    using (var csv = new CsvWriter(writer,
                    //        new CsvConfiguration(CultureInfo.CurrentCulture) { Delimiter = ";" }))
                    //    {
                    //        csv.Context.RegisterClassMap<FooMap>();
                    //        csv.WriteRecords(Answer.Where(t => t.MsgType == IncomeMessageType.game_crash));
                    //    }
                    //    ProcessStartInfo psi = new ProcessStartInfo();
                    //    psi.FileName = sfd.FileName;
                    //    psi.UseShellExecute = true;
                    //    Process.Start(psi);
                    //    Status = "Saved";
                    //}
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
                    SaveTimer.Stop();
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
