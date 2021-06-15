using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

namespace StorageLib.CloudStorage.Api
{
    /// <summary>
    /// Represent resource.
    /// </summary>
    public interface IResource : INotifyPropertyChanged
    {
        /// <summary>
        /// True if resource is folder.
        /// </summary>
        bool IsFolder { get; }

        /// <summary>
        /// Web link to resource.
        /// </summary>
        string WebLink { get; }

        /// <summary>
        /// Icon link.
        /// </summary>
        string IconLink { get; }

        /// <summary>
        /// Resource's id.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Resource's mime type.
        /// </summary>
        string MimeType { get; }

        /// <summary>
        /// Resource's modified time.
        /// </summary>
        DateTime? ModifiedTime { get; }

        /// <summary>
        /// Resource's name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Resource's type.
        /// </summary>
        string ResourceType { get; }

        /// <summary>
        /// Resource's size in bytes.
        /// </summary>
        long? Size { get; }

        /// <summary>
        /// Resource's status.
        /// </summary>
        string Status { get; }

        /// <summary>
        /// List of nested resources.
        /// </summary>
        ObservableCollection<IResource> Resources { get; }

        /// <summary>
        /// Parent id.
        /// </summary>
        string ParentId { get;}

        /// <summary>
        /// Parent link
        /// </summary>
        IResource Parent { get;} 

        /// <summary>
        /// True if resource was loaded.
        /// </summary>
        bool IsLoaded { get; }

        /// <summary>
        /// True, if instance was destroyed.
        /// </summary>
        bool IsDestroyed { get; }

        /// <summary>
        /// Load resource.
        /// </summary>
        /// <returns></returns>
        Task Load();

        /// <summary>
        /// Dispose
        /// </summary>
        void Dispose();
    }
}