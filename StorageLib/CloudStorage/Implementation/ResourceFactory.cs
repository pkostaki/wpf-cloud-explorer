using StorageLib.CloudStorage.Api;
using StorageLib.CloudStorage.Implementation;
using System;

namespace StorageLib.CloudStorage.Implementation
{
    /// <summary>
    /// Represent resource creation factory.
    /// </summary>
    /// <typeparam name="T">Folder resource type.</typeparam>
    /// <typeparam name="K">File resource type.</typeparam>
    public class ResourceFactory<T,K> : IResourceFactory where T: Resource, new()
                                      where K: Resource, new()
    {
        ///<inheritdoc/>
        public IResource Create(ICloudStorageApi api,
            bool isFolder,
            string id,
            string iconLink,
            DateTime? modifiedTime,
            string name,
            long? size,
            string mimeType,
            string webViewLink
            )
        {
            Resource resource = isFolder ? new T() : new K();
            resource.Api = api;
            resource.Id = id;
            resource.IconLink = iconLink;
            resource.ModifiedTime = modifiedTime;
            resource.Name = name;
            resource.Size = size;
            resource.MimeType = mimeType;
            resource.WebLink = webViewLink;
            return resource;
        }
    }
}

