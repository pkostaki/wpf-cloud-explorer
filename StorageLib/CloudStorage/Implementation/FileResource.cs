using StorageLib.CloudStorage.Implementation;
using System.Threading.Tasks;

namespace StorageLib.CloudStorage.Implementation
{
    public class FileResource: Resource
    {

        public override Task Load()
        {
            // todo load file content
            IsLoaded = true;
            return Task.CompletedTask;

        }
    }
}