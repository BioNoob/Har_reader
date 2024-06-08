using CsvHelper.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Har_reader
{
    public class Profile : Proper
    {
        private double punkBalance;
        private int id;
        private string username;

        public Profile() { }
        [JsonProperty("punk")]
        public double PunkBalance { get => punkBalance; set => SetProperty(ref punkBalance, value); }
        [JsonProperty("id")]
        public int Id { get => id; set => SetProperty(ref id, value); }
        [JsonProperty("username")]
        public string Username { get => username; set => SetProperty(ref username, value); }
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
    public class game_crash_mess : Proper
    {
        private double elapsed;
        private double game_crash;

        public double Elapsed { get => elapsed; set => SetProperty(ref elapsed, value); }
        public double Game_crash { get => game_crash; set => SetProperty(ref game_crash, value); }
        public bool Lower => Game_crash_normal < 2.0;
        public double Game_crash_normal => Game_crash / 100d;
    }
    public class _webSocketMessages : Proper
    {
        private string data;
        private double time;
        private string type;

        public string Type { get => type; set => SetProperty(ref type, value); }
        public double Time { get => time; set => SetProperty(ref time, value); }
        public string Data { get => data; set => SetProperty(ref data, value); }
        public DateTime Time_normal => new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Time).ToLocalTime();
        //public string time_str => String.Format("{0:dd.MM.yyyy HH:mm:ss}", time_normal);
        public game_crash_mess GetData => JsonConvert.DeserializeObject<game_crash_mess>(JObject.Parse(Data).SelectToken("data").ToString());

    }
    public sealed class FooMap : ClassMap<_webSocketMessages>
    {
        public FooMap()
        {
            Map(m => m.Time_normal);
            Map(m => m.GetData.Game_crash_normal);
        }
    }
}
