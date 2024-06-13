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
        game_starting,
        tick,
        none
    }
    public class OdysseyClient : Proper
    {
        private ManualResetEvent exitEvent = new ManualResetEvent(false);
        private WebsocketClient client = new WebsocketClient(new Uri("wss://ton-rocket-server.pfplabs.xyz/"));
        private const string _init_mess = "{\"type\":\"join\",\"data\":{\"initData\":\"\",\"browserInitData\":{\"token\":\"" + "ECHO" + "\",\"locale\":\"en\"}}}";
        private string init_mess = "";
        private static readonly object GATE1 = new object();
        private bool is_connected;
        public bool Is_connected { get => is_connected; set => SetProperty(ref is_connected, value); }
        private Profile My;
        public OdysseyClient()
        {
            My = new Profile();
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
                .Subscribe(StartingMessageRecived);
            client.MessageReceived
                .Where(t => !string.IsNullOrEmpty(t.Text))
                .Where(t => t.Text.Contains("\"type\":\"tick\""))
                .Where(t => t.Text.Contains($"{My.Username}"))
                .ObserveOn(TaskPoolScheduler.Default)
                .Synchronize(GATE1)
                .Subscribe(MyCashOutMsgRecived);
        }
        public delegate void SendStatus(string txt);
        public event SendStatus StatusChanged;
        public delegate void GetedMessage(IncomeMessageType type, _webSocketMessages mess);
        public event GetedMessage MessageGeted;
        private void ConnMessageRecived(ResponseMessage msg)
        {
            _webSocketMessages wm = _webSocketMessages.FromJson(msg.Text);
        }
        private void StartingMessageRecived(ResponseMessage msg)
        {
            //wm.Data = msg.Text;
            _webSocketMessages wm = _webSocketMessages.FromJson(msg.Text);
        }
        private void MyCashOutMsgRecived(ResponseMessage msg)
        {

        }
        private void CrashMessageRecived(ResponseMessage msg)
        {
            _webSocketMessages wm = _webSocketMessages.FromJson(msg.Text);
        }
        public async void Connect(string token)
        {
            init_mess = _init_mess.Replace("ECHO", token);
            await client.Start();

            client.Send(init_mess);
            exitEvent.WaitOne();
        }
        public void StopClient()
        {
            if (client.IsStarted)
            {
                Is_connected = false;
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
        public double BetVal { get => bets; set => SetProperty(ref bets, value); }
        public double CashOut { get => cashOut; set => SetProperty(ref cashOut, value); }
        private double _truebet => BetVal * 100000000; //0.1 = 100000000
        private double _truecashout => CashOut * 100; //0.1 = 100000000
        public string GetRequest => "{\"type\":\"place_bet\",\"data\":{\"amount\":" +
            $"{_truecashout},\"autoCashOut\":{_truebet}}}";
    }
    public class game_crash_mess : Message
    {
        private double elapsed;
        private double game_crash;

        public double Elapsed { get => elapsed; set => SetProperty(ref elapsed, value); }
        public double Game_crash { get => game_crash; set => SetProperty(ref game_crash, value); }
        public bool Lower => Game_crash_normal < 2.0;
        public double Game_crash_normal => Game_crash / 100d;
    }
    public partial class Balance
    {
        [JsonProperty("punk")]
        public long Punk { get; set; }

        [JsonProperty("ton")]
        public long Ton { get; set; }
    }
    public class Profile : Message
    {
        private Balance punkBalance;
        private int id;
        private string username;

        public Profile() { }
        [JsonProperty("balance")]
        public Balance PunkBalance { get => punkBalance; set => SetProperty(ref punkBalance, value); }
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
    public class TickMess : Message
    {
        private List<CashOuts> cashOutsLst;

        public class CashOuts : Proper
        {
            private double val;
            private string id;

            public string Id { get => id; set => SetProperty(ref id, value); }
            public double Value { get => val; set => SetProperty(ref val, value); }
        }
        [JsonProperty("cashouts")]
        public List<CashOuts> CashOutsLst { get => cashOutsLst; set => cashOutsLst = value; }
    }
    public class _webSocketMessages : Message
    {
        private string data;
        private double time;
        private string type;
        public string Data { get => data; set => SetProperty(ref data, value); }
        public string Type { get => type; set => SetProperty(ref type, value); }
        public double Time { get => time; set => SetProperty(ref time, value); }
        public static _webSocketMessages FromJson(string json)
        {
            JObject o = JObject.Parse(json);
            return new _webSocketMessages() {Type = o.SelectToken("type").ToString(), Data = o.SelectToken("data").ToString(), Time = DateTimeOffset.Now.ToUnixTimeSeconds()};
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
                    case "game_starting":
                        return IncomeMessageType.game_starting;
                    case "tick":
                        return IncomeMessageType.tick;
                    default:
                        return IncomeMessageType.none;
                }
            }
        }
        public DateTime Time_normal => new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Time).ToLocalTime();
        //public string time_str => String.Format("{0:dd.MM.yyyy HH:mm:ss}", time_normal);
        public game_crash_mess GetCrashData => MsgType == IncomeMessageType.game_crash ? JsonConvert.DeserializeObject<game_crash_mess>(JObject.Parse(Data).ToString()) : null;
        public Profile GetProfileData => MsgType == IncomeMessageType.initial_data ? JsonConvert.DeserializeObject<Profile>(JObject.Parse(Data).SelectToken("user").ToString()) : null;
        public StartingMess GetStartingData => MsgType == IncomeMessageType.game_starting ? JsonConvert.DeserializeObject<StartingMess>(JObject.Parse(Data).ToString()) : null;
        public TickMess GetTickdData => MsgType == IncomeMessageType.tick ? JsonConvert.DeserializeObject<TickMess>(JObject.Parse(Data).ToString()) : null;

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
