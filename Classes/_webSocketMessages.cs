using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Linq;

namespace Har_reader
{
    public class _webSocketMessages : Message
    {
        private string data;
        private double time;
        private string type;
        private game_crash_mess getCrashData;
        private Profile getProfileData;
        private TickMess getTickdData;
        private Bet getBetAcc;
        private BetsMessage getBetsMessageData;
        private string imgPath;
        private string reviewData;
        private long gameId;


        public string Data { get => data; set => SetProperty(ref data, value); }
        public string Type { get => type; set => SetProperty(ref type, value); }
        public double Time { get => time; set => SetProperty(ref time, value); }
        public string ImgPath { get => imgPath; set => SetProperty(ref imgPath, value); }
        public string ReviewData { get => reviewData; set => SetProperty(ref reviewData, value); }
        public long GameId { get => gameId; set => SetProperty(ref gameId, value); }
        private _webSocketMessages()
        {
            GetCrashData = null;
            GetProfileData = null;
            GetTickdData = null;
            GetBetAccData = null;
            GetBetsMessageData = null;
            Time = DateTimeOffset.Now.ToUnixTimeSeconds();
        }
        public _webSocketMessages(long gameid) : base()
        {
            GameId = gameid;
        }
        public static _webSocketMessages FromJson(string json, long game_id, object dop_info = null)
        {
            JObject o = JObject.Parse(json);
            var m = new _webSocketMessages(game_id) { Type = o.SelectToken("type").ToString(), Data = o.SelectToken("data").ToString(), Time = DateTimeOffset.Now.ToUnixTimeSeconds() };
            m.SetData(dop_info);
            return m;
        }
        public IncomeMessageType MsgType
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
                        return IncomeMessageType.win;
                    case "bets":
                        return IncomeMessageType.bets;
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
        [JsonIgnore]
        public BetsMessage GetBetsMessageData { get => getBetsMessageData; set => SetProperty(ref getBetsMessageData, value); }
        /// <summary>
        /// Отриц при луз, Положительный при вин
        /// </summary>
        public double ProfitData { get; set; }
        public void HandSetData(string type)
        {
            Type = type;
            SetData();
        }
        private void SetData(object dop_info = null)
        {
            switch (MsgType)
            {
                case IncomeMessageType.initial_data:
                    GameId = (long)JObject.Parse(Data).SelectToken("game_id");
                    GetProfileData = JsonConvert.DeserializeObject<Profile>(JObject.Parse(Data).SelectToken("user").ToString());
                    ReviewData = $"{GetProfileData.Username} : {GetProfileData.Balance.NormalPunk.ToString("#0.000", CultureInfo.InvariantCulture)}";
                    ImgPath = "Resources/profile.png";
                    break;
                case IncomeMessageType.game_crash:
                    if (dop_info is DateTime @date)
                        Time = ((DateTimeOffset)date).ToUnixTimeSeconds();
                    var tok = JObject.Parse(Data);
                    GetCrashData = JsonConvert.DeserializeObject<game_crash_mess>(tok.ToString());
                    if (GetCrashData.Game_crash < 100)
                        GetCrashData.Game_crash = 100;
                    if (dop_info is long @lng)
                    {
                        var z = tok.SelectToken("cashouts").Children().ToList();
                        foreach (var item in z)
                        {
                            var itstr = item.ToString().Trim().Split(":");
                            if (long.Parse(itstr[0].Trim('\"')) == @lng)
                            {
                                CashOuts w = new CashOuts();
                                w.Id = @lng;
                                w.Value = double.Parse(itstr[1], CultureInfo.InvariantCulture);
                                GetCrashData.MyCashOut = w;
                                break;
                            }
                        }
                    }
                    ReviewData = $"{GetCrashData.Game_crash_normal.ToString(CultureInfo.InvariantCulture)}";
                    ImgPath = "Resources/explosion.png";
                    break;
                case IncomeMessageType.win:
                    var t = JObject.Parse(Data);
                    if (dop_info is long @int)
                    {
                        GetTickdData = new TickMess();

                        var z = t.SelectToken("cashouts").Children().ToList();
                        foreach (var item in z)
                        {
                            var itstr = item.ToString().Trim().Split(":");
                            if (long.Parse(itstr[0].Trim('\"')) == @int)
                            {
                                CashOuts w = new CashOuts();
                                w.Id = @int;
                                w.Value = double.Parse(itstr[1], CultureInfo.InvariantCulture);
                                GetTickdData.MyCashOut = w;
                                break;
                            }
                        }
                    }
                    ImgPath = "Resources/win.png";
                    break;
                case IncomeMessageType.bets:
                    GetBetsMessageData = new BetsMessage();
                    var str = Data.TrimStart('[').TrimEnd(']').Trim().Split(",");
                    GetBetsMessageData.UserId = long.Parse(str[0].Trim());
                    GetBetsMessageData.UserName = str[1].Trim();
                    GetBetsMessageData.BetValue = double.Parse(str[2].Trim(), CultureInfo.InvariantCulture);
                    GetBetAccData = new Bet() { JAmount = (long)GetBetsMessageData.BetValue, CashOut = 100 };
                    ReviewData = $"Bet {GetBetAccData.BetVal.ToString("0.##", CultureInfo.InvariantCulture)} CashOut ? x";
                    ImgPath = "Resources/chip.png";
                    break;
                case IncomeMessageType.bet_accepted:
                    GetBetAccData = JsonConvert.DeserializeObject<Bet>(JObject.Parse(Data).SelectToken("bet").ToString());
                    ReviewData = $"Bet {GetBetAccData.BetVal.ToString("0.##", CultureInfo.InvariantCulture)} CashOut {GetBetAccData.CashOut.ToString(CultureInfo.InvariantCulture)}x";
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
}
