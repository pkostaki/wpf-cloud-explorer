using StorageLib.CloudStorage.Implementation;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace StorageLib.CloudStorage.Api
{
    /// <summary>
    /// Manage cloud resources.
    /// </summary>
    public interface IStorage: IDisposable
    {
        /// <summary>
        /// True, if storage initialize succeed.
        /// </summary>
        Task<bool> Initialized { get; }
            
        /// <summary>
        /// Cloud storage name.
        /// </summary>
        string CloudStorageName { get; }

        /// <summary>
        /// List of resources.
        /// </summary>
        ObservableCollection<IResource> Resources { get; }

        /// <summary>
        /// Delete resource.
        /// </summary>
        /// <param name="resource">Resource.</param>
        /// <returns>Operation result.</returns>
        Task<OperationResult<bool>> Delete(IResource resource);

        /// <summary>
        /// Creates a copy of a resource.
        /// </summary>
        /// <param name="resource">Resource to copy.</param>
        /// <param name="target">Target resource.</param>
        /// <returns>Operation result.</returns>
        Task<OperationResult<IResource>> Copy(IResource resource, IResource target);

        /// <summary>
        /// Move resource.
        /// </summary>
        /// <param name="resource">Resource to move.</param>
        /// <param name="target">Target resource</param>
        /// <returns>Operation result.</returns>
        Task<OperationResult<IResource>> Move(IResource resource, IResource target);

        /// <summary>
        /// Load resource.
        /// </summary>
        /// <param name="resource">Resource.</param>
        /// <returns>Task</returns>
        Task Load(IResource resource);
        
        /// <summary>
        /// Find resource.
        /// </summary>
        /// <param name="match">Match predicate.</param>
        /// <returns>Result of search.</returns>
        Task<IResource> Find(Predicate<IResource> match);

        /// <summary>
        /// Find resource.
        /// </summary>
        /// <param name="match">Match predicate.</param>
        /// <param name="start">Resource from which search will started.</param>
        /// <returns></returns>
        Task<IResource> Find(Predicate<IResource> match, IResource start);

        /// <summary>
        /// Upload file.
        /// </summary>
        /// <param name="target">Target.</param>
        /// <param name="fileName">Name of file.</param>
        /// <param name="stream">Content of file.</param>
        /// <param name="contentType">Content type.</param>
        /// <returns></returns>
        Task<OperationResult<IResource>> Upload( IResource target, string fileName, Stream stream, string contentType);

        /// <summary>
        /// True if operation is supported.
        /// </summary>
        /// <param name="operation">Operation.</param>
        /// <returns>True if supported.</returns>
        bool IsOperationSupported(Operations operation);

        /// <summary>
        /// Sign out.
        /// </summary>
        /// <returns>Task</returns>
        Task SignOut();
            
    }
}

