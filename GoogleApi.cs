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
        static string SheetId = "1PhQbJpiTdkJYacoNTBvDS2ECvRqjXqDm6VWpi7JwR28";

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
        string MyUserName { get; set; }
        List<Sheet> DocSheets { get; set; }
        SheetsService Service { get; set; }
        Sheet MySheet => DocSheets.SingleOrDefault(t => t.Properties.Title == MyUserName);
        private void SetConnAndRead(string myusername)
        {
            Service = AuthorizeGoogleApp();
            MyUserName = myusername;
            //get table info
            GetSheets();
            StatusChanged?.Invoke("Sheets data readed");
        }
        private void GetSheets()
        {
            var request = Service.Spreadsheets.Get(SheetId);
            var response = request.Execute();
            DocSheets = response.Sheets.ToList();
        }
        public async void DoSheetSave(string username, IEnumerable<_webSocketMessages> mess)
        {
            await Task.Run(() =>
            {
                StatusChanged?.Invoke("Saving Data to google");
                SetConnAndRead(username);
                InsertLogData(mess);
                StatusChanged?.Invoke("Saving as can");
            });

        }
        private void InsertLogData(IEnumerable<_webSocketMessages> mess)
        {
            CheckSheetsExist();
            Request rq = new Request();
            AppendCellsRequest aprq = new AppendCellsRequest();
            aprq.SheetId = MySheet.Properties.SheetId;
            aprq.Fields = "*";//"userEnteredValue";
            aprq.Rows = new List<RowData>();
            var aq = mess.OrderBy(t => t.Time_normal);
            foreach (var item in aq)
            {
                RowData row = new RowData();
                row.Values = new List<CellData>();
                row.Values.Add(new CellData()
                {
                    UserEnteredFormat = new CellFormat() { NumberFormat = new NumberFormat() { Type = "DATE_TIME", Pattern = "dd.MM.yyyy hh:mm:ss" }, },
                    UserEnteredValue = new ExtendedValue() { NumberValue = item.Time_normal.ToOADate() },
                }
                );
                row.Values.Add(new CellData() { UserEnteredValue = new ExtendedValue() { NumberValue = item.GetCrashData.Game_crash_normal } });
                aprq.Rows.Add(row);
            }
            StatusChanged?.Invoke("Generate insert request");
            rq.AppendCells = aprq;
            BatchUpdateSpreadsheetRequest updateRequest = new BatchUpdateSpreadsheetRequest
            {
                Requests = new List<Request>() { rq }
            };
            _ = Service.Spreadsheets.BatchUpdate(updateRequest, SheetId).Execute();
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
                var valgetreq = Service.Spreadsheets.Values.Get(SheetId, "Conclusion!A1:A1");//.Execute();
                valgetreq.ValueRenderOption = SpreadsheetsResource.ValuesResource.GetRequest.ValueRenderOptionEnum.FORMULA;
                var formula = valgetreq.Execute().Values[0][0].ToString();
                //докинули в формулу сбоку новый лист по юзеру
                formula = formula.Replace("};", $"{MyUserName}!A:B" + "};");
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
                ucr.Range = new GridRange() { StartColumnIndex = 0, StartRowIndex = 0, SheetId = DocSheets.Single(t => t.Properties.Title == "Conclusion").Properties.SheetId };

                batchUpdateSpreadsheetRequest.Requests.Add(new Request { UpdateCells = ucr });
                var batchUpdateRequest = Service.Spreadsheets.BatchUpdate(batchUpdateSpreadsheetRequest, SheetId);
                _ = batchUpdateRequest.Execute();
            }
            StatusChanged?.Invoke("User sheets Founded!");
            GetSheets();

        }
    }
}
