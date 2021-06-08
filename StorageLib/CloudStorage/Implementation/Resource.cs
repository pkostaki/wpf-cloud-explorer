using StorageLib.CloudStorage.Api;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;


namespace StorageLib.CloudStorage.Implementation
{
    public abstract class Resource : IResource
    {
        public ICloudStorageApi Api { get;  set; }
        public IResource Parent { get; set; }
        public string ParentId { get;  set; }

        public bool IsFolder { get; protected set; }
        public bool IsDestroyed { get; protected set; }

        private ObservableCollection<IResource> _resources = new();
        public ObservableCollection<IResource> Resources { get => _resources; set => Set(ref _resources, value); }

        private string _id;
        public string Id { get => _id; set => Set(ref _id, value); }

        private string _name;
        public string Name
        {
            get => _name; set { Set(ref _name, value); }
        }

        private string _mimeType;
        public string MimeType
        {
            get => _mimeType; set { Set(ref _mimeType, value); }
        }


        private string _iconLink;
        public string IconLink
        {
            get => _iconLink; set { Set(ref _iconLink, value); }
        }

        private string _status = "online";
        public string Status
        {
            get => _status; set { Set(ref _status, value); }
        }

        private bool _isLoaded;
        public bool IsLoaded { get => _isLoaded; set => Set(ref _isLoaded, value); }

        private bool _isLoading;
        public bool IsLoading { get => _isLoading; set => Set(ref _isLoading, value); }

        private DateTime? _modifiedTime;
        public DateTime? ModifiedTime { get => _modifiedTime; set => Set(ref _modifiedTime, value); }

        private string _resourceType;
        public string ResourceType { get => _resourceType; set => Set(ref _resourceType, value); }

        private long? _size;
        public long? Size { get => _size; set => Set(ref _size, value); }

        public abstract Task Load();

        public event PropertyChangedEventHandler PropertyChanged;

        protected void Set<T>(ref T property, T value, [CallerMemberName] string propName = null)
        {
            property = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        protected void NotifyPropertyChanged([CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public virtual void Dispose()
        {
            IsDestroyed = true;
            Parent = null;
        }



        public static class ResourceHelper
        {
            public static bool IsAlive(IResource resource) => resource != null && !resource.IsDestroyed;
            public static bool IsAliveFolder(IResource resource) => IsAlive(resource) && resource.IsFolder;
            public static bool HasAliveParent(IResource resource) => IsAlive(resource) && resource.Parent != null && !resource.Parent.IsDestroyed;
            public static bool HasAliveParentFolder(IResource resource) => HasAliveParent(resource) && resource.Parent.IsFolder;
        }
    }

}