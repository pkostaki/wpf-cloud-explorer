using NUnit.Framework;
using StorageLib.CloudStorage.Api;
using StorageLib.CloudStorage.Implementation;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace StorageTest
{
    public class Tests
    {
        private StubCloudApi _api;
                private ResourceFactory<FolderResource, FileResource> _resourceFactory;
        [SetUp]
        public void Setup()
        {
            _api = new StubCloudApi();
            _resourceFactory = new ResourceFactory<FolderResource, FileResource>();
            
        }

        [Test]
        [TestCase (ResutlStatus.Succeed,1, "root") ]
        [TestCase (ResutlStatus.Failed,0, null) ]
        public async Task InitializationTest(ResutlStatus resutlStatus, int expectedResourceCount, string excpectedResourceName)
        {
            _api.SetExpectedResult(
                new OperationResult<IResource>()
                {
                    Status = resutlStatus,
                    Result = _resourceFactory.Create(_api, true, "root", null, null, "root", null, null, null)
                });
            var storage = new Storage(_api);
            await storage.Initialize();
            Assert.IsTrue(storage.Resources.Count == expectedResourceCount, "Unexpected situation.");
            var rootResource = storage.Resources.Count > 0 ? storage.Resources[0] : null;
            Assert.IsTrue(rootResource?.Name == excpectedResourceName, "Unexpected situation.");
            storage.Dispose();
        }

        private async Task<Storage> CreateStorageInstance()
        {
            var nestedNodes = new ObservableCollection<IResource> {
                    _resourceFactory.Create(_api, false,"subnode1", null,null,"subnode1",null,null, null),
                    _resourceFactory.Create(_api, false,"subnode2", null,null,"subnode2",null,null, null),
                    _resourceFactory.Create(_api, true,"subnode3", null,null,"subnode3",null,null, null)
                };

            _api.SetExpectedResult(
                new OperationResult<IResource>()
                {
                    Status = ResutlStatus.Succeed,
                    Result = _resourceFactory.Create(_api, true, "root", null, null, "root", null, null, null)
                });

            _api.NestendResourceOperationResult = new OperationResult<ObservableCollection<IResource>>
            {
                Status = ResutlStatus.Succeed,
                Result = nestedNodes
            };

            var storage = new Storage(_api);
            await storage.Initialize();
            return storage;
        }

        [Test]
        [TestCase(1,2, ResutlStatus.Failed)]
        [TestCase(0, 2, ResutlStatus.Succeed)]
        public async Task CopyTest(int copyIndex, int copyTargetIndex, ResutlStatus expectedStatus)
        {
            var storage = await CreateStorageInstance();

            var rootResource = storage.Resources[0];
            var source = rootResource.Resources[copyIndex];
            var target = rootResource.Resources[copyTargetIndex];


            _api.CopyResult = new OperationResult<IResource>()
            {
                Status = expectedStatus,
                Result = expectedStatus==ResutlStatus.Failed?null: _resourceFactory.Create(_api,
                    source.IsFolder,
                    source.Id,
                    null, null, "copied", null, null, null)
            };

            Assert.IsTrue(storage.Resources.Count == 1, "Unexpected situation.");
            Assert.IsTrue(rootResource.Name == "root", "Unexpected situation.");
            Assert.IsTrue(rootResource.Resources.Count == 3, "Unexpected situation.");

            Assert.IsTrue(source.Resources.Count==0, "Unexpected situation.");
            Assert.IsTrue(target.Resources.Count==0, "Unexpected situation.");

            var opresult = await storage.Copy(source, target);
            if (opresult.Status == ResutlStatus.Succeed)
            {
                var copiedItem = opresult.Result;
                Assert.IsTrue(copiedItem.Name == "copied", "Unexpected situation.");
                Assert.IsTrue(copiedItem.Parent == target, "Unexpected situation.");
                Assert.IsTrue(copiedItem.Resources.Count== 0, "Unexpected situation.");
                Assert.IsTrue(target.Resources.Contains(copiedItem), "Unexpected situation.");
                Assert.IsTrue(target.Resources.Count == 1, "Unexpected situation.");
                Assert.IsTrue(target.Parent== rootResource, "Unexpected situation.");

                Assert.IsTrue(source.Resources.Count == 0, "Unexpected situation.");
                Assert.IsTrue(source.Parent == rootResource, "Unexpected situation.");
                Assert.IsTrue(rootResource.Resources.Contains(source), "Unexpected situation.");
                Assert.IsTrue(rootResource.Resources.Contains(target), "Unexpected situation.");
                Assert.IsTrue(rootResource.Resources.Count == 3, "Unexpected situation.");
            }
            else
            {
                Assert.IsTrue(target.Resources.Count == 0, "Unexpected situation.");
                Assert.IsTrue(source.Resources.Count == 0, "Unexpected situation.");
                Assert.IsTrue(target.Parent == rootResource, "Unexpected situation.");
                Assert.IsTrue(source.Parent == rootResource, "Unexpected situation.");
                Assert.IsTrue(rootResource.Resources.Contains(source), "Unexpected situation.");
                Assert.IsTrue(rootResource.Resources.Contains(target), "Unexpected situation.");
                Assert.IsTrue(rootResource.Resources.Count == 3, "Unexpected situation.");
            }

            storage.Dispose();
        }     
        
    
        
        [Test]
        [TestCase(1,2, ResutlStatus.Failed)]
        [TestCase(0, 2, ResutlStatus.Succeed)]
        public async Task MoveTest(int moveIndex, int moveTargetIndex, ResutlStatus expectedStatus)
        {
            Storage storage = await CreateStorageInstance();

            var rootResource = storage.Resources[0];
            var source = rootResource.Resources[moveIndex];
            var target = rootResource.Resources[moveTargetIndex];

            _api.MoveResult = new OperationResult<IResource>()
            {
                Status = expectedStatus,
                Result = expectedStatus == ResutlStatus.Failed ? null : _resourceFactory.Create(_api,
                            source.IsFolder,
                            source.Id,
                            null, null, "moved", null, null,null)
            };

            Assert.IsTrue(storage.Resources.Count == 1, "Unexpected situation.");
            Assert.IsTrue(rootResource.Name == "root", "Unexpected situation.");
            Assert.IsTrue(rootResource.Resources.Count == 3, "Unexpected situation.");
            Assert.IsTrue(source.Resources.Count == 0, "Unexpected situation.");
            Assert.IsTrue(target.Resources.Count == 0, "Unexpected situation.");

            var opresult = await storage.Move(source, target);

            if (opresult.Status == ResutlStatus.Succeed)
            {
                var movedItem = opresult.Result;
                Assert.IsTrue(movedItem.Name == "moved", "Unexpected situation.");
                Assert.IsTrue(movedItem.Parent == target, "Unexpected situation.");
                Assert.IsTrue(target.Resources.Contains(movedItem), "Unexpected situation.");
                Assert.IsTrue(target.Resources.Count == 1, "Unexpected situation.");
                Assert.IsTrue(target.Parent == rootResource, "Unexpected situation.");

                Assert.IsTrue(source.IsDestroyed == true, "Unexpected situation.");
                Assert.IsFalse(rootResource.Resources.Contains(source), "Unexpected situation.");
                Assert.IsTrue(rootResource.Resources.Contains(target), "Unexpected situation.");
                Assert.IsTrue(rootResource.Resources.Count == 2, "Unexpected situation.");
            }
            else
            {
                Assert.IsTrue(target.Resources.Count == 0, "Unexpected situation.");
                Assert.IsTrue(target.Parent == rootResource, "Unexpected situation.");

                Assert.IsFalse(source.IsDestroyed == true, "Unexpected situation.");
                Assert.IsTrue(source.Resources.Count == 0, "Unexpected situation.");
                Assert.IsTrue(source.Parent == rootResource, "Unexpected situation.");
                Assert.IsTrue(rootResource.Resources.Contains(source), "Unexpected situation.");
                Assert.IsTrue(rootResource.Resources.Contains(target), "Unexpected situation.");
                Assert.IsTrue(rootResource.Resources.Count == 3, "Unexpected situation.");
            }

            storage.Dispose();
        }

        [Test]
        [TestCase(1, ResutlStatus.Failed)]
        [TestCase(1, ResutlStatus.Succeed)]
        public async Task DeleteTestAsync(int deleteIndex, ResutlStatus expectedStatus)
        {
            var storage = await CreateStorageInstance();

            var rootResource = storage.Resources[0];
            var deleted = rootResource.Resources[deleteIndex];
            
            _api.DeleteResult = new OperationResult<bool>()
            {
                Status = expectedStatus,
                Result = expectedStatus==ResutlStatus.Succeed
            };

            Assert.IsTrue(storage.Resources.Count == 1, "Unexpected situation.");
            Assert.IsTrue(rootResource.Name == "root", "Unexpected situation.");
            Assert.IsTrue(rootResource.Resources.Count == 3, "Unexpected situation.");
            Assert.IsTrue(deleted.Resources.Count == 0, "Unexpected situation.");
            Assert.IsTrue(deleted.Parent == rootResource, "Unexpected situation.");

            var opresult = await storage.Delete(deleted);
            
            if(opresult.Status == ResutlStatus.Succeed)
            {
                Assert.IsTrue(deleted.IsDestroyed, "Unexpected situation.");
                Assert.False(rootResource.Resources.Contains(deleted), "Unexpected situation.");
                Assert.IsTrue(rootResource.Resources.Count == 2, "Unexpected situation.");
                return;
            }

            Assert.IsFalse(deleted.IsDestroyed, "Unexpected situation.");
            Assert.IsTrue(deleted.Parent==rootResource, "Unexpected situation.");
            Assert.IsTrue(rootResource.Resources.Contains(deleted), "Unexpected situation.");
            Assert.IsTrue(rootResource.Resources.Count == 3, "Unexpected situation.");

            storage.Dispose();
        }
    }

    public class StubCloudApi : ICloudStorageApi
    {
        
        public StubCloudApi()
        {
            
        }

        private object _operationResult;

        public void SetExpectedResult<T>(T operationResult)
        {
            _operationResult = operationResult;
        }

        public OperationResult<IResource> CopyResult { get; set; }
        public Task<OperationResult<IResource>> Copy(string id, string targetId)
        {
            return Task.FromResult(CopyResult);
        }

        public OperationResult<bool> DeleteResult { get; set; }
        public Task<OperationResult<bool>> Delete(string id)
        {
            return Task.FromResult(DeleteResult);
        }

        public Task<OperationResult<IResource>> Get(string id)
        {
            return Task.FromResult(_operationResult as OperationResult<IResource>);
        }

        public OperationResult< ObservableCollection<IResource>> NestendResourceOperationResult{ get; set; }
        public Task<OperationResult<ObservableCollection<IResource>>> GetNestedResources(string resourceId)
        {
            return Task.FromResult(NestendResourceOperationResult);
        }

        public Task<OperationResult<IResource>> GetRoot()
        {
            return Task.FromResult(_operationResult as OperationResult<IResource>);
        }


        public OperationResult<IResource> MoveResult { get; set; }

        public string CloudStorageName => throw new System.NotImplementedException();

        public Task<OperationResult<IResource>> Move(string id, string parentId, string targetId)
        {
            return Task.FromResult(MoveResult);
        }

        public Task SignOut()
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {

        }

        public bool IsOperationSupported(Operations operation)
        {
            throw new System.NotImplementedException();
        }

        public Task<OperationResult<IResource>> Upload(string fileName, string parentId, Stream stream, string contentType)
        {
            throw new System.NotImplementedException();
        }
    }
}