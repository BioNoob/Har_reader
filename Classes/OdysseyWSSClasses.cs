using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Windows.Media;

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
        private CashOuts myCashOut;
        public double Elapsed { get => elapsed; set => SetProperty(ref elapsed, value); }
        public double Game_crash { get => game_crash; set => SetProperty(ref game_crash, value); }
        public double Game_crash_normal => Game_crash / 100d;
        [JsonIgnore]
        public bool Lower { get => lower; set => SetProperty(ref lower, value); }
        public CashOuts MyCashOut { get => myCashOut; set => SetProperty(ref myCashOut, value); }
    }
    public partial class Balance : Proper
    {
        private double punk;
        private double ton;
        private double normalpunk;
        [JsonProperty("punk")]
        public double Punk { get => punk; set {  NormalPunk = value / 1000000000; SetProperty(ref punk, value); } }

        [JsonProperty("ton")]
        public double Ton { get => ton; set => SetProperty(ref ton, value); }
        public double NormalPunk { get => normalpunk; set => SetProperty(ref normalpunk, value); }
        //=> Math.Round(Punk / 1000000000, 2, MidpointRounding.ToNegativeInfinity);
        public void SetPunk(double val)
        {
            Punk = val * 1000000000;
        }

        public void SetByAnotherBalance(Balance b)
        {
            Ton = b.Ton;
            Punk = b.Punk;
        }
    }
    public class Profile : Message
    {
        private Balance punkBalance;
        private long id;
        private string username;

        public Profile() { Balance = new Balance(); Balance.PropertyChanged += Balance_PropertyChanged; }

        private void Balance_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            SetProperty(e.PropertyName);
        }
        public void SetByAnotherProfile(Profile prof)
        {
            if (Balance is null)
                Balance = new Balance();
            Balance.SetByAnotherBalance(prof.Balance);
            Id = prof.Id;
            Username = prof.Username;
        }

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
}
