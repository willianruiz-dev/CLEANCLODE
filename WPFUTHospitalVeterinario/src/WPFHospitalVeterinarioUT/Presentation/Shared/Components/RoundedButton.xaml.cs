using MaterialDesignThemes.Wpf;
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

namespace UI.Components
{
    /// <summary>
    /// Lógica de interacción para RoundedButton.xaml
    /// </summary>
    public partial class RoundedButton : TouchableControl
    {
        #region Dependencies

        public static readonly DependencyProperty TitleProperty =
           DependencyProperty.Register("Title", typeof(string), typeof(RoundedButton),
               new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty ColorProperty =
           DependencyProperty.Register("Color", typeof(Brush), typeof(RoundedButton),
               new PropertyMetadata(Brushes.Transparent));
        #endregion
        #region Properties
        public string Title
        {
            get { return (string)base.GetValue(TitleProperty); }
            set { base.SetValue(TitleProperty, value); }
        }
        public Brush Color
        {
            get { return (Brush)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }
        #endregion
        public RoundedButton()
        {
            InitializeComponent();
        }
    }
}
