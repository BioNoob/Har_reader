using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Websocket.Client;

namespace Har_reader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public async void test(string token)
        {
            var url = new Uri("wss://ton-rocket-server.pfplabs.xyz/");
            //https://github.com/Marfusios/websocket-client
            var exitEvent = new ManualResetEvent(false);

            using (var client = new WebsocketClient(url))
            {
                client.MessageReceived.Subscribe(msg => Debug.WriteLine($"Message: {msg}"));
                await client.Start();

                client.Send("{\"type\":\"join\",\"data\":{\"initData\":\"\",\"browserInitData\":{\"token\":\"" + token +"\",\"locale\":\"en\"}}}");
                //exitEvent.WaitOne();
            }
        }
        public MainWindow()
        {
            InitializeComponent();
        }
        string path_;

        public List<_webSocketMessages> Answer = new List<_webSocketMessages>();
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog t = new Microsoft.Win32.OpenFileDialog();
            t.RestoreDirectory = true;
            t.Filter = "Har files (*.har)|*.har|All files (*.*)|*.*";
            t.DefaultExt = "har";
            t.Multiselect = true;
            grid.ItemsSource = null;
            if (t.ShowDialog() == true)
            {
                Answer.Clear();
                path_ = Path.GetDirectoryName(t.FileName);
                FilePath.Text = path_;
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
                            q = q.Where(t => t.data.Contains("\"type\":\"game_crash\"")).Where(t => t.GetData.game_crash != 0).ToList();
                            Answer.AddRange(q);
                        }
                    }
                }
                grid.ItemsSource = Answer;
                Counter.Text = Answer.Count.ToString();
                csvexp.IsEnabled = true;
            }

        }
        public class game_crash_mess
        {
            public double elapsed { get; set; }
            public double game_crash { get; set; }
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
            using (var writer = new StreamWriter($"{path_}\\EXPORT.csv"))
            using (var csv = new CsvWriter(writer,
                new CsvConfiguration(CultureInfo.CurrentCulture) { Delimiter = ";" }))
            {
                csv.Context.RegisterClassMap<FooMap>();
                csv.WriteRecords(Answer);
            }
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = $"{path_}\\EXPORT.csv";
            psi.UseShellExecute = true;
            Process.Start(psi);
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(1);
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            if(!string.IsNullOrWhiteSpace(Token.Text))
            {
                test("Token.Text");
            }
        }
    }
}
