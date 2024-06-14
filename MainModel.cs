using CsvHelper;
using CsvHelper.Configuration;
using Har_reader.Properties;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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

        public OdysseyClient Client { get => client; set => SetProperty(ref client, value); }
        public Profile Profile { get => profile; set => SetProperty(ref profile, value); }
        public string Token { get => token; set => SetProperty(ref token, value); }
        public ObservableCollection<_webSocketMessages> Answer { get => answer; set => SetProperty(ref answer, value); }
        public int Counter_lowers { get => counter_lowers; set => SetProperty(ref counter_lowers, value); }
        public bool AlertSignalOn { get => alertsignalon; set => SetProperty(ref alertsignalon, value); }
        public string Status { get => status; set => SetProperty(ref status, value); }
        public int LowerCheckValue { get => lowerCheckValue; set => SetProperty(ref lowerCheckValue, value); }
        public bool Is_connected { get => is_connected; set => SetProperty(ref is_connected, value); }
        public delegate void AlertBlink(bool blink);
        public event AlertBlink DoAlertBlink;
        public bool ExpEnabled { get => expEnabled; set => SetProperty(ref expEnabled, value); }
        public BetsModel BM { get => bM; set => SetProperty(ref bM, value); }

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
            Profile = new Profile();
            Profile.Username = "NotConn";
            Profile.PunkBalance.Punk = 0;
            Client.StatusChanged += Client_StatusChanged;
            Client.MessageGeted += Client_MessageGeted;
        }

        private void BM_OnReqBet(Bet tobet)
        {
            Client.PlaceBet(tobet);
        }

        private void Client_MessageGeted(IncomeMessageType type, _webSocketMessages mess)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                if (mess != null)
                    Debug.WriteLine(mess.Type + "\t" + mess.Data);
            }
            switch (type)
            {
                case IncomeMessageType.initial_data:
                    Profile = mess.GetProfileData;
                    break;
                case IncomeMessageType.game_crash:
                    Answer.Insert(0, mess);
                    break;
                case IncomeMessageType.tick:
                    break;
                case IncomeMessageType.none:
                    break;
                case IncomeMessageType.connected:
                    Is_connected = true;
                    BM.BetsEnabled = true;
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
            if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Remove)
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    var crsh_data = (e.NewItems[0] as _webSocketMessages).GetCrashData;
                    crsh_data.Lower = crsh_data.Game_crash_normal <= BM.LowerCheckVal;
                    Counter_lowers = crsh_data.Lower ? Counter_lowers + 1 : Counter_lowers;
                    if (Counter_lowers >= BM.AlertValCounter)
                        AlertSignalOn = true;
                    if (!crsh_data.Lower)
                    {
                        Counter_lowers = 0;
                        AlertSignalOn = false;
                    }
                }
            }
            ExpEnabled = Answer.Count > 0 ? true : false;
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
                            csv.WriteRecords(Answer);
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
                    Client.StopClient();
                },
                (obj) => true
                );
            }
        }

        public void connect()
        {
            //https://odyssey.pfplabs.xyz/?token=8a308ac635aa0c626366a3d4b8855f0da128fbe36df18253ac058e6a39cd77ecea99751418853aa9ba4b91675ac81bfc945b429a76ef1a54fc3c25da37b03738&locale=en
            //https://github.com/Marfusios/websocket-client
            //0e0f2b0948f426d8ae1242e8cb803615eeae45ed27ac057b8bdaa08350264c81b4a89968ae5a9809d46d9f2984cdd92a97fd6ec1e0e0e9d2b338a2ea2940c6f2
            //Token = token;
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
