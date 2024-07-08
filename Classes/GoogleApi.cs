using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MathNet.Numerics.Statistics;

namespace Har_reader
{
    public class SavedData : Proper
    {
        private Dictionary<long, double> saved = new Dictionary<long, double>();

        public SavedData()
        {

        }
        public Dictionary<long, double> Saved { get => saved; set => SetProperty(ref saved, value); }
        public double GetMedian(int counter)
        {
            return Saved.Values.TakeLast(counter).Median();
        }
        public void AddRange(IEnumerable<UnitedSockMess> us)
        {
            foreach (var item in us)
            {
                if (!Saved.TryAdd(item.GameId, item.GameCrash.HasValue ? item.GameCrash.Value : 1))
                {
                    //КЛЮЧ УЖЕ БЫЛ
                    System.Diagnostics.Debug.WriteLine($"{item.GameId} tried again add");
                }
            }
        }
    }
    public class GoogleApi : IStatusSender
    {
        static string[] Scopes = { SheetsService.Scope.Spreadsheets };
        static string ApplicationName = "OdysseySheets";
        static string SpreadSheetId = "1PhQbJpiTdkJYacoNTBvDS2ECvRqjXqDm6VWpi7JwR28";
        private bool ProfileSet = false;
        public void SetProfile(Profile myProfile)
        {
            ProfileSet = true;
            MyProfile = myProfile;
            StatusChanged?.Invoke("Profile getted.. Ready to save");
        }
        public GoogleApi()
        {

        }

        public event IStatusSender.SendStatus StatusChanged;

        //https://docs.google.com/spreadsheets/d/1PhQbJpiTdkJYacoNTBvDS2ECvRqjXqDm6VWpi7JwR28/edit?usp=sharing
        private SheetsService AuthorizeGoogleApp()
        {
            var secret = GoogleCredential.FromJson(Properties.Resources.odyseeyproj_4a113e813b35);
            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = secret,
                ApplicationName = ApplicationName,
            });

            return service;
        }
        Profile MyProfile { get; set; }
        string MyUserName => MyProfile.Username;
        List<Sheet> DocSheets { get; set; }
        SheetsService Service { get; set; }
        Sheet MySheet => DocSheets.SingleOrDefault(t => t.Properties.Title == MyUserName);
        //private List<long> Saved { get; set; } =  new List<long>();
        public SavedData SavedData { get; set; } = new SavedData();
        public bool SaveInProgress { get; set; } = false;
        private void SetConnAndRead()
        {
            Service = AuthorizeGoogleApp();
            //get table info
            GetSheets();
            StatusChanged?.Invoke("Sheets data readed");
        }
        private void AddEmptyRows(int count)
        {
            UpdateSheetPropertiesRequest uspr = new UpdateSheetPropertiesRequest();
            uspr.Properties = MySheet.Properties;
            uspr.Fields = "*";
            uspr.Properties.GridProperties.RowCount += count;
            Service.Spreadsheets.BatchUpdate(new BatchUpdateSpreadsheetRequest() { Requests = new List<Request> { new Request { UpdateSheetProperties = uspr } } }, SpreadSheetId).Execute();

        }
        private void GetSheets()
        {
            StatusChanged?.Invoke($"Read sheets data");
            var request = Service.Spreadsheets.Get(SpreadSheetId);
            var response = request.Execute();
            DocSheets = response.Sheets.ToList();
            if (MySheet != null)
            {
                //var val = Service.Spreadsheets.Values.Get(SpreadSheetId, $"{MySheet.Properties.Title}!A2:A").Execute();
                var val = Service.Spreadsheets.Values.Get(SpreadSheetId, $"Conclusion!A2:C").Execute();
                if (val.Values != null)
                {
                    foreach (var item in val.Values)
                    {
                        var _id = long.Parse(item[0].ToString());
                        var _val = float.Parse(item[2].ToString());
                        SavedData.Saved.Add(_id, _val);
                    }
                    if (SavedData.Saved.Count > MySheet.Properties.GridProperties.RowCount - 1000)
                    {
                        AddEmptyRows(10000);
                    }
                }
            }
        }
        public async void DoSheetSave(IEnumerable<UnitedSockMess> mess)
        {
            await Task.Run(() =>
            {
                try
                {
                    if (!ProfileSet)
                    {
                        StatusChanged?.Invoke("No profile to save");
                        return;
                    }

                    StatusChanged?.Invoke("Saving Data to google");
                    var buf = mess.Select(t => t.GameId).Except(SavedData.Saved.Keys);
                    if (buf.Count() < 1)
                    {
                        StatusChanged?.Invoke("Nothing to save");
                        return;
                    }
                    SaveInProgress = true;
                    if (Service is null)
                        SetConnAndRead();
                    var to_save = mess.Where(t => buf.Contains(t.GameId)).Where(t => t.GameCrash != null);
                    InsertLogData(to_save);
                    SavedData.AddRange(to_save);
                    StatusChanged?.Invoke("Saved!");
                    SaveInProgress = false;
                }
                catch (System.Exception)
                {
                    StatusChanged?.Invoke("Error on save!");
                }

            });
        }
        private void InsertLogData(IEnumerable<UnitedSockMess> mess)
        {
            CheckSheetsExist();
            StatusChanged?.Invoke("Generate insert request");
            Request rq = new Request();
            AppendCellsRequest aprq = new AppendCellsRequest();
            aprq.SheetId = MySheet.Properties.SheetId;
            aprq.Fields = "*";//"userEnteredValue";
            aprq.Rows = new List<RowData>();
            var aq = new List<UnitedSockMess>();
            //mess.Select(o => o.GameId).Distinct().ToList().ForEach(w =>
            //  {
            //      var id_mess = mess.Where(t => t.GameId == w);
            //      var a = id_mess.FirstOrDefault(t => t.MsgType == IncomeMessageType.game_crash);
            //      if (a != null)
            //      {
            //          a.ProfitData = id_mess.Any(t => t.ProfitData != 0) ? id_mess.Single(t => t.ProfitData != 0).ProfitData : 0;
            //          aq.Add(a);
            //      }
            //  });
            aq = new List<UnitedSockMess>(mess.OrderBy(t => t.GameId));
            foreach (var item in aq)
            {
                RowData row = new RowData();
                row.Values = new List<CellData>
                {
                    new CellData() { UserEnteredValue = new ExtendedValue() { NumberValue = item.GameId } },
                    new CellData() {
                        UserEnteredFormat = new CellFormat() { NumberFormat = new NumberFormat() { Type = "DATE_TIME", Pattern = "dd.MM.yyyy hh:mm:ss" }, },
                        UserEnteredValue = new ExtendedValue() { NumberValue = item.DateOfGame.ToOADate() },
                    },
                    new CellData() { UserEnteredValue = new ExtendedValue() { NumberValue = item.GameCrash } }
                };
                if (item.Profit < 0)
                {
                    row.Values.Add(new CellData() { UserEnteredValue = new ExtendedValue() { NumberValue = item.Profit } });
                    row.Values.Add(new CellData() { UserEnteredValue = new ExtendedValue() { StringValue = "" } });
                }
                if (item.Profit > 0)
                {
                    row.Values.Add(new CellData() { UserEnteredValue = new ExtendedValue() { StringValue = "" } });
                    row.Values.Add(new CellData() { UserEnteredValue = new ExtendedValue() { NumberValue = item.Profit } });
                }
                aprq.Rows.Add(row);
            }
            rq.AppendCells = aprq;
            BatchUpdateSpreadsheetRequest updateRequest = new BatchUpdateSpreadsheetRequest
            {
                Requests = new List<Request>() { rq }
            };
            _ = Service.Spreadsheets.BatchUpdate(updateRequest, SpreadSheetId).Execute();
            StatusChanged?.Invoke("Saving Done!");
        }
        private void CheckSheetsExist()
        {
            if (MySheet is null)//!DocSheets.Any(t => t.Properties.Title == MyUserName))
            {
                StatusChanged?.Invoke($"Create new sheet for user {MyUserName}");
                var addSheetRequest = new AddSheetRequest();
                addSheetRequest.Properties = new SheetProperties();
                addSheetRequest.Properties.Title = MyUserName;
                BatchUpdateSpreadsheetRequest batchUpdateSpreadsheetRequest = new BatchUpdateSpreadsheetRequest();
                batchUpdateSpreadsheetRequest.Requests = new List<Request>();
                batchUpdateSpreadsheetRequest.Requests.Add(new Request
                {
                    AddSheet = addSheetRequest
                });
                var valgetreq = Service.Spreadsheets.Values.Get(SpreadSheetId, "Conclusion!A2:A2");//.Execute();
                valgetreq.ValueRenderOption = SpreadsheetsResource.ValuesResource.GetRequest.ValueRenderOptionEnum.FORMULA;
                var formula = valgetreq.Execute().Values[0][0].ToString();
                //докинули в формулу сбоку новый лист по юзеру
                formula = formula.Replace("};", $";{MyUserName}!A2:B" + "};");
                UpdateCellsRequest ucr = new UpdateCellsRequest();
                ucr.Fields = "*";
                ucr.Rows = new List<RowData>();
                ucr.Rows.Add(new RowData()
                {
                    Values = new List<CellData>()
                    {
                        new CellData()
                        {
                            UserEnteredValue = new ExtendedValue() { FormulaValue = formula }
                        }
                    }
                });
                ucr.Range = new GridRange() { StartColumnIndex = 0, StartRowIndex = 1, SheetId = DocSheets.Single(t => t.Properties.Title == "Conclusion").Properties.SheetId };
                StatusChanged?.Invoke($"Summary page change");
                batchUpdateSpreadsheetRequest.Requests.Add(new Request { UpdateCells = ucr });
                var batchUpdateRequest = Service.Spreadsheets.BatchUpdate(batchUpdateSpreadsheetRequest, SpreadSheetId);
                var answ = batchUpdateRequest.Execute();
                GetSheets();
                //var sheetprop = answ.Replies.Where(w => w.AddSheet != null).Select(w => w.AddSheet).Single().Properties;
                //Sheet t = new Sheet();
                //t.Properties = new SheetProperties();
                //t.Properties.SheetId = sheetprop.SheetId;
                //t.Properties.Title = sheetprop.Title;
                //DocSheets.Add(t);
                AppendCellsRequest aprq = new AppendCellsRequest();
                aprq.SheetId = MySheet.Properties.SheetId;
                aprq.Fields = "*";//"userEnteredValue";
                aprq.Rows = new List<RowData>();
                RowData row = new RowData();
                row.Values = new List<CellData>();
                row.Values.Add(new CellData() { UserEnteredValue = new ExtendedValue() { StringValue = "Game ID" } });
                row.Values.Add(new CellData() { UserEnteredValue = new ExtendedValue() { StringValue = "Game Date" } });
                row.Values.Add(new CellData() { UserEnteredValue = new ExtendedValue() { StringValue = "Crash value" } });
                row.Values.Add(new CellData() { UserEnteredValue = new ExtendedValue() { StringValue = "Lose value" } });
                row.Values.Add(new CellData() { UserEnteredValue = new ExtendedValue() { StringValue = "Win value" } });
                aprq.Rows.Add(row);
                BatchUpdateSpreadsheetRequest updateRequest = new BatchUpdateSpreadsheetRequest
                {
                    Requests = new List<Request>() { new Request() { AppendCells = aprq } }
                };
                _ = Service.Spreadsheets.BatchUpdate(updateRequest, SpreadSheetId).Execute();
            }
            else
            {
                StatusChanged?.Invoke("User sheets Founded! Get last data");
            }


        }
    }
}
