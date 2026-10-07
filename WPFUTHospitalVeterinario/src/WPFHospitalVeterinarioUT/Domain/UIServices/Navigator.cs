using System;
using System.Dynamic;
using System.Windows;
using System.Windows.Controls;
using UI;
using UI.Modals;

namespace Domain.UIServices
{
    public class Navigator
    {
        // Patron de Diseño Singleton
        private static Navigator? _instance;
        private MainWindow? _mainWindow;
        private ModalWindow? _currentModal; // Incluso se puede hacer con una Cola de modales

        private Navigator() { }

        public static Navigator Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Navigator();
                return _instance;
            }
        }

        public void Init (MainWindow mainWindow)
        {
            _mainWindow = mainWindow ;
        }

        public void NavigateTo(UserControl view)
        {

            if (_mainWindow == null) 
            {
                throw new Exception("El navegador de la aplicación no ha sido inicializado en la ventana principal");
            }

            if (_mainWindow.Dispatcher.CheckAccess())
            {
                _mainWindow.MainContainer.Content = view;
                return;
            }

            _mainWindow.Dispatcher.Invoke(() =>
            {
                _mainWindow.MainContainer.Content = view;
            });
            
        }
        
        public bool ShowModal(string msg, ModalType type )
        {
            bool result = false;

            ModalViewModel model = new ModalViewModel
            {
                Title = "Estimado cliente:",
                Message = msg,
                TypeModal = type,
            };

            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                _currentModal = new ModalWindow(model);
                _currentModal.ShowDialog();
                if (_currentModal.DialogResult.HasValue)
                {
                    result = _currentModal.DialogResult.Value;
                }
            });
            return result;
        }

        public ModalWindow? ShowLoadModal(string msg)
        {
            ModalWindow? loadWindow = null;

            ModalViewModel model = new ModalViewModel
            {
                Title = string.Empty,
                Message = msg,
                TypeModal = new LoadModal(),
            };

            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                loadWindow = new ModalWindow(model);
                loadWindow.Show();
            });

            
            return loadWindow;
        }

        public bool ShowModalOf<T> (T modal) where T : Window
        {
            bool result = false;
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                modal.ShowDialog();
                if (modal.DialogResult.HasValue)
                {
                    result = modal.DialogResult.Value;
                }
            });
            return result;
        }
        
        

        public void CloseModal() => Application.Current.Dispatcher.Invoke((Action)delegate
        {
            if (_currentModal != null)
            {
                _currentModal.Close();
                _currentModal = null;
            }
        });






    }
}
