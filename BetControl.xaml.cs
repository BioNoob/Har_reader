using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;

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
