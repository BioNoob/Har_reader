using System;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Har_reader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("user32")] public static extern int FlashWindow(IntPtr hwnd, bool bInvert);
        WindowInteropHelper wih;
        Timer t;
        public MainWindow()
        {
            InitializeComponent();
            wih = new WindowInteropHelper(this);
            (this.DataContext as MainModel).DoAlertBlink += MainWindow_DoAlertBlink;
            t = new Timer(1000);
            t.AutoReset = true;
            t.Elapsed += T_Elapsed;
        }

        private void T_Elapsed(object sender, ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                FlashWindow(wih.Handle, true);
            }));
        }

        
        private void MainWindow_DoAlertBlink(bool blink)
        {
            if (blink)
            {
                t.Enabled = true;
                t.Start();
            }                
            else
                t.Stop();
            
        }

        string path_;
        //bool Is_connected
        //{
        //    get => is_connected; set
        //    {
        //        is_connected = value;
        //        if (value)
        //        {
        //            SelectFileBtn.IsEnabled = false;
        //            DropConnectBtn.IsEnabled = true;
        //            ConnectBtn.IsEnabled = false;
        //        }
        //        else
        //        {
        //            SelectFileBtn.IsEnabled = true;
        //            DropConnectBtn.IsEnabled = false;
        //            ConnectBtn.IsEnabled = true;
        //        }
        //    }
        //}

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //Microsoft.Win32.OpenFileDialog t = new Microsoft.Win32.OpenFileDialog();
            //t.RestoreDirectory = true;
            //t.Filter = "Har files (*.har)|*.har|All files (*.*)|*.*";
            //t.DefaultExt = "har";
            //t.Multiselect = true;
            ////grid.ItemsSource = null;
            //if (t.ShowDialog() == true)
            //{
            //    Answer.Clear();
            //    path_ = Path.GetDirectoryName(t.FileName);
            //    FilePath.Text = t.FileName;
            //    foreach (var file in t.FileNames)
            //    {
            //        string file_cont = File.ReadAllText(file);
            //        JObject o = JObject.Parse(file_cont);
            //        var messages = o.SelectTokens("log.entries").Select(t => t.Children()["_webSocketMessages"]).ToList();
            //        foreach (var item in messages)
            //        {
            //            foreach (var token in item)
            //            {
            //                List<_webSocketMessages> q = JsonConvert.DeserializeObject<List<_webSocketMessages>>(token.ToString());
            //                q.Where(t => t.data.Contains("\"type\":\"game_crash\"")).Where(t => t.GetData.game_crash != 0).ToList().ForEach(p => Answer.Add(p));
            //                //Answer.AddRange(q);
            //            }
            //        }
            //    }
            //    //Counter.Text = Answer.Count.ToString();
            //    //csvexp.IsEnabled = true;
            //}

        }
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            //Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
            //sfd.RestoreDirectory = true;
            //sfd.Filter = "Csv files (*.csv)|*.csv|All files (*.*)|*.*";
            //sfd.DefaultExt = "csv";
            //sfd.InitialDirectory = path_;
            //sfd.Title = "Save export file";
            //sfd.FileName = "EXPORT.csv";
            //if (sfd.ShowDialog() == true)
            //{
            //    using (var writer = new StreamWriter(sfd.FileName))
            //    using (var csv = new CsvWriter(writer,
            //        new CsvConfiguration(CultureInfo.CurrentCulture) { Delimiter = ";" }))
            //    {
            //        csv.Context.RegisterClassMap<FooMap>();
            //        csv.WriteRecords(Answer);
            //    }
            //    ProcessStartInfo psi = new ProcessStartInfo();
            //    psi.FileName = sfd.FileName;
            //    psi.UseShellExecute = true;
            //    Process.Start(psi);
            //}
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            //if (client.IsStarted)
            //{
            //    Is_connected = false;
            //    FilePath.Text = "Disconnected";
            //    exitEvent.Set();
            //    client.Stop(WebSocketCloseStatus.Empty, string.Empty);
            //}
                

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
            //if (!string.IsNullOrWhiteSpace(Tokentxb.Text))
            //{
            //    string tk = Tokentxb.Text;
            //    Answer.Clear();
            //    FilePath.Text = "Connecting..";
            //    await Task.Run(() => connect(tk));
            //    //a304b3ad82e481804694dfdceb82b4784c694f0c096b28d22221d5fb63fcb53b1be5e19989037f08a40182172067d2efd163250ac8c8a9cc078b348847e50a68
            //}
        }

        private void DropConnectBtn_Click(object sender, RoutedEventArgs e)
        {

            //if (client.IsStarted)
            //{
            //    exitEvent.Set();
            //    client.Stop(WebSocketCloseStatus.Empty, string.Empty);
            //    Is_connected = false;
            //    FilePath.Text = "Disconnected";
            //}
            //client.Stop(WebSocketCloseStatus.Empty, string.Empty);

        }
    }
}
