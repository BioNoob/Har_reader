using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MathNet.Numerics.Statistics;
using System.Collections.ObjectModel;
using System;

namespace Har_reader
{
    public class SavedData : Proper
    {
        public long Id { get; set; }
        public DateTime Dt { get; set; }
        public double Crash { get; set; }
        public double Profit { get; set; }
        public SavedData()
        {
            Id = -1;
            Dt = new DateTime();
            Crash = 0d;
            Profit = 0d;
        }
        public override string ToString()
        {
            return $"{Id} {Crash}";
        }
    }
    public class GoogleApi : Proper, IStatusSender
    {
        static string[] Scopes = { SheetsService.Scope.Spreadsheets };
        static string ApplicationName = "OdysseySheets";
        static string SpreadSheetId = "1PhQbJpiTdkJYacoNTBvDS2ECvRqjXqDm6VWpi7JwR28";
        private bool ProfileSet = false;
        private int autoSaveCounter;

        public void SetProfile(Profile myProfile)
        {
            ProfileSet = true;
            MyProfile = myProfile;
            StatusChanged?.Invoke("Profile getted.. Ready to save");
        }
        public GoogleApi()
        {
            ToSave.CollectionChanged += ToSave_CollectionChanged;
        }

        private void ToSave_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (ToSave.Count >= AutoSaveCounter)
            {
                DoSheetSave();
            }
            else
                StatusChanged?.Invoke($"Waiting new data more { AutoSaveCounter - ToSave.Count} times");
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
        bool IsConnecting { get; set; } = false;
        Sheet MySheet => DocSheets.SingleOrDefault(t => t.Properties.Title == MyUserName);
        //private List<long> Saved { get; set; } =  new List<long>();
        private List<SavedData> SavedData { get; set; } = new List<SavedData>();
        public ObservableCollection<SavedData> ToSave { get; set; } = new ObservableCollection<SavedData>();
        public bool IsConnected { get; set; } = false;
        public bool SaveInProgress { get; set; } = false;
        public int AutoSaveCounter { get => autoSaveCounter; set => SetProperty(ref autoSaveCounter, value); }
        public enum CalcMedType
        {
            FiveHundr,
            Hundr,
            Thous,
            Calculated
        }
        public Dictionary<CalcMedType, double> GetMedian(int counter)
        {
            var unt = SavedData.Concat(ToSave).Select(t => t.Crash);
            var zCNT = unt.TakeLast(counter).Median();
            var z5 = unt.TakeLast(500).Median();
            var z1 = unt.TakeLast(100).Median();
            var z10 = unt.TakeLast(1000).Median();
            Dictionary<CalcMedType, double> ret = new Dictionary<CalcMedType, double>();
            ret.Add(CalcMedType.Calculated, double.IsNaN(zCNT) ? 0d : zCNT);
            ret.Add(CalcMedType.FiveHundr, double.IsNaN(z5) ? 0d : z5);
            ret.Add(CalcMedType.Hundr, double.IsNaN(z1) ? 0d : z1);
            ret.Add(CalcMedType.Thous, double.IsNaN(z10) ? 0d : z10);
            return ret;
        }
        public async void SetConnAndRead()
        {
            if (IsConnecting)
                return;
            IsConnecting = true;
            await Task.Run(() =>
            {
                Service = AuthorizeGoogleApp();
                //get table info
                GetSheets();
                IsConnected = true;
                IsConnecting = false;
                StatusChanged?.Invoke("Sheets data readed");
            });
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
                        SavedData.Add(new SavedData() { Id = _id, Crash = _val });
                    }
                    if (SavedData.Count > MySheet.Properties.GridProperties.RowCount - 1000)
                    {
                        AddEmptyRows(10000);
                    }
                }
            }
        }
        public async void DoSheetSave()//IEnumerable<UnitedSockMess> mess)
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
                    if (ToSave.Count < 1)
                    {
                        StatusChanged?.Invoke("No new data to save");
                        return;
                    }
                    if (SaveInProgress)
                        StatusChanged?.Invoke("Save already in progress.. Waiting..");
                    while (SaveInProgress)
                    {
                        Task.Delay(100);
                    }
                    StatusChanged?.Invoke("Saving Data to google");
                    SaveInProgress = true;
                    if (!IsConnected)//(Service is null)
                        SetConnAndRead();
                    while (!IsConnected)
                    {
                        Task.Delay(100);
                    }
                    InsertLogData();
                    SavedData.AddRange(ToSave);
                    ToSave.Clear();
                    SavedData.Distinct();
                    StatusChanged?.Invoke("Saved!");
                    SaveInProgress = false;
                }
                catch (Exception)
                {
                    StatusChanged?.Invoke("Error on save!");
                }

            });
        }
        private void InsertLogData()
        {
            int skipped = 0;
            CheckSheetsExist();
            StatusChanged?.Invoke("Generate insert request");
            Request rq = new Request();
            AppendCellsRequest aprq = new AppendCellsRequest();
            aprq.SheetId = MySheet.Properties.SheetId;
            aprq.Fields = "*";//"userEnteredValue";
            aprq.Rows = new List<RowData>();
            //var aq = new List<UnitedSockMess>();
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
            var aq = new List<SavedData>(ToSave.OrderBy(t => t.Id)); //new List<UnitedSockMess>(mess.OrderBy(t => t.GameId));
            foreach (var item in aq)
            {
                if (SavedData.Any(t => t.Id == item.Id))
                {
                    skipped++;
                    continue;
                }
                RowData row = new RowData();
                row.Values = new List<CellData>
                {
                    new CellData() { UserEnteredValue = new ExtendedValue() { NumberValue = item.Id } },
                    new CellData() {
                        UserEnteredFormat = new CellFormat() { NumberFormat = new NumberFormat() { Type = "DATE_TIME", Pattern = "dd.MM.yyyy hh:mm:ss" }, },
                        UserEnteredValue = new ExtendedValue() { NumberValue = item.Dt.ToOADate() },
                    },
                    new CellData() { UserEnteredValue = new ExtendedValue() { NumberValue = item.Crash } }
                };
                row.Values.Add(new CellData() { UserEnteredValue = new ExtendedValue() { NumberValue = item.Profit } });
                aprq.Rows.Add(row);
            }
            rq.AppendCells = aprq;
            BatchUpdateSpreadsheetRequest updateRequest = new BatchUpdateSpreadsheetRequest
            {
                Requests = new List<Request>() { rq }
            };
            _ = Service.Spreadsheets.BatchUpdate(updateRequest, SpreadSheetId).Execute();
            string l = $"Saving Done!";
            if (skipped > 0)
                l += $"skipped {skipped} as dupl.";
            StatusChanged?.Invoke(l);
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
                formula = formula.Replace("};", $";{MyUserName}!A2:C" + "};");
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
                row.Values.Add(new CellData() { UserEnteredValue = new ExtendedValue() { StringValue = "Profit value" } });
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
