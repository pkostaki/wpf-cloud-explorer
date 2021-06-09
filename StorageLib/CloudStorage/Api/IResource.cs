using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

namespace StorageLib.CloudStorage.Api
{
    public interface IResource : INotifyPropertyChanged
    {
        bool IsFolder { get; }

        string WebLink { get; }
        string IconLink { get; }
        string Id { get; }

        bool IsLoaded { get; }
        string MimeType { get; }
        DateTime? ModifiedTime { get; }
        string Name { get; }

        string ResourceType { get; }
        long? Size { get; }
        string Status { get; }
        ObservableCollection<IResource> Resources { get; }
        string ParentId { get; set; }
        IResource Parent { get; set; }
        bool IsDestroyed { get; }
        Task Load();

        void Dispose();

    }
}