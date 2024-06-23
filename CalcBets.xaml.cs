using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace Har_reader
{
    /// <summary>
    /// Логика взаимодействия для CalcBets.xaml
    /// </summary>
    public partial class CalcBets : UserControl
    {
        public CalcBets()
        {
            InitializeComponent();
        }
        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.Any(x => Char.IsDigit(x) || '.'.Equals(x));
        }
        private void TextBox_PreviewTextInputInt(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.Any(x => Char.IsDigit(x));
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var x = sender as TextBox;
            if (string.IsNullOrWhiteSpace(x.Text))
            {
                x.Text = "0";
            }
        }
    }
}
