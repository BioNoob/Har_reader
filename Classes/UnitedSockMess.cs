using System;

namespace Har_reader
{
    public class UnitedSockMess : Message
    {
        private long gameId;
        private double betVal;
        private double betCashOut;
        private double profit;
        private string statusImage;
        private string dopData;
        private double? gameCrash;
        private bool gameCrashLower;
        private bool isCrashed;
        private string profitStr;
        private DateTime dateOfGame;

        /*
* 1 Добавили запись при старте игры (строка в таблице)
* 2 Если делали ставку добавили ставку в строку таблицы
* 3 Если Вин/Луз добавили в строку (img val)
* 4 Добавили в строку
*/
        public long GameId { get => gameId; set => SetProperty(ref gameId, value); } //555989
        public double BetVal { get => betVal; set => SetProperty(ref betVal, value); } // BET val, cash out if has
        public double BetCashOut { get => betCashOut; set => SetProperty(ref betCashOut, value); } //
        public string ProfitStr { get => profitStr; set => SetProperty(ref profitStr, value); }
        public DateTime DateOfGame { get => dateOfGame; set => SetProperty(ref dateOfGame, value); }
        public double Profit
        {
            get => profit;
            set
            {
                SetProperty(ref profit, value);
                if (ProfitPos == true)
                    ProfitStr = $"+{value.ToString("#0.0#")} $P";
                else if (ProfitPos == false)
                    ProfitStr = $"{value.ToString("#0.0#")} $P";
                else if (ProfitPos == null)
                    ProfitStr = $"";
                SetProperty("ProfitPos");
            }
        }
        public bool? ProfitPos
        {
            get
            {
                if (Profit > 0)
                    return true;
                else if (Profit < 0)
                    return false;
                else
                    return null;
            }
        }
        public bool IsCrashed { get => isCrashed; set => SetProperty(ref isCrashed, value); }
        public string StatusImage { get => statusImage; set => SetProperty(ref statusImage, value); }
        public double? GameCrash { get => gameCrash; set => SetProperty(ref gameCrash, value); }
        public bool GameCrashLower { get => gameCrashLower; set => SetProperty(ref gameCrashLower, value); }
        public string DopData { get => dopData; set => SetProperty(ref dopData, value); }
        public UnitedSockMess(long game)
        {
            GameId = game;
            GameCrash = null;
            IsCrashed = false;
        }
        public void SetDataByMess(IncomeMessageType type, _webSocketMessages mes)
        {
            if(mes.Time == 0)
                mes.Time = DateTimeOffset.Now.ToUnixTimeSeconds();
            DateOfGame = mes.Time_normal;
            switch (type)
            {
                case IncomeMessageType.game_started:
                    StatusImage = "Resources/rock_start.png";
                    break;
                case IncomeMessageType.game_starting:
                    StatusImage = "Resources/rock_prep.png";
                    break;
                case IncomeMessageType.initial_data:
                    StatusImage = "Resources/rock_start.png";
                    break;
                case IncomeMessageType.game_crash:
                    StatusImage = "Resources/explosion.png";
                    GameCrash = mes.GetCrashData.Game_crash_normal;
                    GameCrashLower = mes.GetCrashData.Lower;
                    IsCrashed = true;
                    break;
                case IncomeMessageType.bet_accepted:
                    StatusImage = "Resources/chip.png";
                    BetVal = mes.GetBetAccData.BetVal;
                    BetCashOut = mes.GetBetAccData.CashOut;
                    DopData = $"{BetVal}$P : {BetCashOut}x";
                    break;
                case IncomeMessageType.bets:
                    StatusImage = "Resources/chip.png";
                    BetVal = mes.GetBetAccData.BetVal;
                    BetCashOut = 0;
                    DopData = $"{BetVal}$P";
                    break;
                case IncomeMessageType.lose:
                    StatusImage = "Resources/lose.png";
                    Profit = mes.ProfitData;
                    break;
                case IncomeMessageType.win:
                    StatusImage = "Resources/win.png";
                    Profit = mes.ProfitData;
                    break;
            }
        }

    }
}
