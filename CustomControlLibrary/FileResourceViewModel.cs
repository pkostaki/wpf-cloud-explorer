using StorageLib.CloudStorage.Api;
using StorageLib.CloudStorage.Implementation;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace CustomControlLibrary
{

    public class FolderResourceViewModel : FolderResource, IResourceViewModel
    {
        private bool _isSelected;

        public override async Task Load()
        {
            if(IsLoaded|| IsLoading)
            {
                return;
            }
            await base.Load();

            CreateFolderList();
            Resources.CollectionChanged += Resources_CollectionChanged;
        }

        //public BitmapSource IconBitmap { get; set; }
        public override void Dispose()
        {
            Resources.CollectionChanged -= Resources_CollectionChanged;
            base.Dispose();
        }

        private void Resources_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                foreach (var item in e.OldItems)
                {
                    if (item is IResource resource && resource.IsFolder)
                    {
                        Folders.Remove(resource);
                    }
                }
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is IResource resource && resource.IsFolder)
                    {
                        Folders.Add(resource);
                    }

                }
            }
        }

        public bool IsSelected
        {
            get => _isSelected; set { Set(ref _isSelected, value); }
        }

        private ObservableCollection<IResource> _folders = new();

        public ObservableCollection<IResource> Folders
        {
            get => _folders;
            set => Set(ref _folders, value);
        }

        private void CreateFolderList()
        {
            foreach (var r in Resources)
            {
                if (r.IsFolder)
                {
                    Folders.Add(r);
                }
            }
        }
        
        private bool _isCutted;
        public bool IsCutted
        {
            get => _isCutted; set { Set(ref _isCutted, value); }
        }
    }


    public class FileResourceViewModel : FileResource, IResourceViewModel
    {
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected; set { Set(ref _isSelected, value); }
        }   
        
        private bool _isCutted;
        public bool IsCutted
        {
            get => _isCutted; set { Set(ref _isCutted, value); }
        }
    }

    public interface IResourceViewModel: IResource
    {
        bool IsSelected { get; set; }
        bool IsCutted { get; set; }
    }
}
