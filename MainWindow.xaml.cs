using System;
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
        SoundPlayer p = new SoundPlayer(Properties.Resources.alert);
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

        private void T_Elapsed(object sender, ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                p.Play();
                FlashWindow(wih.Handle, true);
            }));
        }
        private void MainWindow_DoAlertBlink(bool blink)
        {
            if (blink)
            {
                T_Elapsed(null, null);
                t.Start();
            }
            else
                t.Stop();

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
                Keyboard.ClearFocus();
        }
    }
}
