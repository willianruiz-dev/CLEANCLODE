using Microsoft.Web.WebView2.Core;
using Presentation.UserControls.Flows;
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
using UI.Bases;

namespace Presentation.UserControls.Bases
{
    /// <summary>
    /// Interaction logic for TreatmentPolicy.xaml
    /// </summary>
    public partial class TreatmentPolicy : AppUserControl
    {
        public TreatmentPolicy()
        {
            InitializeComponent();

            this.Loaded += loadweb;
        }
        private void BtnSalir_MouseDown(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() => GoTo(new PublicityUC()));
        }

        private void BtnAtras_MouseDown(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() => GoTo(new FormUC()));
        }

        public  void loadweb(object sender, RoutedEventArgs e)
        {
            InitializeWebView();

          


        }

        public void InitializeWebView()
        {
            webView.Source = new Uri("http://www.scielo.org.co/pdf/rfdcp/v53n138/0120-3886-rfdcp-53-138-1d.pdf");
            //webView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompletedAsync;
            webView.EnsureCoreWebView2Async();


        }


    }
}
