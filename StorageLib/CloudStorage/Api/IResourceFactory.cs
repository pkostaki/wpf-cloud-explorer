using System;

namespace StorageLib.CloudStorage.Api
{
    public interface IResourceFactory
    {
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