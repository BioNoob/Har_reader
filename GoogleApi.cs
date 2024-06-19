using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Har_reader
{
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
        private List<long> Saved = new List<long>();
        public bool SaveInProgress { get; set; } = false;
        private void SetConnAndRead()
        {
            Service = AuthorizeGoogleApp();
            //get table info
            GetSheets();
            StatusChanged?.Invoke("Sheets data readed");
        }
        private void GetSheets()
        {
            StatusChanged?.Invoke($"Read sheets data");
            var request = Service.Spreadsheets.Get(SpreadSheetId);
            var response = request.Execute();
            DocSheets = response.Sheets.ToList();
            if (MySheet != null)
            {
                var val = Service.Spreadsheets.Values.Get(SpreadSheetId, $"{MySheet.Properties.Title}!A2:A").Execute();
                if (val.Values != null)
                    Saved = val.Values.Select(t => long.Parse(t[0].ToString())).ToList();
            }
        }
        public async void DoSheetSave(IEnumerable<_webSocketMessages> mess)
        {
            await Task.Run(() =>
            {
                if (!ProfileSet)
                {
                    StatusChanged?.Invoke("No profile to save");
                    return;
                }

                StatusChanged?.Invoke("Saving Data to google");
                var buf = mess.Select(t => t.GameId).Except(Saved);
                if (buf.Count() < 1)
                {
                    StatusChanged?.Invoke("Nothing to save");
                    return;
                }
                SaveInProgress = true;
                if (Service is null)
                    SetConnAndRead();
                InsertLogData(mess.Where(t => buf.Contains(t.GameId)));
                Saved.AddRange(buf);
                StatusChanged?.Invoke("Saved!");
                SaveInProgress = false;
            });

        }
        private void InsertLogData(IEnumerable<_webSocketMessages> mess)
        {
            CheckSheetsExist();
            StatusChanged?.Invoke("Generate insert request");
            Request rq = new Request();
            AppendCellsRequest aprq = new AppendCellsRequest();
            aprq.SheetId = MySheet.Properties.SheetId;
            aprq.Fields = "*";//"userEnteredValue";
            aprq.Rows = new List<RowData>();
            //var aq = mess.OrderBy(t => t.GameId).Where(t => t.MsgType != IncomeMessageType.connected 
            var aq = new List<_webSocketMessages>();
            mess.Select(o => o.GameId).Distinct().ToList().ForEach(w =>
              {
                  var id_mess = mess.Where(t => t.GameId == w);
                  var a = id_mess.FirstOrDefault(t => t.MsgType == IncomeMessageType.game_crash);
                  if (a != null)
                  {
                      a.ProfitData = id_mess.Any(t => t.ProfitData != 0) ? id_mess.Single(t => t.ProfitData != 0).ProfitData : 0;
                      aq.Add(a);
                  }
              });
            aq = new List<_webSocketMessages>(aq.OrderBy(t => t.GameId));
            foreach (var item in aq)
            {
                RowData row = new RowData();
                row.Values = new List<CellData>();
                //row.Values.Add(new CellData()
                //{
                //    UserEnteredFormat = new CellFormat() { NumberFormat = new NumberFormat() { Type = "DATE_TIME", Pattern = "dd.MM.yyyy hh:mm:ss" }, },
                //    UserEnteredValue = new ExtendedValue() { NumberValue = item.Time_normal.ToOADate() },
                //}
                //);
                row.Values.Add(new CellData() { UserEnteredValue = new ExtendedValue() { NumberValue = item.GameId } });
                row.Values.Add(new CellData() { UserEnteredValue = new ExtendedValue() { NumberValue = item.GetCrashData.Game_crash_normal } });
                if (item.ProfitData < 0)
                {
                    row.Values.Add(new CellData() { UserEnteredValue = new ExtendedValue() { NumberValue = item.ProfitData } });
                    row.Values.Add(new CellData() { UserEnteredValue = new ExtendedValue() { StringValue = "" } });
                }
                if (item.ProfitData > 0)
                {
                    row.Values.Add(new CellData() { UserEnteredValue = new ExtendedValue() { StringValue = "" } });
                    row.Values.Add(new CellData() { UserEnteredValue = new ExtendedValue() { NumberValue = item.ProfitData } });
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
