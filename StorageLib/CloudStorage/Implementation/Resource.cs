using StorageLib.CloudStorage.Api;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;


namespace StorageLib.CloudStorage.Implementation
{
    /// <summary>
    /// Abstract class for resources
    /// </summary>
    public abstract class Resource : IResource
    {
        /// <summary>
        /// Cloud api
        /// </summary>
        public ICloudStorageApi Api { get; set; }

        ///<inheritdoc/>
        public IResource Parent { get; set; }

        ///<inheritdoc/>
        public string ParentId { get; set; }

        private string _webLink;
        ///<inheritdoc/>
        public string WebLink { get => _webLink; set => Set(ref _webLink, value); }

        ///<inheritdoc/>
        public bool IsFolder { get; protected set; }

        ///<inheritdoc/>
        public bool IsDestroyed { get; protected set; }

        private ObservableCollection<IResource> _resources = new();

        ///<inheritdoc/>
        public ObservableCollection<IResource> Resources { get => _resources; set => Set(ref _resources, value); }

        private string _id;

        ///<inheritdoc/>
        public string Id { get => _id; set => Set(ref _id, value); }

        private string _name;

        ///<inheritdoc/>
        public string Name { get => _name; set { Set(ref _name, value); } }

        private string _mimeType;

        ///<inheritdoc/>
        public string MimeType { get => _mimeType; set { Set(ref _mimeType, value); } }


        private string _iconLink;

        ///<inheritdoc/>
        public string IconLink { get => _iconLink; set { Set(ref _iconLink, value); } }

        private string _status = "online";

        ///<inheritdoc/>
        public string Status { get => _status; set { Set(ref _status, value); } }

        private bool _isLoaded;

        ///<inheritdoc/>
        public bool IsLoaded { get => _isLoaded; set => Set(ref _isLoaded, value); }

        private bool _isLoading;

        ///<inheritdoc/>
        public bool IsLoading { get => _isLoading; set => Set(ref _isLoading, value); }

        private DateTime? _modifiedTime;

        ///<inheritdoc/>
        public DateTime? ModifiedTime { get => _modifiedTime; set => Set(ref _modifiedTime, value); }

        private string _resourceType;

        ///<inheritdoc/>
        public string ResourceType { get => _resourceType; set => Set(ref _resourceType, value); }

        private long? _size;

        ///<inheritdoc/>
        public long? Size { get => _size; set => Set(ref _size, value); }

        ///<inheritdoc/>
        public abstract Task Load();

        protected void Set<T>(ref T property, T value, [CallerMemberName] string propName = null)
        {
            property = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        ///<inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        ///<inheritdoc/>
        public virtual void Dispose()
        {
            foreach (var resource in Resources)
            {
                resource.Dispose();
            }
            Resources.Clear();
            Resources = null;
            IsDestroyed = true;
            Parent = null;
        }

        /// <summary>
        /// Resource helper
        /// </summary>
        public static class ResourceHelper
        {
            public static bool IsAlive(IResource resource) => resource != null && !resource.IsDestroyed;
            public static bool IsAliveFolder(IResource resource) => IsAlive(resource) && resource.IsFolder;
            public static bool HasAliveParent(IResource resource) => IsAlive(resource) && resource.Parent != null && !resource.Parent.IsDestroyed;
            public static bool HasAliveParentFolder(IResource resource) => HasAliveParent(resource) && resource.Parent.IsFolder;
        }
    }

}