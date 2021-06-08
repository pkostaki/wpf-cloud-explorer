using StorageLib.CloudStorage.Api;
using StorageLib.CloudStorage.Implementation;
using System.Diagnostics;
using System.Threading.Tasks;

namespace StorageLib.CloudStorage.Implementation
{

    public class FolderResource : Resource
    {
        public FolderResource()
        {
            IsFolder = true;
        }

        private TaskCompletionSource _loading;
        private readonly object _lock = new();
        
        public override Task Load()
        {
            lock (_lock)
            {
                if (_loading != null)
                {
                    return _loading.Task;
                }
                else{
                    _loading = new TaskCompletionSource();
                }
                LoadInternal().GetAwaiter().OnCompleted(() => { _loading?.TrySetResult(); });
                return _loading.Task;
            }
        }

        private async Task LoadInternal()
        {
            if (IsLoading || IsLoaded)
            {
                return;
            }

            IsLoading = true;
            var result = await Api.GetNestedResources(Id);
            if (result.Status == ResutlStatus.Succeed)
            {
                var nestedResources = result.Result;
                foreach (var nested in nestedResources)
                {
                    nested.Parent = this;
                    nested.ParentId = this.Id;
                }
                Resources = nestedResources;
            }
            _loading.TrySetResult();
            IsLoading = false;
            IsLoaded = true;
        }
    }
}