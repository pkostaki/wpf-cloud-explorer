using Microsoft.Graph;
using Microsoft.Identity.Client;
using StorageLib.CloudStorage.Api;
using StorageLib.CloudStorage.Implementation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace StorageLib.OneDrive
{
    /// <summary>
    /// Implements OneDrive resource operations.
    /// </summary>
    public sealed class OneDriveApi : ICloudStorageApi
    {
        private readonly string[] _scopes = new string[] { "user.read", "files.readwrite", "offline_access"};

        private IPublicClientApplication _clientApp;
        private readonly TaskCompletionSource<bool> _initialization = new();
        private readonly string _clientId;
        private readonly string _tenant;
        private readonly string _azureCloudInstance;
        private readonly IResourceFactory _resourceFactory;
        private string _driveId;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="clientId">Application client id.</param>
        /// <param name="azureCloudInstance">Authority Uri parameter</param>
        /// <param name="tenant">Authority Uri parameter</param>
        /// <param name="resourceFactory">Resource creation factory.</param>
        public OneDriveApi(string clientId, string azureCloudInstance, string tenant, IResourceFactory resourceFactory)
        {
            _clientId = clientId;
            _tenant = tenant;
            _azureCloudInstance = azureCloudInstance;
            _resourceFactory = resourceFactory;

            _ = InitializeAsync();
        }

        #region Authentication

        private AuthenticationResult _authResult = null;

        private GraphServiceClient _graphServiceClient;

        private async Task InitializeAsync()
        {

            CreateApplication();

            // https://docs.microsoft.com/en-us/azure/active-directory/develop/msal-acquire-cache-tokens
            IAccount firstAccount;
            var accounts = await _clientApp.GetAccountsAsync();
            firstAccount = accounts.FirstOrDefault();
            try
            {
                _authResult = await _clientApp.AcquireTokenSilent(_scopes, firstAccount)
                    .ExecuteAsync();
            }
            catch (MsalUiRequiredException ex)
            {
                // A MsalUiRequiredException happened on AcquireTokenSilent. 
                // This indicates you need to call AcquireTokenInteractive to acquire a token
                Debug.WriteLine($"MsalUiRequiredException: {ex.Message}");
                try
                {
                    _authResult = await _clientApp.AcquireTokenInteractive(_scopes)
                        .WithAccount(firstAccount)
                        .WithPrompt(Microsoft.Identity.Client.Prompt.SelectAccount)
                        .ExecuteAsync();
                }
                catch (MsalException msalex)
                {
                    // Error Acquiring Token:{System.Environment.NewLine}{msalex};
                }
            }
            catch (Exception ex)
            {
                // Error Acquiring Token Silently:{System.Environment.NewLine}{ex}";
                _initialization.TrySetResult(false);
                return;
            }

            _graphServiceClient = new GraphServiceClient(new DelegateAuthenticationProvider((requestMessage) =>
            {
                requestMessage.Headers.Authorization
                    = new AuthenticationHeaderValue("Bearer", _authResult.AccessToken);
                return Task.CompletedTask;
            }));

            _initialization.TrySetResult(true);
        }

        private void CreateApplication()
        {
            var builder = PublicClientApplicationBuilder.Create(_clientId)
                    .WithAuthority($"{_azureCloudInstance}{_tenant}")
                    .WithDefaultRedirectUri();
            _clientApp = builder.Build();
            TokenCacheHelper.EnableSerialization(_clientApp.UserTokenCache);
        }
        #endregion  

        ///<inheritdoc/>
        public async Task<OperationResult<bool>> Delete(string id)
        {
            if(await _initialization.Task == false)
            {
                return OperationResult<bool>.FailedWithNotInitializeResult();
            }

            var result = new OperationResult<bool>();
            try
            {
                await _graphServiceClient.Me.Drive.Items[id].Request().DeleteAsync();
                result.Result = true;
                result.Status = ResutlStatus.Succeed;
            }
            catch ( Exception ex)
            {
                result.FailedWithException(ex);
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task<OperationResult<IResource>> GetRoot()
        {
            if (await _initialization.Task == false)
            {
                return OperationResult<IResource>.FailedWithNotInitializeResult();
            }

            var result = new OperationResult<IResource>();
            try
            {
                var root = await _graphServiceClient.Me.Drive.Root.Request()
                .Select("id,name,folder,file,parentReference,WebUrl,size,lastModifiedDateTime,folder")
                .Expand("thumbnails")
                .GetAsync();
                _driveId = root.ParentReference?.DriveId;
                var resource = CreateResourceInternal(root);
                result.SucceedWithResult(resource);
            }
            catch (Exception ex)
            {
                result.FailedWithException(ex);
            }

            return result;
        }

        ///<inheritdoc/>
        public async Task<OperationResult<IResource>> Get(string id)
        {
            if (await _initialization.Task == false)
            {
                return OperationResult<IResource>.FailedWithNotInitializeResult();
            }

            var result = new OperationResult<IResource>();

            try
            {
                var driveItem = await _graphServiceClient.Me.Drive.Items[id].Request()
                    .Select("id,name,size,file,parentReference,lastModifiedDateTime,folder,WebUrl")
                    .Expand("thumbnails")
                    .GetAsync();

                var resource = CreateResourceInternal(driveItem);
                result.SucceedWithResult(resource);
            }
            catch (Exception ex)
            {
                result.FailedWithException(ex);
            }

            return result;
        }

        ///<inheritdoc/>
        public async Task<OperationResult<ObservableCollection<IResource>>> GetNestedResources(string resourceId)
        {
            if (await _initialization.Task == false)
            {
                return OperationResult<ObservableCollection<IResource>>.FailedWithNotInitializeResult();
            }

            var result = new OperationResult<ObservableCollection<IResource>>();

            try
            {
                var resources = new ObservableCollection<IResource>();
                var request = _graphServiceClient.Me.Drive.Items[resourceId].Children.Request()
                    .Select("id,name,size,file,parentReference,lastModifiedDateTime,folder,WebUrl")
                    .Expand("thumbnails");
                do
                {
                    var childCollection = await request.GetAsync();
                    foreach (var driveItem in childCollection)
                    {
                        var resource = CreateResourceInternal(driveItem);
                        resources.Add(resource);
                    }
                    request = childCollection.NextPageRequest;
                } while (request != null);

                result.Result = resources;
                result.Status = ResutlStatus.Succeed;
            }
            catch (ServiceException ex)
            {
                result.FailedWithException(ex);
            }

            return result;
        }

        ///<inheritdoc/>
        public async Task<OperationResult<IResource>> Move(string id, string parentId, string targetId)
        {
            // parentId - not used for OneDriveApi
            if (await _initialization.Task == false)
            {
                return OperationResult<IResource>.FailedWithNotInitializeResult();
            }

            var result = new OperationResult<IResource>();

            try
            {
                DriveItem item = new()
                {
                    ParentReference = new ItemReference
                    {
                        Id = targetId,
                        DriveId = _driveId
                    }
                };
                var request = await _graphServiceClient.Me.Drive.Items[id].Request().UpdateAsync(item);
                if (request != null)
                {
                    var getUpdatedResult = await Get(request.Id);
                    result.CompleteWithResult(getUpdatedResult);
                }
            }
            catch (ServiceException ex)
            {
                result.FailedWithException(ex);
            }

            return result;
        }

        ///<inheritdoc/>
        public async Task<OperationResult<IResource>> Copy(string id, string targetId)
        {
            // 1. Wait initialization
            // 2. Form and execute copy request
            // 3. Wait while copying will completed
            // 4. If copying successed make request to fetch new item and return resource
            // 5. If failed return null 
            if (await _initialization.Task == false)
            {
                return OperationResult<IResource>.FailedWithNotInitializeResult();
            }

            var result = new OperationResult<IResource>();

            try
            {
                ItemReference parentRef = new()
                {
                    Id = targetId,
                    DriveId = _driveId
                };

                var request = _graphServiceClient.Me.Drive.Items[id].Copy(parentReference: parentRef).Request();
                using var message = new HttpRequestMessage(HttpMethod.Post, request.RequestUrl);
                var json = $"{{\"parentReference\":{{\"driveId\":\"{parentRef.DriveId}\",\"id\":\"{parentRef.Id}\"}}}}";
                message.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                await _graphServiceClient.AuthenticationProvider.AuthenticateRequestAsync(message);

                using HttpResponseMessage response = await _graphServiceClient.HttpProvider.SendAsync(message);
                if (!response.IsSuccessStatusCode || response.Headers.Location == null)
                {
                    await result.FailedBasedHttpResponce(response);
                    return result;
                }

                var copingCompleted = false;
                string resourceId = null;
                using var httpClient = new HttpClient();
                do
                {
                    using var statusMessage = new HttpRequestMessage(HttpMethod.Get, response.Headers.Location);
                    using var statusResponce = await httpClient.SendAsync(statusMessage);

                    if (!statusResponce.IsSuccessStatusCode)
                    {
                        await result.FailedBasedHttpResponce(statusResponce);
                        return result;
                    }

                    var content = await statusResponce.Content.ReadAsStringAsync();
                    var resultStatus = _graphServiceClient.HttpProvider.Serializer.DeserializeObject<Dictionary<string, string>>(content);
                    copingCompleted = resultStatus != null && resultStatus.ContainsKey("status") && resultStatus?["status"] == "completed";
                    if (copingCompleted)
                    {
                        resultStatus.TryGetValue("resourceId", out resourceId);
                    }
                    else
                    {
                        await Task.Delay(500);
                    }
                }
                while (!copingCompleted); // todo add max time wait interval, e.g. 60sec.

                if (string.IsNullOrEmpty(resourceId))
                {
                    result.Status = ResutlStatus.Failed;
                    result.ErrorMessage = "Copying resource is failed.";
                    return result;
                }

                var getRequest = await Get(resourceId);
                result.CompleteWithResult(getRequest);
                return result;
            }
            catch (Exception ex)
            {
                result.FailedWithException(ex);
            }

            return result;

        }

        private IResource CreateResourceInternal(DriveItem item)
        {
            return _resourceFactory.Create(
                this,
                item.Folder != null,
                item.Id,
                item.Thumbnails?.FirstOrDefault()?.Small?.Url,
                item.LastModifiedDateTime.HasValue ? item.LastModifiedDateTime.Value.DateTime : null,
                item.Name,
                item.Size,
                item.File?.MimeType,
                item.WebUrl);
        }

        private readonly Operations _supportedOperations =
                                                Operations.CopyFile |
                                                Operations.CopyFolder |
                                                Operations.DeleteFile |
                                                Operations.DeleteFolder |
                                                Operations.CutFile |
                                                Operations.CutFolder |
                                                Operations.UploadFile;

        ///<inheritdoc/>
        public string CloudStorageName => "OneDrive";

        ///<inheritdoc/>
        public bool IsOperationSupported(Operations operation)
        {
            return (operation & _supportedOperations) == operation;
        }

        ///<inheritdoc/>
        public async Task SignOut()
        {
            foreach (var user in await _clientApp.GetAccountsAsync())
            {
                await _clientApp.RemoveAsync(user);
            }
            TokenCacheHelper.ClearCacheAsync();
        }

        ///<inheritdoc/>
        public void Dispose()
        {
            _initialization.TrySetResult(false);
            _clientApp = null;
            _graphServiceClient = null;
        }

        ///<inheritdoc/>
        public async Task<OperationResult<IResource>> Upload(string fileName, string parentId, Stream stream, string contentType)
        {
            if (await _initialization.Task == false)
            {
                return OperationResult<IResource>.FailedWithNotInitializeResult();
            }

            // https://docs.microsoft.com/ru-ru/graph/sdks/large-file-upload?tabs=csharp
            var result = new OperationResult<IResource>();

            try
            {
                var uploadProps = new DriveItemUploadableProperties
                {
                    ODataType = null,
                    Name = fileName,
                    AdditionalData = new Dictionary<string, object> { { "@microsoft.graph.conflictBehavior", "rename" } }
                };
                UploadSession uploadSession = await _graphServiceClient.Me.Drive.Items[parentId]
                                                        .ItemWithPath(fileName)
                                                        .CreateUploadSession(uploadProps)
                                                        .Request()
                                                        .PostAsync();

                // Max slice size must be a multiple of 320 KiB
                int maxSliceSize = 320 * 1024;

                var fileUploadTask =
                    new LargeFileUploadTask<DriveItem>(uploadSession, stream, maxSliceSize, _graphServiceClient);

                // Upload the file
                var uploadResult = await fileUploadTask.UploadAsync();
                if (uploadResult.UploadSucceeded)
                {
                    result.Status = ResutlStatus.Succeed;
                    result.Result = CreateResourceInternal(uploadResult.ItemResponse);
                }
                else
                {
                    result.Status = ResutlStatus.Failed;
                }
            }
            catch (Exception ex)
            {
                result.FailedWithException(ex);
            }

            return result;

        }
    }
}
