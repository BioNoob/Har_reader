using CsvHelper.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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
            client.ReconnectionHappened.Subscribe(info => client.Send(init_mess));
            client.MessageReceived.Where(t => !string.IsNullOrEmpty(t.Text)).Where(t => t.Text.Contains("\"type\":\"game_crash\"")).Subscribe(msg =>
            {
                _webSocketMessages wm = new _webSocketMessages();
                wm.Time = DateTimeOffset.Now.ToUnixTimeSeconds();
                wm.Data = msg.Text;
                //Application.Current.Dispatcher.Invoke(new Action(() =>
                //{
                Answer.Insert(0, wm);
                //Answer.Add(wm);

                //}));
                Debug.WriteLine($"Message: {msg}");
            });
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
                Counter = y.Count;
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    bool lower = (e.NewItems[0] as _webSocketMessages).GetData.Lower;
                    Counter_lowers = lower ? Counter_lowers + 1 : Counter_lowers;
                    if (Counter_lowers > 3)
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
        private const string _init_mess = "{\"type\":\"join\",\"data\":{\"initData\":\"\",\"browserInitData\":{\"token\":\"" + "ECHO" + "\",\"locale\":\"en\"}}}";
        private string init_mess = "";
        private bool is_connected;
        private bool alertsignalon;
        private string status;
        private string path;
        public bool Is_connected { get => is_connected; set => SetProperty(ref is_connected, value); }
        public string Token { get => token; set { SetProperty(ref token, value); init_mess = _init_mess.Replace("ECHO", value); } }
        public ObservableCollection<_webSocketMessages> Answer { get => answer; set => SetProperty(ref answer, value); }
        public int Counter { get => counter; set => SetProperty(ref counter, value); }
        public int Counter_lowers { get => counter_lowers; set => SetProperty(ref counter_lowers, value); }
        public bool AlertSignalOn { get => alertsignalon; set => SetProperty(ref alertsignalon, value); }
        public string Status { get => status; set => SetProperty(ref status, value); }

        public delegate void AlertBlink(bool blink);
        public event AlertBlink DoAlertBlink;

        ManualResetEvent exitEvent = new ManualResetEvent(false);
        WebsocketClient client = new WebsocketClient(new Uri("wss://ton-rocket-server.pfplabs.xyz/"));

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
                        //a304b3ad82e481804694dfdceb82b4784c694f0c096b28d22221d5fb63fcb53b1be5e19989037f08a40182172067d2efd163250ac8c8a9cc078b348847e50a68
                    }
                },
                (obj) => true
                );
            }
        }
        private CommandHandler _selectfilecomand;
        public CommandHandler SelectFileCommand
        {
            get
            {
                return _selectfilecomand ??= new CommandHandler(obj =>
                {
                    Microsoft.Win32.OpenFileDialog t = new Microsoft.Win32.OpenFileDialog();
                    t.RestoreDirectory = true;
                    t.Filter = "Har files (*.har)|*.har|All files (*.*)|*.*";
                    t.DefaultExt = "har";
                    t.Multiselect = true;
                    if (t.ShowDialog() == true)
                    {
                        Answer.Clear();
                        path = Path.GetDirectoryName(t.FileName);
                        Status = path;
                        foreach (var file in t.FileNames)
                        {
                            string file_cont = File.ReadAllText(file);
                            JObject o = JObject.Parse(file_cont);
                            var messages = o.SelectTokens("log.entries").Select(t => t.Children()["_webSocketMessages"]).ToList();
                            foreach (var item in messages)
                            {
                                foreach (var token in item)
                                {
                                    List<_webSocketMessages> q = JsonConvert.DeserializeObject<List<_webSocketMessages>>(token.ToString());
                                    q.Where(t => t.Data.Contains("\"type\":\"game_crash\"")).Where(t => t.GetData.Game_crash != 0).ToList().ForEach(p => Answer.Add(p));
                                }
                            }
                        }
                    }
                    //if(a)
                    //{
                    //    DoAlertBlink?.Invoke(false);
                    //    a = false;
                    //    return;
                    //}
                    //DoAlertBlink?.Invoke(true);
                    //a = true;
                },
                (obj) => true
                );
            }
        }

        public async void connect(string token)
        {
            //https://odyssey.pfplabs.xyz/?token=a304b3ad82e481804694dfdceb82b4784c694f0c096b28d22221d5fb63fcb53b1be5e19989037f08a40182172067d2efd163250ac8c8a9cc078b348847e50a68&locale=en
            //https://github.com/Marfusios/websocket-client
            Token = token;
            try
            {
                await client.Start();
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
            client.Send(init_mess);
            exitEvent.WaitOne();
        }
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
