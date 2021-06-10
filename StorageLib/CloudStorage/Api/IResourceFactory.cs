using System;

namespace StorageLib.CloudStorage.Api
{
    /// <summary>
    /// Resource creation factory
    /// </summary>
    public interface IResourceFactory
    {
        /// <summary>
        /// Create instance of resource.
        /// </summary>
        /// <param name="api">Cloud api instance.</param>
        /// <param name="isFolder">True, if resource is folder.</param>
        /// <param name="id">Id.</param>
        /// <param name="iconLink">Icon link.</param>
        /// <param name="modifiedTime">Modified time.</param>
        /// <param name="name">Name.</param>
        /// <param name="size">Size in bytes.</param>
        /// <param name="mimeType">Mime type.</param>
        /// <param name="webLink">Resource web link.</param>
        /// <returns>Instance of resource.</returns>
        IResource Create(ICloudStorageApi api,
            bool isFolder,
            string id,
            string iconLink,
            DateTime? modifiedTime,
            string name,
            long? size,
            string mimeType,
            string webLink);
    }
}