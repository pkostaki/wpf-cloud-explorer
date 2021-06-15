using StorageLib.CloudStorage.Implementation;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace StorageLib.CloudStorage.Api
{
    /// <summary>
    /// Cloud storage api.
    /// </summary>
    public interface ICloudStorageApi: IDisposable
    {
        /// <summary>
        /// Name of storage.
        /// </summary>
        string CloudStorageName { get; }

        /// <summary>
        /// Get root resource.
        /// </summary>
        /// <returns>Operation result.</returns>
        Task<OperationResult<IResource>> GetRoot();

        /// <summary>
        /// Get resource.
        /// </summary>
        /// <param name="id">Resource id.</param>
        /// <returns>Operation result.</returns>
        Task<OperationResult<IResource>> Get(string id);

        /// <summary>
        /// Delete resource.
        /// </summary>
        /// <param name="id">Resource's id.</param>
        /// <returns>Operation result.</returns>
        Task<OperationResult<bool>> Delete(string id);

        /// <summary>
        /// Get list of nested resources.
        /// </summary>
        /// <param name="resourceId">Resource id.</param>
        /// <returns>Operation result.</returns>
        Task<OperationResult<ObservableCollection<IResource>>> GetNestedResources(string resourceId);

        /// <summary>
        /// Copy resource.
        /// </summary>
        /// <param name="id">Id of copyied resource.</param>
        /// <param name="targetId">Destination resource id.</param>
        /// <returns>Operation result with new instance of copied resource.</returns>
        Task<OperationResult<IResource>> Copy(string id, string targetId);

        /// <summary>
        /// True if current storage suppors an <paramref name="operation"/>.
        /// </summary>
        /// <param name="operation">Operation.</param>
        /// <returns></returns>
        bool IsOperationSupported(Operations operation);

        /// <summary>
        /// Move resource.
        /// </summary>
        /// <param name="id">Resource's id.</param>
        /// <param name="parentId">Resource parent's id.</param>
        /// <param name="targetId">Target resource's id.</param>
        /// <returns>Operation result with new instance of moved resource.</returns>
        Task<OperationResult<IResource>> Move(string id, string parentId, string targetId);

        /// <summary>
        /// Upload file.
        /// </summary>
        /// <param name="fileName">Name of file.</param>
        /// <param name="parentId">Parent resource id.</param>
        /// <param name="stream">Content stream.</param>
        /// <param name="contentType">Content type.</param>
        /// <returns>Operation result.</returns>
        Task<OperationResult<IResource>> Upload(string fileName, string parentId, Stream stream, string contentType);

        /// <summary>
        /// Sign out current session.
        /// </summary>
        /// <returns>Task.</returns>
        Task SignOut();
    }
}

