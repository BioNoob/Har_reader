﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Linq;

namespace Har_reader
{

    public class Message : Proper
    {
    }
    public class Bet : Proper
    {
        private double bets;
        private double cashOut;
        private long jAmount;
        private long jCash;

        public Bet() { }
        public Bet(double bet, double cash_out)
        {
            BetVal = bet;
            CashOut = cash_out;
        }
        [JsonProperty("amount")]
        public long JAmount { get => jAmount; set { jAmount = value; BetVal = value / 1000000000d; } }
        [JsonProperty("autoCashOut")]
        public long JCash { get => jCash; set { jCash = value; CashOut = value / 100d; } }
        [JsonIgnore]
        public double BetVal
        {
            get => bets; set
            {
                if (value <= 0.1)
                    value = 0.1;
                SetProperty(ref bets, Math.Round(value, 2));
            }
        }
        [JsonIgnore]
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
        private int _truebet => (int)(BetVal * 1000000000); //0.1 = 1000000000
        private int _truecashout => (int)(CashOut * 100);
        public string GetRequest => "{\"type\":\"place_bet\",\"data\":{\"amount\":" +
            $"{_truebet},\"autoCashOut\":{_truecashout}" + "}}";
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
        private double normalpunk;
        [JsonProperty("punk")]
        public double Punk { get => punk; set { SetProperty(ref punk, value); NormalPunk = Punk / 1000000000; } }//Math.Round(Punk / 1000000000, 2, MidpointRounding.ToNegativeInfinity); } }

        [JsonProperty("ton")]
        public double Ton { get => ton; set => SetProperty(ref ton, value); }
        public double NormalPunk { get => normalpunk; set => SetProperty(ref normalpunk, value); }
        //=> Math.Round(Punk / 1000000000, 2, MidpointRounding.ToNegativeInfinity);
        public void SetPunk(double val)
        {
            Punk = val * 1000000000;
        }
    }
    public class Profile : Message
    {
        private Balance punkBalance;
        private long id;
        private string username;

        public Profile() { Balance = new Balance(); }
        [JsonProperty("balance")]
        public Balance Balance { get => punkBalance; set => SetProperty(ref punkBalance, value); }
        [JsonProperty("id")]
        public long Id { get => id; set => SetProperty(ref id, value); }
        [JsonProperty("username")]
        public string Username { get => username; set => SetProperty(ref username, value); }
    }
    public class StartingMess : Message
    {
        private long id;
        [JsonProperty("game_id")]
        public long Id { get => id; set => SetProperty(ref id, value); }
    }
    public class CashOuts : Proper
    {
        private double val;
        private long id;

        public long Id { get => id; set => SetProperty(ref id, value); }
        public double Value { get => val; set => SetProperty(ref val, value); }
        public double NValue => Value / 100;
    }
    public class TickMess : Message
    {
        //private List<CashOuts> cashOutsLst;
        private CashOuts myCashOut;

        //[JsonProperty("cashouts")]
        //public List<CashOuts> CashOutsLst { get => cashOutsLst; set => SetProperty(ref cashOutsLst, value); }
        //[JsonIgnore]
        public CashOuts MyCashOut { get => myCashOut; set => SetProperty(ref myCashOut, value); }
    }
    public class BetsMessage
    {
        [JsonProperty("0")]
        public long UserId { get; set; }
        [JsonProperty("1")]
        public string UserName { get; set; }
        [JsonProperty("2")]
        public double BetValue { get; set; }
    }
    public class History_record_crash
    {
        [JsonProperty("game_id")]
        public long game_id { get; set; }
        [JsonProperty("game_crash")]
        public int game_crash { get; set; }
        [JsonProperty("created")]
        public string created { get; set; }
        public DateTime Normal_created => DateTime.ParseExact(created, "yyyy-MM-dd'T'HH:mm:ss.FFF'Z'", CultureInfo.InvariantCulture).AddHours(3);
        public _webSocketMessages GetMess => _webSocketMessages.FromJson("{\"type\":\"game_crash\",\"data\":{\"elapsed\":7834,\"game_crash\":" + game_crash + "}}", game_id, Normal_created);
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
                        return IncomeMessageType.tick;
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
                    GameId = (int)JObject.Parse(Data).SelectToken("game_id");
                    GetProfileData = JsonConvert.DeserializeObject<Profile>(JObject.Parse(Data).SelectToken("user").ToString());
                    ReviewData = $"{GetProfileData.Username} : {GetProfileData.Balance.NormalPunk.ToString("#0.000", CultureInfo.InvariantCulture)}";
                    ImgPath = "Resources/profile.png";
                    break;
                case IncomeMessageType.game_crash:
                    if (dop_info is DateTime @date)
                        Time = ((DateTimeOffset)date).ToUnixTimeSeconds();
                    GetCrashData = JsonConvert.DeserializeObject<game_crash_mess>(JObject.Parse(Data).ToString());
                    ReviewData = $"{GetCrashData.Game_crash_normal.ToString(CultureInfo.InvariantCulture)}";
                    ImgPath = "Resources/explosion.png";
                    break;
                case IncomeMessageType.tick:
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

                        //if (t.SelectToken("cashouts") is JObject)
                        //{
                        //    var z = t.SelectToken("cashouts").Children().Single().ToString();
                        //    var str1 = z.ToString().Trim().Split(":");
                        //    if (long.Parse(str1[0].Trim('\"')) == @int)
                        //    {
                        //        CashOuts w = new CashOuts();
                        //        w.Id = @int;//long.Parse(str1[0].Trim());
                        //        w.Value = double.Parse(str1[1].Trim());
                        //        GetTickdData.MyCashOut = w;
                        //    }
                        //}
                        //else if (t.SelectToken("cashouts") is JArray)
                        //{

                        //    var z = t.SelectToken("cashouts").Children().ToList();
                        //    foreach (var item in z)
                        //    {
                        //        var itstr = item.ToString().Trim().Split(":");
                        //        if (long.Parse(itstr[0].Trim('\"')) == @int)
                        //        {
                        //            CashOuts w = new CashOuts();
                        //            w.Id = @int;
                        //            w.Value = double.Parse(itstr[1]);
                        //            GetTickdData.MyCashOut = w;
                        //            break;
                        //        }
                        //    }
                        //}
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
