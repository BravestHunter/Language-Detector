using System;
using System.Collections.Generic;
using System.IO;
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
using System.Globalization;

namespace LanguageAnalizeApp
{
    public partial class MainWindow : Window
    {
        private Dictionary<string, string> LanguageCodesDict = new Dictionary<string, string>()
        {
             ["en"] = "English",
             ["fr"] = "French",
             ["ru"] = "Russian"
        };
        private int MIN_LENGTH = 3;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void GetResult()
        {
            if (InputTextBox.Text.Length >= MIN_LENGTH)
            {
                Dictionary<string, double> result = LanguageAnalizer.AnalizeLanguages(InputTextBox.Text, 2);

                ResultTextBlock.Text = "This is " + CultureInfo.GetCultureInfoByIetfLanguageTag(result.Where(x => x.Value == result.Max(y => y.Value)).First().Key).EnglishName + " language";

                double full = Math.Round(result.Values.Aggregate((x, y) => x + y));
                if (full != 0)
                {
                    FullResultTextBox.Text = "\n";
                    foreach (KeyValuePair<string, double> pair in result)
                    {
                        FullResultTextBox.Text += CultureInfo.GetCultureInfoByIetfLanguageTag(pair.Key).EnglishName + "  -  " + ((int)pair.Value / full * 100) + "%\n";
                    }
                }
                else
                {
                    ResultTextBlock.Text = "This is unknown language";
                    FullResultTextBox.Text = string.Empty;
                }
            }
            else
            {
                if (InputTextBox.Text.Length == 0)
                    ResultTextBlock.Text = string.Empty;
                else
                    ResultTextBlock.Text = "This is unknown language";

                FullResultTextBox.Text = string.Empty;
            }
        }

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            GetResult();
        }
    }
}
