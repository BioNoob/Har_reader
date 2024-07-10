using Har_reader.Models;
using Har_reader.Properties;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Media;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Har_reader
{

    public class MainModel : Proper
    {
        private int counter_lowers;
        private ObservableCollection<UnitedSockMess> usmes = new ObservableCollection<UnitedSockMess>();
        private string token;
        private bool alertsignalon;
        private string status;
        private bool is_connected = false;
        private OdysseyClient client;
        private CommandHandler _connCommand;
        private CommandHandler _exitcommand;
        private CommandHandler _savecsvcommand;
        private CommandHandler _disconnectCommand;
        private CommandHandler _cahngeVisCommand;
        private Profile profile;
        private BetsModel bM;
        private CalcBetsModel cBM;
        private MedianModel mM;
        private SoundControlModel sM;
        private GoogleApi gP;
        private bool expEnabled;
        private Bet CurrBet = null;
        private int crashCount;
        private string saveStatus;
        private string timerStatus;
        private bool setsIsOpen;
        private bool HandledBet { get; set; } = false;
        private OdysseyClient Client { get => client; set => SetProperty(ref client, value); }
        
        public bool SetsIsOpen { get => setsIsOpen; set => SetProperty(ref setsIsOpen, value); }
        public string TimerStatus { get => timerStatus; set => SetProperty(ref timerStatus, value); }
        public string SaveStatus { get => saveStatus; set => SetProperty(ref saveStatus, value); }
        public Profile Profile { get => profile; set => SetProperty(ref profile, value); }
        public string Token { get => token; set => SetProperty(ref token, value); }
        public ObservableCollection<UnitedSockMess> USmes { get => usmes; set => SetProperty(ref usmes, value); }
        public int Counter_lowers { get => counter_lowers; set => SetProperty(ref counter_lowers, value); }
        public bool AlertSignalOn
        {
            get => alertsignalon;
            set
            {
                SetProperty(ref alertsignalon, value);
                DoAlertBlink?.Invoke(value, SM.GetSound(SoundControlModel.SoundEnum.AlertSnd));
            }
        }
        public string Status { get => status; set => SetProperty(ref status, value); }
        public bool Is_connected { get => is_connected; set => SetProperty(ref is_connected, value); }
        public delegate void AlertBlink(bool blink, SoundPlayer sound);
        public event AlertBlink DoAlertBlink;
        public bool ExpEnabled { get => expEnabled; set => SetProperty(ref expEnabled, value); }
        public BetsModel BM { get => bM; set => SetProperty(ref bM, value); }
        public SoundControlModel SM { get => sM; set => SetProperty(ref sM, value); }
        public CalcBetsModel CBM { get => cBM; set => SetProperty(ref cBM, value); }
        public MedianModel MM { get => mM; set => SetProperty(ref mM, value); }
        public GoogleApi GP { get => gP; set => SetProperty(ref gP, value); }
        public int CrashCount { get => crashCount; set { SetProperty(ref crashCount, value); ExpEnabled = value > 0 ? true : false; } }

        public MainModel()
        {
            USmes.CollectionChanged += USmes_CollectionChanged;
            BM = new BetsModel();
            SM = new SoundControlModel();
            CBM = new CalcBetsModel();
            MM = new MedianModel();
            BM.OnReqBet += BM_OnReqBet;
            BM.AlertValCounter = Settings.Default.AlertVal;
            BM.LowerCheckVal = Settings.Default.LowerVal;
            BM.Bet.BetVal = Settings.Default.BetVal;
            BM.Bet.CashOut = Settings.Default.CashOutVal;
            Token = Settings.Default.Token;
            object lockObj = new object();
            BindingOperations.EnableCollectionSynchronization(USmes, lockObj);
            Client = new OdysseyClient();
            Profile = new Profile();
            GP = new GoogleApi();
            GP.AutoSaveCounter = Settings.Default.AutoSaveTime;
            SaveStatus = "Hello, Im'a save status";
            Status = "Hello, Im'a status";

            TimerStatus = "Wait new round";
            Profile.Username = "Hello, Im'a your name";
            Profile.Balance.Punk = 0;
            Client.StatusChanged += Client_StatusChanged;
            Client.MessageGeted += Client_MessageGeted;
            Client.TimerUpdated += Client_TimerUpdated;
            GP.StatusChanged += Gp_StatusChanged;
            BM.PropertyChanged += BM_PropertyChanged;
            Profile.PropertyChanged += PunkValueChanged;
            MM.RequestMedianEvent += MM_RequestMedianEvent;
        }

        private void MM_RequestMedianEvent()
        {
            MM.CalcMedian = GP.GetMedian(MM.Counter);
        }

        private void BM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (!CBM.UseData)
                if (e.PropertyName == "BetVal" || e.PropertyName == "AlertValCounter")
                    CBM.SetData(BM.AlertValCounter, BM.Bet.BetVal);
            //throw new NotImplementedException();
        }

        private void PunkValueChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Punk")
            {
                CBM.DataBalance = (sender as Profile).Balance.NormalPunk;
            }
        }
        private void USmes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //if (e.Action == NotifyCollectionChangedAction.Add)
            //{
            //    int last = CrashCount;
            //    CrashCount = USmes.Where(t => t.IsCrashed).Count();
            //    CurrSaveCounter += CrashCount - last;
            //    if (AutoSaveCounter != 0 && permition_to_save)
            //    {
            //        if (CurrSaveCounter >= AutoSaveCounter)
            //        {
            //            gp.DoSheetSave();
            //            CurrSaveCounter = 0;
            //        }
            //        else
            //        {
            //            if (!gp.SaveInProgress)
            //                SaveStatus = $"Waiting new data more { AutoSaveCounter - CurrSaveCounter} times";
            //        }
            //    }
            //    ExpEnabled = CrashCount > 0 ? true : false;
            //}
        }
        private void Client_TimerUpdated(TimerMess type, double val)
        {
            switch (type)
            {
                case TimerMess.fly:
                    TimerStatus = val.ToString("0.00x", CultureInfo.InvariantCulture);
                    break;
                case TimerMess.start:
                    TimerStatus = val.ToString("0.00s", CultureInfo.InvariantCulture);
                    break;
            }
        }
        private void Gp_StatusChanged(string txt)
        {
            SaveStatus = txt;
        }
        private void BM_OnReqBet(Bet tobet)
        {
            HandledBet = true;
            Client.PlaceBet(tobet);
        }
        private void Client_MessageGeted(IncomeMessageType type, _webSocketMessages mess)
        {
            switch (type)
            {
                case IncomeMessageType.initial_data:
                    Profile.SetByAnotherProfile(mess.GetProfileData);
                    GP.SetProfile(Profile);
                    //CBM.SetData(Profile.Balance.NormalPunk, null, null);
                    if (!GP.IsConnected)
                        GP.SetConnAndRead();
                    break;
                case IncomeMessageType.game_crash:
                    if (!BM.BetsEnabled)
                        BM.BetsEnabled = true;
                    //Узнали меньше ли оно требования
                    mess.GetCrashData.Lower = mess.GetCrashData.Game_crash_normal <= BM.LowerCheckVal;
                    //счетчик, если меньше увеличили на 1, по идее если нет, то скинули в 0
                    Counter_lowers = mess.GetCrashData.Lower ? Counter_lowers + 1 : 0;
                    //если досчитали до требования то бьем тревогу, и там еще и ставку по требованию делаем
                    if (Counter_lowers >= BM.AlertValCounter)
                    {
                        if (!AlertSignalOn) AlertSignalOn = true;
                        if (BM.AutoBetOn)
                        {
                            BM_OnReqBet(BM.Bet);
                            BM.AutoBetOn = false;
                        }
                    }
                    else
                    {
                        if (AlertSignalOn) AlertSignalOn = false;
                    }
                    CBM.SetData(Counter_lowers, BM.Bet.BetVal);
                    SM.GetSound(SoundControlModel.SoundEnum.CrushSound)?.Play();
                    break;
                case IncomeMessageType.bets:
                    if (HandledBet)
                        return;
                    CurrBet = new Bet();
                    CurrBet.BetVal = mess.GetBetAccData.BetVal;
                    CurrBet.CashOut = mess.GetBetAccData.CashOut;
                    Profile.Balance.SetPunk(Profile.Balance.NormalPunk - CurrBet.BetVal);
                    CBM.SetData(Counter_lowers, BM.Bet.BetVal);
                    break;
                case IncomeMessageType.bet_accepted:
                    if (!HandledBet)
                        return;
                    CurrBet = new Bet();
                    CurrBet.BetVal = mess.GetBetAccData.BetVal;
                    CurrBet.CashOut = mess.GetBetAccData.CashOut;
                    //Debug.WriteLine($"BALANCE FROM {Profile.Balance.NormalPunk} DOWN TO {Profile.Balance.NormalPunk - CurrBet.BetVal}");
                    Profile.Balance.SetPunk(Profile.Balance.NormalPunk - CurrBet.BetVal);
                    CBM.SetData(Counter_lowers, BM.Bet.BetVal);
                    break;
                case IncomeMessageType.win:
                    //WIN by AutoCashOut
                    //Debug.WriteLine($"BALANCE FROM {Profile.Balance.NormalPunk} UP TO {Profile.Balance.NormalPunk + CurrBet.Profit}");
                    SM.GetSound(SoundControlModel.SoundEnum.WinSnd)?.Play();
                    HandledBet = false;
                    mess.ProfitData = mess.GetTickdData.MyCashOut.NValue * CurrBet.BetVal - CurrBet.BetVal;
                    Profile.Balance.SetPunk(Profile.Balance.NormalPunk + mess.ProfitData + CurrBet.BetVal);
                    mess.ReviewData = $"Win {mess.ProfitData.ToString("0.##", CultureInfo.InvariantCulture)}";
                    CBM.SetData(Counter_lowers, BM.Bet.BetVal);
                    break;
                case IncomeMessageType.lose:
                    SM.GetSound(SoundControlModel.SoundEnum.LoseSnd)?.Play();
                    HandledBet = false;
                    mess.ProfitData = -1 * CurrBet.BetVal;
                    mess.ReviewData = $"Lose {CurrBet.BetVal.ToString("0.##", CultureInfo.InvariantCulture)}";
                    CBM.SetData(Counter_lowers, BM.Bet.BetVal);
                    break;
                case IncomeMessageType.connected:
                    Is_connected = true;
                    //BM.BetsEnabled = true;
                    break;
                case IncomeMessageType.disconnected:
                    Is_connected = false;
                    BM.BetsEnabled = false;
                    CBM.UseData = false;
                    //Profile.SetByAnotherProfile(new Profile() {Username = "" });
                    TimerStatus = "Disconnected";
                    break;
                default:
                    break;
            }
            if (mess is null)
                return;

            if (USmes.Any(w => w.GameId == mess.GameId))
            {
                USmes.Single(w => w.GameId == mess.GameId).SetDataByMess(type, mess);
            }
            else
            {
                var x = new UnitedSockMess(mess.GameId);
                x.SetDataByMess(type, mess);
                USmes.Insert(0, x);
            }
            var q = USmes.Single(w => w.GameId == mess.GameId);
            if (q.IsCrashed)
            {
                CrashCount++;
                GP.ToSave.Add(new SavedData() { Id = q.GameId, Crash = q.GameCrash.Value, Profit = q.Profit, Dt = q.DateOfGame });
                MM.CalcMedian = GP.GetMedian(MM.Counter);
            }
                
        }
        private void Client_StatusChanged(string txt)
        {
            Status = txt;
        }
        public CommandHandler ConnectCommand
        {
            get
            {
                return _connCommand ??= new CommandHandler(async obj =>
                {
                    if (!string.IsNullOrWhiteSpace(Token))
                    {
                        USmes.Clear();
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
        public CommandHandler ExitCommand
        {
            get
            {
                return _exitcommand ??= new CommandHandler(obj =>
                {
                    Client.StopClient();
                    GP.DoSheetSave();
                    SM.SaveSettings();
                    CBM.SaveSettings();
                    MM.SaveSettings();
                    Settings.Default.AlertVal = BM.AlertValCounter;
                    Settings.Default.LowerVal = BM.LowerCheckVal;
                    Settings.Default.BetVal = BM.Bet.BetVal;
                    Settings.Default.CashOutVal = BM.Bet.CashOut;
                    Settings.Default.Token = Token;
                    Settings.Default.AutoSaveTime = GP.AutoSaveCounter;
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
                    GP.DoSheetSave();
                },
                (obj) => true
                );
            }
        }
        public CommandHandler ChangeVisSettings
        {
            get
            {
                return _cahngeVisCommand ??= new CommandHandler(obj =>
                {
                    SetsIsOpen = !SetsIsOpen;
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
