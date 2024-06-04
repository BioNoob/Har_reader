using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Websocket.Client;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Reactive.Linq;

namespace Har_reader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
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
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    Is_connected = false;
                }));
                return;
            }
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                FilePath.Text = "Connected";
                Is_connected = true;
            }));
            client.Send(init_mess);
            exitEvent.WaitOne();
        }
        public MainWindow()
        {
            InitializeComponent();
            grid.ItemsSource = Answer;
            Answer.CollectionChanged += Answer_CollectionChanged;
            //client.DisconnectionHappened.Subscribe(t => { client.Stop(WebSocketCloseStatus.Empty, string.Empty); Is_connected = false; connect(Token); });
            client.ReconnectionHappened.Subscribe(info => client.Send(init_mess));
            client.MessageReceived.Where(t => !string.IsNullOrEmpty(t.Text)).Where(t => t.Text.Contains("\"type\":\"game_crash\"")).Subscribe(msg =>
            {
                _webSocketMessages wm = new _webSocketMessages();
                wm.time = DateTimeOffset.Now.ToUnixTimeSeconds();
                wm.data = msg.Text;
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    Answer.Insert(0, wm);
                    //Answer.Add(wm);

                }));
                Debug.WriteLine($"Message: {msg}");
            });
        }

        private void Answer_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Remove)
            {
                var y = sender as ObservableCollection<_webSocketMessages>;
                if (y.Count > 0)
                    csvexp.IsEnabled = true;
                else
                    csvexp.IsEnabled = false;
                Counter.Text = y.Count.ToString();
            }
            else if(e.Action == NotifyCollectionChangedAction.Reset)
            {
                Counter.Text = 0.ToString();
            }
        }

        const string _init_mess = "{\"type\":\"join\",\"data\":{\"initData\":\"\",\"browserInitData\":{\"token\":\"" + "ECHO" + "\",\"locale\":\"en\"}}}";
        string init_mess = "";
        string token;
        string Token
        {
            get => token; set
            {
                token = value;
                init_mess = _init_mess.Replace("ECHO", value);
            }
        }
        string path_;
        bool Is_connected
        {
            get => is_connected; set
            {
                is_connected = value;
                if (value)
                {
                    SelectFileBtn.IsEnabled = false;
                    DropConnectBtn.IsEnabled = true;
                    ConnectBtn.IsEnabled = false;
                }
                else
                {
                    SelectFileBtn.IsEnabled = true;
                    DropConnectBtn.IsEnabled = false;
                    ConnectBtn.IsEnabled = true;
                }
            }
        }
        ManualResetEvent exitEvent = new ManualResetEvent(false);
        WebsocketClient client = new WebsocketClient(new Uri("wss://ton-rocket-server.pfplabs.xyz/"));
        public ObservableCollection<_webSocketMessages> Answer = new ObservableCollection<_webSocketMessages>();
        private bool is_connected;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog t = new Microsoft.Win32.OpenFileDialog();
            t.RestoreDirectory = true;
            t.Filter = "Har files (*.har)|*.har|All files (*.*)|*.*";
            t.DefaultExt = "har";
            t.Multiselect = true;
            //grid.ItemsSource = null;
            if (t.ShowDialog() == true)
            {
                Answer.Clear();
                path_ = Path.GetDirectoryName(t.FileName);
                FilePath.Text = t.FileName;
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
                            q.Where(t => t.data.Contains("\"type\":\"game_crash\"")).Where(t => t.GetData.game_crash != 0).ToList().ForEach(p => Answer.Add(p));
                            //Answer.AddRange(q);
                        }
                    }
                }
                //Counter.Text = Answer.Count.ToString();
                //csvexp.IsEnabled = true;
            }

        }
        public class game_crash_mess
        {
            public double elapsed { get; set; }
            public double game_crash { get; set; }
            public bool lower => game_crash_normal < 2.0;
            public double game_crash_normal => game_crash / 100d;
        }
        public class _webSocketMessages
        {
            public string type { get; set; }
            public double time { get; set; }
            public DateTime time_normal => new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(time).ToLocalTime();
            //public string time_str => String.Format("{0:dd.MM.yyyy HH:mm:ss}", time_normal);
            public string data { get; set; }
            public game_crash_mess GetData => JsonConvert.DeserializeObject<game_crash_mess>(JObject.Parse(data).SelectToken("data").ToString());

        }
        public sealed class FooMap : ClassMap<_webSocketMessages>
        {
            public FooMap()
            {
                Map(m => m.time_normal);
                Map(m => m.GetData.game_crash_normal);
            }
        }
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
            sfd.RestoreDirectory = true;
            sfd.Filter = "Csv files (*.csv)|*.csv|All files (*.*)|*.*";
            sfd.DefaultExt = "csv";
            sfd.InitialDirectory = path_;
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
            }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (client.IsStarted)
            {
                Is_connected = false;
                FilePath.Text = "Disconnected";
                exitEvent.Set();
                client.Stop(WebSocketCloseStatus.Empty, string.Empty);
            }
                

            Environment.Exit(1);
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private async void ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(Tokentxb.Text))
            {
                string tk = Tokentxb.Text;
                Answer.Clear();
                FilePath.Text = "Connecting..";
                await Task.Run(() => connect(tk));
                //a304b3ad82e481804694dfdceb82b4784c694f0c096b28d22221d5fb63fcb53b1be5e19989037f08a40182172067d2efd163250ac8c8a9cc078b348847e50a68
            }
        }

        private void DropConnectBtn_Click(object sender, RoutedEventArgs e)
        {

            if (client.IsStarted)
            {
                exitEvent.Set();
                client.Stop(WebSocketCloseStatus.Empty, string.Empty);
                Is_connected = false;
                FilePath.Text = "Disconnected";
            }
            //client.Stop(WebSocketCloseStatus.Empty, string.Empty);

        }
    }
}
