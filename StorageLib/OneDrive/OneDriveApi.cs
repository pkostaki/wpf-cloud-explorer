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
    public sealed class OneDriveApi : ICloudStorageApi
    {
        private readonly string[] _scopes = new string[] { "user.read", "files.readwrite", "offline_access"};

        private IPublicClientApplication _clientApp;
        private readonly TaskCompletionSource _initialization = new();
        private readonly string _clientId;
        private readonly IResourceFactory _resourceFactory;
        private string _driveId;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="clientId">Application client id.</param>
        /// <param name="resourceFactory">Resource factory.</param>
        public OneDriveApi(string clientId, IResourceFactory resourceFactory)
        {
            _clientId = clientId;
            _resourceFactory = resourceFactory;

            InitializeAsync();
        }

        #region Authentication

        private AuthenticationResult _authResult = null;

        private GraphServiceClient _graphServiceClient;

        private void CreateApplication()
        {
            const string tenant = "common";
            const string azureCloudInstance = "https://login.microsoftonline.com/";

            var builder = PublicClientApplicationBuilder.Create(_clientId)
                    .WithAuthority($"{azureCloudInstance}{tenant}")
                    .WithDefaultRedirectUri();
            _clientApp = builder.Build();
            TokenCacheHelper.EnableSerialization(_clientApp.UserTokenCache);
        }

        private Task InitializeAsync()
        {
            return Task.Run(async () =>
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
                    return;
                }

                _graphServiceClient = new GraphServiceClient(new DelegateAuthenticationProvider((requestMessage) =>
                {
                    requestMessage
                        .Headers
                        .Authorization = new AuthenticationHeaderValue("Bearer", _authResult.AccessToken);
                    return Task.CompletedTask;
                }));

                _initialization.TrySetResult();
            });
        }

        #endregion  

        public Task<OperationResult< bool>> Delete(string id)
        {
            return Task.Run(async ()=> {
                await _initialization.Task;
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
            });
        }

        /// <inheritdoc/>
        public Task<OperationResult<IResource>> GetRoot()
        {
            return Task.Run(async () =>
            {
                await _initialization.Task;
                var result = new OperationResult<IResource> ();

                try
                {
                    var root = await _graphServiceClient.Me.Drive.Root.Request()
                    .Select("id,name,folder,file,parentReference")
                    .Expand("thumbnails")
                    .GetAsync();
                    _driveId = root.ParentReference?.DriveId;
                    var resource = CreateResourceInternal(root);
                    result.SucceedWithResult(resource);
                }
                catch(Exception ex)
                {
                    result.FailedWithException(ex);
                }
                return result;
            });
        }

        public Task<OperationResult<IResource>> Get(string id)
        {
            return Task.Run(async () =>
            {
                // folder: Folder metadata, if the item is a folder. Read-only.
                // id: The unique identifier of the item within the Drive. Read-only.
                // lastModifiedDateTime: Date and time the item was last modified. Read-only.
                // name: The name of the item (filename and extension). Read-write.
                // parentReference: https://docs.microsoft.com/en-us/onedrive/developer/rest-api/resources/itemreference?view=odsp-graph-online
                // size: Size of the item in bytes. Read-only.
                // file: https://docs.microsoft.com/en-us/onedrive/developer/rest-api/resources/file?view=odsp-graph-online

                var result = new OperationResult<IResource>();
                try
                {
                    await _initialization.Task;

                    var driveItem = await _graphServiceClient.Me.Drive.Items[id].Request()
                        .Select("id,name,size,file,parentReference,lastModifiedDateTime,folder")
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
            });
       
        }

        public Task<OperationResult<ObservableCollection<IResource>>> GetNestedResources(string folderId)
        {
            return Task.Run(async () =>
            {
                await _initialization.Task;
                var result = new OperationResult<ObservableCollection<IResource>>();
                
                try
                {
                    var resources = new ObservableCollection<IResource>();
                    var request = _graphServiceClient.Me.Drive.Items[folderId].Children.Request()
                        .Select("id,name,size,file,parentReference,lastModifiedDateTime,folder,thumbnails")
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
            });
        }

        public Task<OperationResult<IResource>> Move(string id, string parentId, string targetId)
        {
            // parentId - doesn't used for OneDriveApi
            return Task.Run(async ()=>
            {
                await _initialization.Task;
                var result = new OperationResult<IResource>();

                DriveItem item = new()
                {
                    ParentReference = new ItemReference { 
                        Id = targetId,
                        DriveId = _driveId
                    }
                };

                try
                {
                    var request = await _graphServiceClient.Me.Drive.Items[id].Request().UpdateAsync(item);
                    if (request != null)
                    {
                        var getUpdatedResult = await Get(request.Id);
                        result.CompleteAsResult(getUpdatedResult);
                    }
                }
                catch (ServiceException ex)
                {
                    result.FailedWithException(ex);
                }

                return result;
            });
        }

        public Task<OperationResult< IResource>> Copy(string id, string targetId)
        {
            // 1. Wait initialization
            // 2. Form and execute copy request
            // 3. Wait while copying will completed
            // 4. If copying successed make request to fetch new item and return resource
            // 5. If failed return null 
            return Task.Run(async () =>
            {
                await _initialization.Task;

                var result = new OperationResult<IResource>();
                try
                {
                    ItemReference parentRef = new()
                    {
                        Id = targetId,
                        DriveId = _driveId
                    };

                    var request = _graphServiceClient.Me.Drive.Items[id].Copy(parentReference: parentRef).Request();
                    var message = new HttpRequestMessage(HttpMethod.Post, request.RequestUrl);
                    var json = $"{{\"parentReference\":{{\"driveId\":\"{parentRef.DriveId}\",\"id\":\"{parentRef.Id}\"}}}}";
                    message.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    await _graphServiceClient.AuthenticationProvider.AuthenticateRequestAsync(message);

                    HttpResponseMessage response = await _graphServiceClient.HttpProvider.SendAsync(message);

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
                        var statusMessage = new HttpRequestMessage(HttpMethod.Get, response.Headers.Location);
                        var statusResponce = await httpClient.SendAsync(statusMessage);

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
                    while (!copingCompleted);

                    if (string.IsNullOrEmpty(resourceId))
                    {
                        result.Status = ResutlStatus.Failed;
                        result.ErrorMessage = "Copying resource is failed.";
                        return result;
                    }

                    var getRequest = await Get(resourceId);
                    result.CompleteAsResult(getRequest);
                    return result;
                }
                catch (Exception ex)
                {
                    result.FailedWithException(ex);
                }

                return result;
            });
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
                item.File?.MimeType);
        }

        private Operations _supportedOperations = Operations.CopyFile | Operations.CopyFolder| Operations.DeleteFile | Operations.DeleteFolder | Operations.CutFile | Operations.CutFolder;
        public bool IsOperationSupported(Operations operation)
        {
            return (operation & _supportedOperations) == operation;
        }

        public async Task SignOut()
        {
            foreach (var user in await _clientApp.GetAccountsAsync())
            {
                await _clientApp.RemoveAsync(user);
            }
            TokenCacheHelper.ClearCacheAsync();
        }

        public void Dispose()
        {
            _clientApp = null;
            _graphServiceClient = null;
        }

        public Task<OperationResult<IResource>> Upload(string fileName, string parentId, Stream stream, string contentType)
        {
            return Task.Run(async () =>
            {
                await _initialization.Task;

                // https://docs.microsoft.com/ru-ru/graph/sdks/large-file-upload?tabs=csharp
                var result = new OperationResult<IResource>();
                var uploadProps = new DriveItemUploadableProperties
                {
                    ODataType = null,
                    Name = fileName,
                    AdditionalData = new Dictionary<string, object> { { "@microsoft.graph.conflictBehavior", "rename" } }
                };
                
                try
                {
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
            });
        }
    }
}
