﻿using CsvHelper.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private int jAmount;
        private int jCash;

        public Bet() { }
        public Bet(double bet, double cash_out)
        {
            BetVal = bet;
            CashOut = cash_out;
        }
        [JsonProperty("amount")]
        public int JAmount { get => jAmount; set { jAmount = value; BetVal = value / 1000000000d; } }
        [JsonProperty("autoCashOut")]
        public int JCash { get => jCash; set { jCash = value; CashOut = value / 100d; } }
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
        private int id;

        public int Id { get => id; set => SetProperty(ref id, value); }
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
    public class History_record_crash
    {
        [JsonProperty("game_id")]
        public int game_id { get; set; }
        [JsonProperty ("game_crash")]
        public int game_crash { get; set; }
        [JsonProperty("created")]
        public string created { get; set; }
        public DateTime Normal_created => DateTime.ParseExact(created, "yyyy-MM-dd'T'HH:mm:ss.FFF'Z'", CultureInfo.InvariantCulture).AddHours(3);
        public _webSocketMessages GetMess => _webSocketMessages.FromJson("{\"type\":\"game_crash\",\"data\":{\"elapsed\":7834,\"game_crash\":" + game_crash + "}}", Normal_created);
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
        public static _webSocketMessages FromJson(string json, object dop_info = null)
        {
            JObject o = JObject.Parse(json);
            var m = new _webSocketMessages() { Type = o.SelectToken("type").ToString(), Data = o.SelectToken("data").ToString(), Time = DateTimeOffset.Now.ToUnixTimeSeconds() };
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
        private void SetData(object dop_info = null)
        {
            switch (MsgType)
            {
                case IncomeMessageType.initial_data:
                    GetProfileData = JsonConvert.DeserializeObject<Profile>(JObject.Parse(Data).SelectToken("user").ToString());
                    ReviewData = $"{GetProfileData.Username} : {GetProfileData.Balance.NormalPunk.ToString("#0.00", CultureInfo.InvariantCulture)}";
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
                    GetTickdData = new TickMess();
                    if (t.SelectToken("cashouts") is JObject)
                    {
                        CashOuts w = JsonConvert.DeserializeObject<CashOuts>(t.SelectToken("cashouts").ToString());
                        GetTickdData.MyCashOut = w;
                    }
                    else if (t.SelectToken("cashouts") is JArray)
                    {
                        List<CashOuts> wl = JsonConvert.DeserializeObject<List<CashOuts>>(t.SelectToken("cashouts").ToString());
                        if (dop_info is int @int)
                            GetTickdData.MyCashOut = wl.Single(q => q.Id == @int);
                    }
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
