using StorageLib.CloudStorage.Api;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace StorageLib.CloudStorage.Implementation
{
    public sealed class Storage : IStorage
    {
        private readonly ICloudStorageApi _api;

        public ObservableCollection<IResource> Resources { get; private set; } = new();
        
        ///<inheritdoc/>
        public string StorageName => _api.CloudStorageName;

        private readonly TaskCompletionSource<bool> _initializatoin = new();

        public Storage(ICloudStorageApi api)
        {
            _api = api;
        }

        public async Task Initialize()
        {
            var result = await _api.GetRoot();
            if(result.Status==ResutlStatus.Failed)
            {
                return;
            }
            var resource = result.Result;
            Resources.Add(resource);
            await resource.Load();
            _initializatoin.TrySetResult(true);
        }

        public async Task<OperationResult<IResource>> Move(IResource resource, IResource folderTo)
        {
            if (resource.IsDestroyed || folderTo.IsDestroyed)
            {
                throw new InvalidOperationException("Resource was destroyed previously.");
            }
            await _initializatoin.Task;
            var movedPreviousParent = resource.Parent;
            var result = await _api.Move(resource.Id, resource.Parent?.Id, folderTo.Id);
            if (result.Status == ResutlStatus.Succeed)
            {
                var newResource = result.Result;
                folderTo.Resources.Add(newResource);
                newResource.Parent = folderTo;
                newResource.ParentId = folderTo.Id;

                movedPreviousParent?.Resources?.Remove(resource);
                resource.Dispose();
            }

            return result;
        }

        ///<inheritdoc/>
        public async Task<OperationResult<IResource>> Copy(IResource resource, IResource folderTo)
        {
            // 1. check that resources are properly
            // 2. create resource copy with storage api
            // 3. update resource's collection
            if (resource.IsDestroyed || folderTo.IsDestroyed)
            {
                throw new InvalidOperationException("Resource was destroyed previously.");
            }
            if (resource.IsFolder && !_api.IsOperationSupported(Operations.CopyFolder))
            {
                throw new ArgumentException($"Parameter {nameof(resource)} must be a single file resource.");
            }
            if (!folderTo.IsFolder)
            {
                throw new ArgumentException($"Parameter {nameof(folderTo)} must be a folder resource.");
            }
            await _initializatoin.Task;

            var result = await _api.Copy(resource.Id, folderTo.Id);
            if (result.Status == ResutlStatus.Succeed)
            {
                var newResource = result.Result;
                newResource.Parent = folderTo;
                newResource.ParentId = folderTo.Id;
                folderTo.Resources.Add(result.Result);
            }
            return result;
        }

        public async Task<OperationResult<bool>> Delete(IResource resource)
        {
            await _initializatoin.Task;

            var result = await _api.Delete(resource.Id);

            if (result.Status == ResutlStatus.Succeed)
            {
                var parent = resource.Parent;
                parent?.Resources?.Remove(resource);
                resource.Dispose();
            }
            return result;
        }

        public async Task<OperationResult<IResource>> Upload(string fileName, IResource parent, Stream stream, string contentType)
        {
            var result =  await _api.Upload(fileName, parent.Id, stream, contentType);
            if(result.Status == ResutlStatus.Succeed)
            {
                parent.Resources.Add(result.Result);
            }

            return result;
        }

        public async Task<IResource> Find(Predicate<IResource> match )
        {
            await _initializatoin.Task;
            return Find(Resources, match);
        }      
        
        public async Task<IResource> Find(Predicate<IResource> match, IResource start )
        {
            await _initializatoin.Task;
            if(match.Invoke(start))
            {
                return start;
            }
            return Find(start.Resources, match);
        }

        private IResource Find( ObservableCollection<IResource> resources, Predicate<IResource> match)
        {
            // todo optimize search, e.g. use binary search
            foreach (var nested in resources)
            {
                if (match.Invoke(nested))
                {
                    return nested;
                }
                var founded = Find(nested.Resources, match);
                if (founded != null)
                {
                    return founded;
                }
            }
            return null;
        }

        public Task Load(IResource resource)
        {
            return resource.Load();
        }


        public bool IsOperationSupported(Operations operation)
        {
            return _api.IsOperationSupported(operation);
        }
        
        public Task SignOut()
        {
            return  _api?.SignOut(); 
        }


        public void Dispose()
        {
            if (Resources != null)
            {
                foreach (var resource in Resources)
                {
                    resource.Dispose();
                }
                Resources.Clear();
            }
            
            _api?.Dispose();
        }
    }
}