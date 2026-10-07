using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Domain.Variables;

namespace UI.Components
{
    /// <summary>
    /// Lógica de interacción para Teclado.xaml
    /// </summary>
    public partial class GifViewer : UserControl
    {
        private string[] _imagePaths;
        private int _currentIndex = 0;
        private DispatcherTimer _timerImages;

        public string PathFolder
        {
            get { return (string)GetValue(PathFolderProperty); }
            set { SetValue(PathFolderProperty, value); }
        }

        public static readonly DependencyProperty PathFolderProperty =
            DependencyProperty.Register("PathFolder", typeof(string), typeof(GifViewer), new PropertyMetadata(string.Empty));

        public GifViewer()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }

        public void OnLoaded(object sender, RoutedEventArgs e)
        {

            LoadImages(PathFolder);
            SetupTimer(TimeSpan.FromMilliseconds(30)); // Set the frame rate (100 ms per frame)
        }

        private void LoadImages(string folderPath)
        {
            string rootPath = AppInfo.APP_DIR;
            string tempPath = Path.Combine(rootPath, folderPath.Replace("/", "\\"));
            _imagePaths = Directory.GetFiles(tempPath, "*.png");
        }

        private void SetupTimer(TimeSpan interval)
        {
            _timerImages = new DispatcherTimer(DispatcherPriority.Render);
            _timerImages.Interval = interval;
            _timerImages.Tick += Timer_Tick;
            _timerImages.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_imagePaths.Length == 0) return;

            _currentIndex = (_currentIndex + 1) % _imagePaths.Length;
            var currentImagePath = _imagePaths[_currentIndex];

            // Convert relative path to absolute path
            string absolutePath = System.IO.Path.GetFullPath(currentImagePath);
            FrameImage.Source = new BitmapImage(new Uri(absolutePath, UriKind.Absolute));
        }
    }
}
