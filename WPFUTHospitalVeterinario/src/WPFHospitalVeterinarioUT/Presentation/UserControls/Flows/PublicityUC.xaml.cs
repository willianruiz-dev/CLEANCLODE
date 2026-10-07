using Presentation.UserControls.Bases;
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
using VirtualKeyboard.Wpf;

namespace Presentation.UserControls.Flows
{
    /// <summary>
    /// Interaction logic for PublicityUC.xaml
    /// </summary>
    public partial class PublicityUC : AppUserControl
    {
        private bool auxChangeUrl = true;
        public PublicityUC()
        {
            InitializeComponent();
        }
        private void mediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            Thread.Sleep(300);

            if (auxChangeUrl)
            {

                MediaElement mediaElement = (MediaElement)sender;
                mediaElement.Source = new Uri("Assets/Videos/Cirugia_1920x1080.mp4", UriKind.Relative); // Establece la fuente del video
            }
            else
            {
                MediaElement mediaElement = (MediaElement)sender;
                mediaElement.Source = new Uri("Assets/Videos/ConsultaEspecializada_1920x1080.mp4", UriKind.Relative); // Establece la fuente del video
            }



            mediaElement.Volume = 0.15;
            mediaElement.Position = TimeSpan.Zero;
            auxChangeUrl = !auxChangeUrl;



        }

        private void LayoutRoot_MouseDown(object sender, MouseButtonEventArgs e)
        {
            GC.Collect();
            Dispatcher.Invoke(() => GoTo(new ConfigUC()));
        }

    }
}
