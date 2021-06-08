using StorageLib.CloudStorage.Implementation;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace StorageLib.CloudStorage.Api
{
    public interface IStorage: IDisposable
    {
        /// <summary>
        /// List of resources.
        /// </summary>
        ObservableCollection<IResource> Resources { get; }

        /// <summary>
        /// Delete resource.
        /// </summary>
        /// <param name="resource">Resource.</param>
        /// <returns></returns>
        Task<OperationResult<bool>> Delete(IResource resource);
        /// <summary>
        /// Creates a copy of a resource. Folders cannot be copied. 
        /// </summary>
        /// <param name="resource">Resource to copy.</param>
        /// <param name="folderTo">Target resource.</param>
        /// <returns></returns>
        Task<OperationResult<IResource>> Copy(IResource resource, IResource folderTo);
        /// <summary>
        /// Move resource.
        /// </summary>
        /// <param name="resource">Resource to move.</param>
        /// <param name="folderTo">Target resource</param>
        /// <returns></returns>
        Task<OperationResult<IResource>> Move(IResource resource, IResource folderTo);

        Task Load(IResource resource);

        Task<IResource> Find(Predicate<IResource> match);

        Task<IResource> Find(Predicate<IResource> match, IResource start);

        Task<OperationResult<IResource>> Upload(string fileName, IResource parent, Stream stream, string contentType);


        bool IsOperationSupported(Operations operation);

        Task SignOut();
            
    }
}

