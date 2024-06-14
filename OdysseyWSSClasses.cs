using CsvHelper.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.WebSockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using Websocket.Client;
using System.Timers;
using Timer = System.Timers.Timer;
using System.Threading.Tasks;
using System.Linq;

namespace Har_reader
{
    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }
    public enum IncomeMessageType
    {
        initial_data,
        game_crash,
        tick,
        bet_accepted,
        lose,
        none,
        connected,
        disconnected
    }
    public class OdysseyClient : Proper
    {
        private Timer _timer = new Timer(1000);
        private ManualResetEvent exitEvent = new ManualResetEvent(false);
        private WebsocketClient client = new WebsocketClient(new Uri("wss://ton-rocket-server.pfplabs.xyz/"));
        private const string _init_mess = "{\"type\":\"join\",\"data\":{\"initData\":\"\",\"browserInitData\":{\"token\":\"" + "ECHO" + "\",\"locale\":\"en\"}}}";
        private string init_mess = "";
        private static readonly object GATE1 = new object();
        private int counter_of_sec = 0;
        private string MyUserName;
        private bool gameInProgress;
        private List<string> PresetStatusBet = new List<string> { "Game in progress", "Game in progress.", "Game in progress..", "Game in progress...", "Game in progress...." };
        private List<string> PresetStatusConn = new List<string> { "Connecting", "Connecting.", "Connecting..", "Connecting...", "Connecting...." };
        private bool haveActiveBet;
        private bool authDone;

        public bool GameInProgress { get => gameInProgress; set => SetProperty(ref gameInProgress, value); }
        public bool HaveActiveBet { get => haveActiveBet; set => SetProperty(ref haveActiveBet, value); }
        private bool AuthDone { get => authDone; set => SetProperty(ref authDone, value); }
        public OdysseyClient()
        {
            _timer.Elapsed += _timer_Elapsed;
            _timer.AutoReset = true;
            GameInProgress = true;
            HaveActiveBet = false;
            AuthDone = false;
            client.ReconnectionHappened.Subscribe(info => client.Send(init_mess));
            client.MessageReceived
                .Where(t => !string.IsNullOrEmpty(t.Text))
                .Where(t => t.Text.Contains("\"type\":\"initial_data\""))
                .ObserveOn(TaskPoolScheduler.Default)
                .Synchronize(GATE1)
                .Subscribe(ConnMessageRecived);
            client.MessageReceived
                .Where(t => !string.IsNullOrEmpty(t.Text))
                .Where(t => t.Text.Contains("\"type\":\"game_crash\""))
                .ObserveOn(TaskPoolScheduler.Default)
                .Synchronize(GATE1)
                .Subscribe(CrashMessageRecived);
            client.MessageReceived
                .Where(t => !string.IsNullOrEmpty(t.Text))
                .Where(t => t.Text.Contains("\"type\":\"game_starting\""))
                .ObserveOn(TaskPoolScheduler.Default)
                .Synchronize(GATE1)
                .Subscribe(t => GameInProgress = false);
            client.MessageReceived
                .Where(t => !string.IsNullOrEmpty(t.Text))
                .Where(t => t.Text.Contains("\"type\":\"tick\""))
                .ObserveOn(TaskPoolScheduler.Default)
                .Synchronize(GATE1)
                .Subscribe(MyCashOutMsgRecived);
            client.MessageReceived
                .Where(t => !string.IsNullOrEmpty(t.Text))
                .Where(t => t.Text.Contains("\"type\":\"game_started\""))
                .ObserveOn(TaskPoolScheduler.Default)
                .Synchronize(GATE1)
                .Subscribe(t => GameInProgress = true);
            client.MessageReceived
                .Where(t => !string.IsNullOrEmpty(t.Text))
                .Where(t => t.Text.Contains("\"type\":\"bet_accepted\""))
                .ObserveOn(TaskPoolScheduler.Default)
                .Synchronize(GATE1)
                .Subscribe(BetAccMsgRecived);
        }

        private async void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (counter_of_sec > 5)
            {
                counter_of_sec = 0;
                _timer.Stop();
                StatusChanged?.Invoke("Conn TO, check token");
                MessageGeted?.Invoke(IncomeMessageType.disconnected, null);
                await Task.Delay(2000);
                StopClient();
            }
            else
            {
                if (counter_of_sec > PresetStatusConn.Count - 1)
                    StatusChanged?.Invoke(PresetStatusConn[0]);
                else
                    StatusChanged?.Invoke(PresetStatusConn[counter_of_sec]);
                counter_of_sec++;

            }
        }

        public delegate void SendStatus(string txt);
        public event SendStatus StatusChanged;
        public delegate void GetedMessage(IncomeMessageType type, _webSocketMessages mess);
        public event GetedMessage MessageGeted;
        private void ConnMessageRecived(ResponseMessage msg)
        {
            if (!AuthDone)
            {
                _timer.Stop();
                StatusChanged?.Invoke("Connected");
                MessageGeted?.Invoke(IncomeMessageType.connected, null);
                var xz = _webSocketMessages.FromJson(msg.Text);
                MessageGeted?.Invoke(IncomeMessageType.initial_data, xz);
                MyUserName = xz.GetProfileData.Username;
                AuthDone = true;
            }
        }
        private void MyCashOutMsgRecived(ResponseMessage msg)
        {
            if (!string.IsNullOrEmpty(MyUserName))
                if (msg.Text.Contains(MyUserName))
                {
                    HaveActiveBet = false;
                    var xz = _webSocketMessages.FromJson(msg.Text);
                    xz.GetTickdData.MyCashOut = xz.GetTickdData.CashOutsLst.Single(t => t.Id == MyUserName);
                    MessageGeted?.Invoke(IncomeMessageType.tick, xz);
                }

        }
        private void BetAccMsgRecived(ResponseMessage msg)
        {
            HaveActiveBet = true;
            StatusChanged?.Invoke("BET placed");
            MessageGeted?.Invoke(IncomeMessageType.bet_accepted, _webSocketMessages.FromJson(msg.Text));
        }
        private void CrashMessageRecived(ResponseMessage msg)
        {
            MessageGeted?.Invoke(IncomeMessageType.game_crash, _webSocketMessages.FromJson(msg.Text));
            if (HaveActiveBet)
            {
                var xz = new _webSocketMessages();
                xz.HandSetData("lose");
                MessageGeted?.Invoke(IncomeMessageType.lose, xz);
            }

        }
        public async void Connect(string token)
        {
            StatusChanged?.Invoke("Try Connect");
            init_mess = _init_mess.Replace("ECHO", token);
            _timer.Start();
            await client.Start();
            client.Send(init_mess);
            exitEvent.WaitOne();
        }
        public async void PlaceBet(Bet bet)
        {
            StatusChanged?.Invoke("Try place a bet");
            int a = 0;
            while (GameInProgress)
            {
                StatusChanged?.Invoke(PresetStatusBet[a++]);
                if (a > PresetStatusBet.Count) a = 0;
                await Task.Delay(500);
            }
            if (!client.Send(bet.GetRequest))
                StatusChanged?.Invoke("Error place bet");
        }
        public void StopClient()
        {
            if (client.IsStarted)
            {
                AuthDone = false;
                HaveActiveBet = false;
                GameInProgress = false;
                MyUserName = string.Empty;
                StatusChanged?.Invoke("Disconnected");
                MessageGeted?.Invoke(IncomeMessageType.disconnected, null);
                exitEvent.Set();
                client.Stop(WebSocketCloseStatus.Empty, string.Empty);
            }
        }
    }
    public class Message : Proper
    {
    }
    public class Bet : Proper
    {
        private double bets;
        private double cashOut;

        public Bet() { }
        public Bet(double bet, double cash_out)
        {
            BetVal = bet;
            CashOut = cash_out;
        }
        public double BetVal
        {
            get => bets; set
            {
                if (value <= 0.1)
                    value = 0.1;
                SetProperty(ref bets, Math.Round(value, 2));
            }
        }
        public double CashOut
        {
            get => cashOut;
            set
            {
                if (value <= 1)
                    value = 1;
                SetProperty(ref cashOut, Math.Round(value, 2));
            }
        }
        public double Profit => Math.Round(BetVal * CashOut, 3);
        private double _truebet => BetVal * 100000000; //0.1 = 100000000
        private double _truecashout => CashOut * 100;
        public string GetRequest => "{\"type\":\"place_bet\",\"data\":{\"amount\":" +
            $"{_truebet},\"autoCashOut\":{_truecashout}}}";
    }
    public class game_crash_mess : Message
    {
        private double elapsed;
        private double game_crash;
        private bool lower;

        public double Elapsed { get => elapsed; set => SetProperty(ref elapsed, value); }
        public double Game_crash { get => game_crash; set => SetProperty(ref game_crash, value); }
        public double Game_crash_normal => Game_crash / 100d;
        [JsonIgnore]
        public bool Lower { get => lower; set => SetProperty(ref lower, value); }
    }
    public partial class Balance : Proper
    {
        private double punk;
        private double ton;

        [JsonProperty("punk")]
        public double Punk { get => punk; set => SetProperty(ref punk, value); }

        [JsonProperty("ton")]
        public double Ton { get => ton; set => SetProperty(ref ton, value); }
        public double NormalPunk => Math.Round(Punk / 1000000000, 2,MidpointRounding.ToNegativeInfinity);
        public void SetPunk(double val)
        {
            Punk = val * 1000000000;
        }
    }
    public class Profile : Message
    {
        private Balance punkBalance;
        private int id;
        private string username;

        public Profile() { Balance = new Balance(); }
        [JsonProperty("balance")]
        public Balance Balance { get => punkBalance; set => SetProperty(ref punkBalance, value); }
        [JsonProperty("id")]
        public int Id { get => id; set => SetProperty(ref id, value); }
        [JsonProperty("username")]
        public string Username { get => username; set => SetProperty(ref username, value); }
    }
    public class StartingMess : Message
    {
        private int id;
        [JsonProperty("game_id")]
        public int Id { get => id; set => SetProperty(ref id, value); }
    }
    public class CashOuts : Proper
    {
        private double val;
        private string id;

        public string Id { get => id; set => SetProperty(ref id, value); }
        public double Value { get => val; set => SetProperty(ref val, value); }
    }
    public class TickMess : Message
    {
        private List<CashOuts> cashOutsLst;
        private CashOuts myCashOut;

        [JsonProperty("cashouts")]
        public List<CashOuts> CashOutsLst { get => cashOutsLst; set => SetProperty(ref cashOutsLst, value); }
        [JsonIgnore]
        public CashOuts MyCashOut { get => myCashOut; set => SetProperty(ref myCashOut, value); }
    }
    public class _webSocketMessages : Message
    {
        private string data;
        private double time;
        private string type;
        private game_crash_mess getCrashData;
        private Profile getProfileData;
        private TickMess getTickdData;
        private Bet getBetAcc;
        private string imgPath;
        private string reviewData;

        public string Data { get => data; set => SetProperty(ref data, value); }
        public string Type { get => type; set => SetProperty(ref type, value); }
        public double Time { get => time; set => SetProperty(ref time, value); }
        public string ImgPath { get => imgPath; set => SetProperty(ref imgPath, value); }
        public string ReviewData { get => reviewData; set => SetProperty(ref reviewData, value); } 
        public _webSocketMessages()
        {
            GetCrashData = null;
            GetProfileData = null;
            GetTickdData = null;
            GetBetAccData = null;
            Time = DateTimeOffset.Now.ToUnixTimeSeconds();
        }
        public static _webSocketMessages FromJson(string json)
        {
            JObject o = JObject.Parse(json);
            var m = new _webSocketMessages() { Type = o.SelectToken("type").ToString(), Data = o.SelectToken("data").ToString(), Time = DateTimeOffset.Now.ToUnixTimeSeconds() };
            m.SetData();
            return m;
        }
        private IncomeMessageType MsgType
        {
            get
            {
                switch (Type)
                {
                    case "initial_data":
                        return IncomeMessageType.initial_data;
                    case "game_crash":
                        return IncomeMessageType.game_crash;
                    case "tick":
                        return IncomeMessageType.tick;
                    case "bet_accepted":
                        return IncomeMessageType.bet_accepted;
                    case "lose":
                        return IncomeMessageType.lose;
                    default:
                        return IncomeMessageType.none;
                }
            }
        }
        public DateTime Time_normal => new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Time).ToLocalTime();
        //public string time_str => String.Format("{0:dd.MM.yyyy HH:mm:ss}", time_normal);
        [JsonIgnore]
        public game_crash_mess GetCrashData { get => getCrashData; set => SetProperty(ref getCrashData, value); }
        [JsonIgnore]
        public Profile GetProfileData { get => getProfileData; set => SetProperty(ref getProfileData, value); }
        [JsonIgnore]
        public TickMess GetTickdData { get => getTickdData; set => SetProperty(ref getTickdData, value); }
        [JsonIgnore]
        public Bet GetBetAccData { get => getBetAcc; set => SetProperty(ref getBetAcc, value); }
        public void HandSetData(string type)
        {
            Type = type;
            SetData();
        }
        private void SetData()
        {
            switch (MsgType)
            {
                case IncomeMessageType.initial_data:
                    GetProfileData = JsonConvert.DeserializeObject<Profile>(JObject.Parse(Data).SelectToken("user").ToString());
                    ReviewData = $"{GetProfileData.Username} : {GetProfileData.Balance.NormalPunk.ToString("#0.00",CultureInfo.InvariantCulture)}";
                    ImgPath = "Resources/profile.png";
                    break;
                case IncomeMessageType.game_crash:
                    GetCrashData = JsonConvert.DeserializeObject<game_crash_mess>(JObject.Parse(Data).ToString());
                    ReviewData = $"{GetCrashData.Game_crash_normal.ToString(CultureInfo.InvariantCulture)}";
                    ImgPath = "Resources/explosion.png";
                    break;
                case IncomeMessageType.tick:
                    GetTickdData = JsonConvert.DeserializeObject<TickMess>(JObject.Parse(Data).ToString());
                    ReviewData = $"{GetTickdData.MyCashOut.Value.ToString(CultureInfo.InvariantCulture)}";
                    ImgPath = "Resources/win.png";
                    break;
                case IncomeMessageType.bet_accepted:
                    GetBetAccData = JsonConvert.DeserializeObject<Bet>(JObject.Parse(Data).SelectToken("bet").ToString());
                    ReviewData = $"Bet {GetBetAccData.BetVal.ToString(CultureInfo.InvariantCulture)} CashOut {GetBetAccData.CashOut.ToString(CultureInfo.InvariantCulture)}x";
                    ImgPath = "Resources/chip.png";
                    break;
                case IncomeMessageType.lose:
                    ImgPath = "Resources/lose.png";
                    break;
                case IncomeMessageType.none:
                case IncomeMessageType.connected:
                case IncomeMessageType.disconnected:
                    break;
                default:
                    break;
            }
        }
    }
    public sealed class FooMap : ClassMap<_webSocketMessages>
    {
        public FooMap()
        {
            Map(m => m.Time_normal);
            Map(m => m.GetCrashData.Game_crash_normal);
        }
    }
}
