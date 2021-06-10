using StorageLib.CloudStorage.Implementation;
using System.Threading.Tasks;

namespace StorageLib.CloudStorage.Implementation
{
    /// <summary>
    /// Represent file resource.
    /// </summary>
    public class FileResource: Resource
    {
        ///<inheritdoc/>
        public override Task Load()
        {
            // todo load file content
            IsLoaded = true;
            return Task.CompletedTask;

        }
    }
}