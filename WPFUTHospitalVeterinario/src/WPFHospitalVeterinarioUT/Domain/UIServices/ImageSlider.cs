using System.ComponentModel;
using System.IO;
using System.Windows.Threading;

namespace Domain.UIServices
{
    public class ImageSliderViewModel: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;


        private List<string> _images;
        private DispatcherTimer? _slideshowTimer;
        private int _indexImage = 0;

        private string _imgSource = string.Empty;
        public string ImgSource
        {
            get
            {
                return _imgSource;
            }
            set
            {
                _imgSource = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ImgSource)));
            }
        }

        public ImageSliderViewModel()
        {
            
            _images = Directory.GetFiles(AppConfig.Get("publishDir")).ToList();
            ImgSource = _images[0];
            // Set up the timer
            _slideshowTimer = new DispatcherTimer();
            _slideshowTimer.Tick += ChangeImage;
            _slideshowTimer.Interval = TimeSpan.FromSeconds(5);
            _slideshowTimer.Start();
        }

        

        private void ChangeImage(object? sender, EventArgs e)
        {
            if (_images.Count > 0)
            {
                _indexImage = (_indexImage + 1) % _images.Count;
                ImgSource = _images[_indexImage];
                
            }
        }

        public void Stop()
        {
            if (_slideshowTimer == null) return;

            _slideshowTimer.Tick -= ChangeImage;
            _slideshowTimer.Stop();
            _slideshowTimer = null;
            
        }
    }
}
