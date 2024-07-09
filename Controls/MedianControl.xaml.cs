using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Har_reader.Controls
{
    /// <summary>
    /// Логика взаимодействия для MedianControl.xaml
    /// </summary>
    public partial class MedianControl : UserControl
    {
        public MedianControl()
        {
            InitializeComponent();
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
                e.Handled = !e.Text.Any(x => Char.IsDigit(x));
        }
    }
}
