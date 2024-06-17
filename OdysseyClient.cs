using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using Websocket.Client;
using System.Timers;
using Timer = System.Timers.Timer;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Globalization;
using Newtonsoft.Json.Linq;

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
    public interface IStatusSender
    {
        public delegate void SendStatus(string txt);
        public event SendStatus StatusChanged;
    }
    public class OdysseyClient : Proper, IStatusSender
    {
        private Timer _timer = new Timer(1000);
        private ManualResetEvent exitEvent = new ManualResetEvent(false);
        private WebsocketClient client = new WebsocketClient(new Uri("wss://ton-rocket-server.pfplabs.xyz/"));
        private const string _init_mess = "{\"type\":\"join\",\"data\":{\"initData\":\"\",\"browserInitData\":{\"token\":\"" + "ECHO" + "\",\"locale\":\"en\"}}}";
        private string init_mess = "";
        private static readonly object GATE1 = new object();
        private int counter_of_sec = 0;
        private int MyId;
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
                .Subscribe(t => { GameInProgress = false; StatusChanged?.Invoke("Run is starting"); });
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
                .Subscribe(t => { GameInProgress = true; StatusChanged?.Invoke("Run is started"); });
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
        public delegate void GetedMessage(IncomeMessageType type, _webSocketMessages mess);
        public event GetedMessage MessageGeted;
        public event IStatusSender.SendStatus StatusChanged;

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
                var xz = _webSocketMessages.FromJson(msg.Text);
                MessageGeted?.Invoke(IncomeMessageType.initial_data, xz);
                MyId = xz.GetProfileData.Id;
                GetHistory(xz);
                AuthDone = true;
            }
        }
        int gip_count = 0;
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
                    MessageGeted?.Invoke(IncomeMessageType.tick, _webSocketMessages.FromJson(msg.Text, MyId));
                }
            }
        }
        private void BetAccMsgRecived(ResponseMessage msg)
        {
            if (!AuthDone) return;
            HaveActiveBet = true;
            StatusChanged?.Invoke("BET placed");
            MessageGeted?.Invoke(IncomeMessageType.bet_accepted, _webSocketMessages.FromJson(msg.Text));
        }
        private void CrashMessageRecived(ResponseMessage msg)
        {
            if (!AuthDone) return;
            MessageGeted?.Invoke(IncomeMessageType.game_crash, _webSocketMessages.FromJson(msg.Text));
            StatusChanged?.Invoke("Crush!");
            if (HaveActiveBet)
            {
                var xz = new _webSocketMessages();
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
