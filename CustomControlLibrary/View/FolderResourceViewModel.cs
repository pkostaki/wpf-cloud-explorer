using StorageLib.CloudStorage.Api;
using StorageLib.CloudStorage.Implementation;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace CustomControlLibrary
{
    /// <summary>
    /// Represent folder resource view.
    /// </summary>
    public class FolderResourceViewModel : FolderResource, IResourceViewModel
    {
        private bool _isCutted;

        ///<inheritdoc/>
        public bool IsCutted
        {
            get => _isCutted; set { Set(ref _isCutted, value); }
        }


        private bool _isSelected;

        ///<inheritdoc/>
        public bool IsSelected
        {
            get => _isSelected; set { Set(ref _isSelected, value); }
        }

        private ObservableCollection<IResource> _folders = new();

        /// <summary>
        /// List of folders.
        /// </summary>
        public ObservableCollection<IResource> Folders
        {
            get => _folders; set => Set(ref _folders, value);
        }

        protected override async Task LoadInternal()
        {
            await base.LoadInternal();
            
            CreateFolderList();
            Resources.CollectionChanged -= Resources_CollectionChanged;
            Resources.CollectionChanged += Resources_CollectionChanged;
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

        private void CreateFolderList()
        {
            Folders.Clear();
            foreach (var r in Resources)
            {
                if (r.IsFolder)
                {
                    Folders.Add(r);
                }
            }
        }
        
        public override void Dispose()
        {
            Resources.CollectionChanged -= Resources_CollectionChanged;
            base.Dispose();
        }
    }
}
