using CustomControlLibrary;
using StorageLib;
using StorageLib.CloudStorage.Api;
using StorageLib.CloudStorage.Implementation;
using StorageLib.OneDrive;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace wpf_cloud_explorer
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
        }

        public Visibility ForgetVisibility
        {
            get { return (Visibility)GetValue(ForgetVisibilityProperty); }
            set { SetValue(ForgetVisibilityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ForgetVisibility.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ForgetVisibilityProperty =
            DependencyProperty.Register("ForgetVisibility", typeof(Visibility), typeof(MainWindow), new PropertyMetadata(Visibility.Collapsed));


        private IStorage _storage;
        public IStorage Storage
        {
            get => _storage; 
            set
            {
                if(_storage!=null)
                {
                    OnlineStorageBrowser.Storage = null;
                    _storage.Dispose();
                    _storage = null;
                    ForgetVisibility = Visibility.Collapsed;
                }

                if (value==null)
                {
                    return;
                }

                _storage = value;
                ForgetVisibility = Visibility.Visible;
                OnlineStorageBrowser.Storage = _storage;
            }
        }



        private string _lastUsedProviderId;
        private async void OnChangeProvider(object sender, RoutedEventArgs e)
        {
            var button = (sender as ToggleButton);
            var requestedProvider = button.Tag as string;
            Func<Task<IStorage>> apiInitialization = requestedProvider == "googledrive" ? 
                SetupGoogleDriveStorage : SetupOneDriveStorage;

            if (_lastUsedProviderId != requestedProvider)
            {
                Storage = null;
                GoogleDriveSignButton.IsChecked = null;
                OneDriveSignButton.IsChecked = null;
                _lastUsedProviderId = null;
                button.IsChecked = false;
            }

            GoogleDriveSignButton.IsEnabled= false;
            OneDriveSignButton.IsEnabled = false;

            if (button.IsChecked.HasValue && !button.IsChecked.Value)
            {
                Storage = await apiInitialization();
                _lastUsedProviderId = button.Tag as string;
                button.IsChecked = true;
                

                GoogleDriveSignButton.IsEnabled = true;
                OneDriveSignButton.IsEnabled = true;
                return;
            }

            if (!button.IsChecked.HasValue)
            {
                Storage = null;
                button.IsChecked = null;
                button.IsEnabled = true;
            }
            GoogleDriveSignButton.IsEnabled = true;
            OneDriveSignButton.IsEnabled = true;
        }

        private void ForgetCurrent(object sender, RoutedEventArgs e)
        {
            Storage?.SignOut();
            Storage = null;
            GoogleDriveSignButton.IsChecked = null;
            OneDriveSignButton.IsChecked = null;
            _lastUsedProviderId = null;
        }

        private async Task<IStorage> SetupOneDriveStorage()
        {
            var resourceFactory = new ResourceFactory<FolderResourceViewModel, FileResourceViewModel>();
            var api = new OneDriveApi("234cd3b6-6dd1-42da-8658-b06ae5834feb", resourceFactory);
            var storage = new Storage(api);
            await storage.Initialize();
            return storage;
        }

        private async Task<IStorage> SetupGoogleDriveStorage()
        {
            var resourceFactory = new ResourceFactory<FolderResourceViewModel, FileResourceViewModel>();
            var api = new GoogleDriveApi("credentials.json", "Cloud explorer", resourceFactory);
            var storage = new Storage(api);
            await storage.Initialize();
            return storage;
        }
    }

}
