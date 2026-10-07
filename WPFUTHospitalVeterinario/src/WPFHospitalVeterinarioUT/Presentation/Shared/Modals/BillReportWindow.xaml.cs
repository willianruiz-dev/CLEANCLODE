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
using System.Windows.Shapes;

namespace UI.Modals
{
    /// <summary>
    /// Lógica de interacción para BillReportWindow.xaml
    /// </summary>
    public partial class BillReportWindow : Window
    {
        private List<DictionaryData> _billData = new List<DictionaryData>();
        public BillReportWindow(Dictionary<string, string> header, Dictionary<string, string> body, Dictionary<string, string> footer)
        {
            InitializeComponent();
            void InsertData(Dictionary<string, string> toInsert)
            {
                foreach (var entry in toInsert)
                {
                    _billData.Add(new DictionaryData { Key = entry.Key, Value = entry.Value });
                }
            }
            InsertData(new Dictionary<string, string>() { { "=====================", "===========================" } });
            InsertData(header);
            InsertData(new Dictionary<string, string>() { { "=====================", "===========================" } });
            InsertData(body);
            InsertData(new Dictionary<string, string>() { { "=====================", "===========================" } });
            InsertData(footer);
            InsertData(new Dictionary<string, string>() { { "=====================", "===========================" } });


            DataView.ItemsSource = _billData;
        }

        private void BtnClose_MouseDown(object sender, EventArgs e)
        {
            var btn = (Grid)sender;
            this.DialogResult = btn.Name == "BtnYes" ? true : false;
        }
    }


    public class DictionaryData
    {
        public string Key { get; set; }

        public string Value { get; set; }

    }
}
