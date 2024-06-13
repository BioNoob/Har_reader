using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using Websocket.Client;

namespace Har_reader
{

    public class MainModel : Proper
    {
        public MainModel()
        {
            Answer.CollectionChanged += Answer_CollectionChanged;
            object lockObj = new object();
            BindingOperations.EnableCollectionSynchronization(Answer, lockObj);
            Client = new OdysseyClient();
            //client.MessageReceived.Where(t => !string.IsNullOrEmpty(t.Text)).Where(t => t.Text.Contains("\"type\":\"game_crash\"")).Subscribe(msg =>
            //{
            //    _webSocketMessages wm = new _webSocketMessages();
            //    wm.Time = DateTimeOffset.Now.ToUnixTimeSeconds();
            //    wm.Data = msg.Text;
            //    Answer.Insert(0, wm);
            //    Debug.WriteLine($"Message: {msg}");
            //});
        }

        private void Answer_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Remove)
            {
                var y = sender as ObservableCollection<_webSocketMessages>;
                //if (y.Count > 0)
                //    csvexp.IsEnabled = true;
                //else
                //    csvexp.IsEnabled = false;
                Counter = y.Where(t => t.Type == "").Count();
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    bool lower = (e.NewItems[0] as _webSocketMessages).GetCrashData.Lower;
                    Counter_lowers = lower ? Counter_lowers + 1 : Counter_lowers;
                    if (Counter_lowers > LowerCheckValue)
                        AlertSignalOn = true;
                    if (!lower)
                    {
                        Counter_lowers = 0;
                        AlertSignalOn = false;
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                Counter = 0;
            }
        }

        private int counter;
        private int counter_lowers;
        private ObservableCollection<_webSocketMessages> answer = new ObservableCollection<_webSocketMessages>();
        private string token;
        private bool is_connected;
        private bool alertsignalon;
        private string status;
        private string path; 
        private int lowerCheckValue;
        private OdysseyClient client;
        private OdysseyClient Client { get => client; set => client = value; }
        public bool Is_connected { get => is_connected; set => SetProperty(ref is_connected, value); }
        public string Token { get => token; set => SetProperty(ref token, value); }
        public ObservableCollection<_webSocketMessages> Answer { get => answer; set => SetProperty(ref answer, value); }
        public int Counter { get => counter; set => SetProperty(ref counter, value); }
        public int Counter_lowers { get => counter_lowers; set => SetProperty(ref counter_lowers, value); }
        public bool AlertSignalOn { get => alertsignalon; set => SetProperty(ref alertsignalon, value); }
        public string Status { get => status; set => SetProperty(ref status, value); }
        public int LowerCheckValue { get => lowerCheckValue; set => SetProperty(ref lowerCheckValue, value); }

        public delegate void AlertBlink(bool blink);
        public event AlertBlink DoAlertBlink;



        private CommandHandler _connCommand;
        public CommandHandler ConnectCommand
        {
            get
            {
                return _connCommand ??= new CommandHandler(async obj =>
                {
                    if (!string.IsNullOrWhiteSpace(Token))
                    {
                        Answer.Clear();
                        Status = "Connecting..";
                        await Task.Run(() => connect(Token));
                    }
                },
                (obj) => true
                );
            }
        }
        //private CommandHandler _selectfilecomand;
        //public CommandHandler SelectFileCommand
        //{
        //    get
        //    {
        //        return _selectfilecomand ??= new CommandHandler(obj =>
        //        {
        //            Microsoft.Win32.OpenFileDialog t = new Microsoft.Win32.OpenFileDialog();
        //            t.RestoreDirectory = true;
        //            t.Filter = "Har files (*.har)|*.har|All files (*.*)|*.*";
        //            t.DefaultExt = "har";
        //            t.Multiselect = true;
        //            if (t.ShowDialog() == true)
        //            {
        //                Answer.Clear();
        //                path = Path.GetDirectoryName(t.FileName);
        //                Status = path;
        //                foreach (var file in t.FileNames)
        //                {
        //                    string file_cont = File.ReadAllText(file);
        //                    JObject o = JObject.Parse(file_cont);
        //                    var messages = o.SelectTokens("log.entries").Select(t => t.Children()["_webSocketMessages"]).ToList();
        //                    foreach (var item in messages)
        //                    {
        //                        foreach (var token in item)
        //                        {
        //                            List<_webSocketMessages> q = JsonConvert.DeserializeObject<List<_webSocketMessages>>(token.ToString());
        //                            q.Where(t => t.Data.Contains("\"type\":\"game_crash\"")).Where(t => t.GetData.Game_crash != 0).ToList().ForEach(p => Answer.Add(p));
        //                        }
        //                    }
        //                }
        //            }
        //            //if(a)
        //            //{
        //            //    DoAlertBlink?.Invoke(false);
        //            //    a = false;
        //            //    return;
        //            //}
        //            //DoAlertBlink?.Invoke(true);
        //            //a = true;
        //        },
        //        (obj) => true
        //        );
        //    }
        //}
        private CommandHandler _exitcommand;
        public CommandHandler ExitCommand
        {
            get
            {
                return _exitcommand ??= new CommandHandler(obj =>
                {
                    //if (client.IsStarted)
                    //{
                    //    Is_connected = false;
                    //    exitEvent.Set();
                    //    client.Stop(WebSocketCloseStatus.Empty, string.Empty);
                    //    Status = "Disconnected";
                    //}
                    Client.StopClient();
                    Environment.Exit(1);
                },
                (obj) => true
                );
            }
        }
        private CommandHandler _savecsvcommand;
        public CommandHandler SaveCsvCommand
        {
            get
            {
                return _savecsvcommand ??= new CommandHandler(obj =>
                {
                    Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
                    sfd.RestoreDirectory = true;
                    sfd.Filter = "Csv files (*.csv)|*.csv|All files (*.*)|*.*";
                    sfd.DefaultExt = "csv";
                    sfd.InitialDirectory = path;
                    sfd.Title = "Save export file";
                    sfd.FileName = "EXPORT.csv";
                    if (sfd.ShowDialog() == true)
                    {
                        using (var writer = new StreamWriter(sfd.FileName))
                        using (var csv = new CsvWriter(writer,
                            new CsvConfiguration(CultureInfo.CurrentCulture) { Delimiter = ";" }))
                        {
                            csv.Context.RegisterClassMap<FooMap>();
                            csv.WriteRecords(Answer);
                        }
                        ProcessStartInfo psi = new ProcessStartInfo();
                        psi.FileName = sfd.FileName;
                        psi.UseShellExecute = true;
                        Process.Start(psi);
                        Status = "Saved";
                    }
                },
                (obj) => true
                );
            }
        }
        private CommandHandler _disconnectCommand;

        public CommandHandler DisconnectCommand
        {
            get
            {
                return _disconnectCommand ??= new CommandHandler(obj =>
                {
                    //if (client.IsStarted)
                    //{
                    //    Is_connected = false;
                    //    exitEvent.Set();
                    //    client.Stop(WebSocketCloseStatus.Empty, string.Empty);
                    //    Status = "Disconnected";
                    //}
                    Client.StopClient();
                },
                (obj) => true
                );
            }
        }

        public void connect(string token)
        {
            //https://odyssey.pfplabs.xyz/?token=8a308ac635aa0c626366a3d4b8855f0da128fbe36df18253ac058e6a39cd77ecea99751418853aa9ba4b91675ac81bfc945b429a76ef1a54fc3c25da37b03738&locale=en
            //https://github.com/Marfusios/websocket-client
            //1b3f392abbae5c40cfa12760c7fa08df1b9abfcbb02698348982918a032f0c195e5adf38ae02ba3834d0f586db596475e5eaf111929a6edbccdd93ec0bfc571a
            Token = token;
            try
            {
                //await client.Start();
                Client.Connect(token);
            }
            catch (Exception)
            {
                //Application.Current.Dispatcher.Invoke(new Action(() =>
                //{
                Is_connected = false;
                //}));
                return;
            }
            //Application.Current.Dispatcher.Invoke(new Action(() =>
            //{
            Status = "Connected";
            Is_connected = true;
            //}));
            //client.Send(init_mess);
            //exitEvent.WaitOne();
        }
    }
}
