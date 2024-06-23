using System.Windows.Controls;
using System.Windows.Input;

namespace Har_reader
{
    /// <summary>
    /// Логика взаимодействия для SoundControl.xaml
    /// </summary>
    public partial class SoundControl : UserControl
    {
        public SoundControl()
        {
            InitializeComponent();
        }

        private void TextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            (this.DataContext as SoundControlModel).SetPath((sender as TextBox).Tag.ToString());
        }

        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
    }
}
