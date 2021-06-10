using StorageLib.CloudStorage.Api;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace StorageLib.CloudStorage.Implementation
{
    ///<inheritdoc/>
    public sealed class Storage : IStorage
    {
        private readonly ICloudStorageApi _api;

        public ObservableCollection<IResource> Resources { get; private set; } = new();
        
        ///<inheritdoc/>
        public string CloudStorageName => _api.CloudStorageName;

        private readonly TaskCompletionSource<bool> _initializatoin = new();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="api">Cloud api <see cref="ICloudStorageApi"/></param>
        public Storage(ICloudStorageApi api)
        {
            _api = api;
        }

        ///<inheritdoc/>
        public Task<bool> Initialized { get => _initializatoin.Task; }

        /// <summary>
        /// Initialize storage.
        /// </summary>
        /// <returns>Task</returns>
        public async Task Initialize()
        {
            var result = await _api.GetRoot();
            if (result.Status == ResutlStatus.Failed) 
            {
                _initializatoin.TrySetResult(false);
                return;
            }
            
            var resource = result.Result;
            Resources.Add(resource);
            await resource.Load();
            _initializatoin.TrySetResult(true);
        }

        ///<inheritdoc/>
        public async Task<OperationResult<IResource>> Move(IResource resource, IResource target)
        {
            if(await _initializatoin.Task == false)
            {
                return OperationResult<IResource>.FailedWithNotInitializeResult();
            }
            if (resource.IsDestroyed || target.IsDestroyed)
            {
                throw new ArgumentException("Resource was destroyed previously.");
            }
            if (!IsOperationSupported(resource.IsFolder ? Operations.CutFolder : Operations.CutFile))
            {
                throw new ArgumentException("Unsupported operation on resource.");
            }

            var previousParent = resource.Parent;
            var result = await _api.Move(resource.Id, resource.Parent?.Id, target.Id);
            if (result.Status == ResutlStatus.Succeed)
            {
                var newResource = result.Result;
                target.Resources.Add(newResource);
                newResource.Parent = target;
                newResource.ParentId = target.Id;

                previousParent?.Resources?.Remove(resource);
                resource.Dispose();
            }

            return result;
        }

        ///<inheritdoc/>
        public async Task<OperationResult<IResource>> Copy(IResource resource, IResource target)
        {
            if (await _initializatoin.Task == false)
            {
                return OperationResult<IResource>.FailedWithNotInitializeResult();
            }
            if (resource.IsDestroyed || target.IsDestroyed)
            {
                throw new ArgumentException("Resource was destroyed previously.");
            }
            if (!IsOperationSupported(resource.IsFolder ? Operations.CopyFolder : Operations.CopyFile))
            {
                throw new ArgumentException("Unsupported operation on resource.");
            }
          
            var result = await _api.Copy(resource.Id, target.Id);
            if (result.Status == ResutlStatus.Succeed)
            {
                var newResource = result.Result;
                newResource.Parent = target;
                newResource.ParentId = target.Id;
                target.Resources.Add(newResource);
            }
            return result;
        }

        ///<inheritdoc/>
        public async Task<OperationResult<bool>> Delete(IResource resource)
        {
            if (await _initializatoin.Task == false)
            {
                return OperationResult<bool>.FailedWithNotInitializeResult();
            }

            if (resource.IsDestroyed)
            {
                throw new ArgumentException("Resource was destroyed previously.");
            }

            if (!IsOperationSupported(resource.IsFolder ? Operations.DeleteFolder : Operations.DeleteFile))
            {
                throw new ArgumentException("Unsupported operation on resource.");
            }

            var result = await _api.Delete(resource.Id);
            if (result.Status == ResutlStatus.Succeed)
            {
                var parent = resource.Parent;
                parent?.Resources?.Remove(resource);
                resource.Dispose();
            }
            return result;
        }
        
        ///<inheritdoc/>
        public async Task<OperationResult<IResource>> Upload( IResource target, string fileName, Stream stream, string contentType)
        {
            if (await _initializatoin.Task == false)
            {
                return OperationResult<IResource>.FailedWithNotInitializeResult();
            }

            if (target.IsDestroyed)
            {
                throw new ArgumentException("Resource was destroyed previously.");
            }

            if (!IsOperationSupported(Operations.UploadFile))
            {
                throw new ArgumentException("Unsupported operation.");
            }

            var result =  await _api.Upload(fileName, target.Id, stream, contentType);
            if(result.Status == ResutlStatus.Succeed)
            {
                var newResource = result.Result;
                newResource.Parent = target;
                newResource.ParentId = target.Id;
                target.Resources.Add(result.Result);
            }

            return result;
        }

        ///<inheritdoc/>
        public async Task<IResource> Find(Predicate<IResource> match )
        {
            if (await _initializatoin.Task == false)
            {
                return null;
            }

            return Find(Resources, match);
        }   
        
        ///<inheritdoc/>
        public async Task<IResource> Find(Predicate<IResource> match, IResource start )
        {
            if (await _initializatoin.Task == false)
            {
                return null;
            }

            if (start.IsDestroyed)
            {
                throw new ArgumentException("Resource was destroyed previously.");
            }

            if (match.Invoke(start))
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

        ///<inheritdoc/>
        public Task Load(IResource resource)
        {
            return resource.Load();
        }

        ///<inheritdoc/>
        public bool IsOperationSupported(Operations operation)
        {
            return _api.IsOperationSupported(operation);
        }

        ///<inheritdoc/>
        public Task SignOut()
        {
            return  _api.SignOut(); 
        }

        ///<inheritdoc/>
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
            Resources = null;
            _api?.Dispose();
        }
    }
}