using System;
using System.Diagnostics;
using System.Media;
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
        SoundPlayer p = new SoundPlayer();
        [DllImport("user32")] private static extern int FlashWindow(IntPtr hwnd, bool bInvert);
        WindowInteropHelper wih;
        Timer t;
        public MainWindow()
        {
            InitializeComponent();
            wih = new WindowInteropHelper(this);
            (this.DataContext as MainModel).DoAlertBlink += MainWindow_DoAlertBlink;
            t = new Timer(2000);
            t.AutoReset = true;
            t.Elapsed += T_Elapsed;
        }
        private bool muted = false;
        private int count_alert = 0;
        private void T_Elapsed(object sender, ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                if (!muted)
                {
                    if(count_alert != 2)
                    {
                        p.Play();
                        count_alert++;
                    }
                }
                FlashWindow(wih.Handle, true);
            }));
        }
        private void MainWindow_DoAlertBlink(bool blink, SoundPlayer sp)
        {
            p = sp;
            if (blink)
            {
                T_Elapsed(null, null);
                muted = false;
                t.Start();
            }
            else
            {
                t.Stop();
                count_alert = 0;
            }

        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
                //Keyboard.ClearFocus();
                Satas.Focus();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (muted)
                muted = false;
            else
            {
                p.Stop();
                muted = true;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true});
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
    }
}
