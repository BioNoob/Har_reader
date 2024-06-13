using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

namespace Har_reader
{
    /// <summary>
    /// Логика взаимодействия для BetControl.xaml
    /// </summary>
    public partial class BetControl : UserControl
    {
        public BetControl()
        {
            InitializeComponent();
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.Any(x => Char.IsDigit(x) || '.'.Equals(x));
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var x = sender as TextBox;
            if (string.IsNullOrWhiteSpace(x.Text))
            {
                x.Text = "0";
            }
            //else if(x.Text.EndsWith('.'))
            //{
            //    x.Text = "0.0";
            //}
        }
    }
}
