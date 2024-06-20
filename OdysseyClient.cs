using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.WebSockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Websocket.Client;
using Timer = System.Timers.Timer;

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
        bets,
        lose,
        none,
        connected,
        disconnected
    }
    public enum TimerMess
    {
        fly,
        start
    }
    public interface IStatusSender
    {
        public delegate void SendStatus(string txt);
        public event SendStatus StatusChanged;
    }
    public class OdysseyClient : Proper, IStatusSender
    {
        private Timer _timer = new Timer(1000);
        private Timer expired_timer = new Timer(50);
        private ManualResetEvent exitEvent = new ManualResetEvent(false);
        private WebsocketClient client = new WebsocketClient(new Uri("wss://ton-rocket-server.pfplabs.xyz/"));
        private const string _init_mess = "{\"type\":\"join\",\"data\":{\"initData\":\"\",\"browserInitData\":{\"token\":\"" + "ECHO" + "\",\"locale\":\"en\"}}}";
        private string init_mess = "";
        private static readonly object GATE1 = new object();
        private int counter_of_sec = 0;
        private int gip_count = 0;
        private TimerMess curr_timer;
        private double flytime = 0;
        private long MyId;
        private bool gameInProgress;
        private List<string> PresetStatusBet = new List<string> { "Game in progress", "Game in progress.", "Game in progress..", "Game in progress...", "Game in progress...." };
        private List<string> PresetStatusConn = new List<string> { "Connecting", "Connecting.", "Connecting..", "Connecting...", "Connecting...." };
        private bool haveActiveBet;
        private bool authDone;
        public delegate void GetedMessage(IncomeMessageType type, _webSocketMessages mess);
        public event GetedMessage MessageGeted;
        public event IStatusSender.SendStatus StatusChanged;
        public delegate void TimerDelegate(TimerMess type, double val);
        public event TimerDelegate TimerUpdated;
        TimerMess Curr_timer { get => curr_timer; set { curr_timer = value; flytime = 0; expired_timer.Start(); } }
        public bool GameInProgress { get => gameInProgress; set => SetProperty(ref gameInProgress, value); }
        public bool HaveActiveBet { get => haveActiveBet; set => SetProperty(ref haveActiveBet, value); }
        private bool AuthDone { get => authDone; set => SetProperty(ref authDone, value); }
        private long GameID { get; set; }
        public OdysseyClient()
        {
            expired_timer.Elapsed += Exp_Elapsed;
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
                .Subscribe(t =>
                {
                    GameInProgress = false;
                    Curr_timer = TimerMess.start;
                    JObject o = JObject.Parse(t.Text);
                    GameID = (long)o.SelectToken("data").SelectToken("game_id");
                    StatusChanged?.Invoke("Run is starting");
                });
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
                .Subscribe(t =>
                {
                    GameInProgress = true;
                    expired_timer.Stop();
                    Curr_timer = TimerMess.fly;
                    StatusChanged?.Invoke("Run is started");
                });
            client.MessageReceived
                .Where(t => !string.IsNullOrEmpty(t.Text))
                .Where(t => t.Text.Contains("\"type\":\"bet_accepted\""))
                .ObserveOn(TaskPoolScheduler.Default)
                .Synchronize(GATE1)
                .Subscribe(BetAccMsgRecived);
            client.MessageReceived
                .Where(t => !string.IsNullOrEmpty(t.Text))
                .Where(t => t.Text.Contains("\"type\":\"bets\""))
                .ObserveOn(TaskPoolScheduler.Default)
                .Synchronize(GATE1)
                .Subscribe(BetsMsgRecived);
        }

        private double CalcFly()
        {
            return Math.Floor(100 * Math.Pow(Math.E, 6e-5 * flytime)) / 100;
        }
        private double CalcStart()
        {
            return 5 - (flytime / 1000);
        }
        private void Exp_Elapsed(object sender, ElapsedEventArgs e)
        {
            flytime += 50;
            switch (Curr_timer)
            {
                case TimerMess.fly:
                    TimerUpdated?.Invoke(Curr_timer, CalcFly());
                    break;
                case TimerMess.start:
                    TimerUpdated?.Invoke(Curr_timer, CalcStart());
                    break;
            }
            //var ttt = 
            //Debug.WriteLine($"{ttt.ToString("0.00", CultureInfo.InvariantCulture)}x");
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

        private void GetHistory(_webSocketMessages auth_data)
        {
            JsonConvert.DeserializeObject<List<History_record_crash>>(JObject.Parse(auth_data.Data)
                .SelectToken("table_history").ToString())
                .OrderBy(t => t.game_id)
                .ToList().ForEach(t => MessageGeted?.Invoke(IncomeMessageType.game_crash, t.GetMess));

        }
        private void ConnMessageRecived(ResponseMessage msg)
        {
            if (!AuthDone)
            {
                _timer.Stop();
                StatusChanged?.Invoke("Connected");
                MessageGeted?.Invoke(IncomeMessageType.connected, null);
                var xz = _webSocketMessages.FromJson(msg.Text, 0);
                GameID = xz.GameId;
                MyId = xz.GetProfileData.Id;
                GetHistory(xz);
                MessageGeted?.Invoke(IncomeMessageType.initial_data, xz);
                AuthDone = true;
            }
        }
        private void MyCashOutMsgRecived(ResponseMessage msg)
        {
            if (!AuthDone) return;
            if (!GameInProgress) GameInProgress = true;
            StatusChanged?.Invoke(PresetStatusBet[gip_count++]);
            if (gip_count > PresetStatusBet.Count - 1) gip_count = 0;

            if (MyId != -1)
            {
                if (msg.Text.Contains(MyId.ToString()))
                {
                    HaveActiveBet = false;
                    MessageGeted?.Invoke(IncomeMessageType.tick, _webSocketMessages.FromJson(msg.Text, GameID, MyId));
                }
            }
        }
        private void BetsMsgRecived(ResponseMessage msg)
        {
            if (!AuthDone) return;
            var q = _webSocketMessages.FromJson(msg.Text, GameID);
            if (q.GetBetsMessageData.UserId == MyId)
            {
                HaveActiveBet = true;
                StatusChanged?.Invoke("BET outdoor placed");
                MessageGeted?.Invoke(IncomeMessageType.bets, q);
            }
        }
        private void BetAccMsgRecived(ResponseMessage msg)
        {
            if (!AuthDone) return;
            HaveActiveBet = true;
            StatusChanged?.Invoke("BET placed");
            MessageGeted?.Invoke(IncomeMessageType.bet_accepted, _webSocketMessages.FromJson(msg.Text, GameID));
        }
        private void CrashMessageRecived(ResponseMessage msg)
        {
            if (!AuthDone) return;
            expired_timer.Stop();
            flytime = 0;
            MessageGeted?.Invoke(IncomeMessageType.game_crash, _webSocketMessages.FromJson(msg.Text, GameID));
            StatusChanged?.Invoke("Crush!");
            if (HaveActiveBet)
            {
                var xz = new _webSocketMessages(GameID);
                xz.HandSetData("lose");
                MessageGeted?.Invoke(IncomeMessageType.lose, xz);
            }
            HaveActiveBet = false;

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
            if (!AuthDone) return;
            StatusChanged?.Invoke("Try place a bet");
            int a = 0;
            while (GameInProgress)
            {
                await Task.Delay(500);
            }
            if (!client.Send(bet.GetRequest))
                StatusChanged?.Invoke("Error place bet");
        }
        public void StopClient()
        {
            if (client.IsStarted)
            {
                expired_timer.Stop();
                flytime = 0;
                AuthDone = false;
                HaveActiveBet = false;
                GameInProgress = false;
                MyId = -1;
                StatusChanged?.Invoke("Disconnected");
                MessageGeted?.Invoke(IncomeMessageType.disconnected, null);
                exitEvent.Set();
                client.Stop(WebSocketCloseStatus.Empty, string.Empty);
            }
        }
    }
}
