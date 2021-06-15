using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using StorageLib.CloudStorage.Api;
using StorageLib.CloudStorage.Implementation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using File = Google.Apis.Drive.v3.Data.File;

namespace StorageLib
{
    /// <summary>
    /// Implements Google Drive resource operations.
    /// </summary>
    public sealed class GoogleDriveApi : ICloudStorageApi
    {
        private readonly string[] _scopes = { DriveService.Scope.Drive };
        private readonly string _credentialsFilePath;
        private readonly string _applicationName;
        private readonly IResourceFactory _resourceFactory;
        private DriveService _service;
        private const string _mimetypeFolder = "application/vnd.google-apps.folder";
        private readonly TaskCompletionSource<bool> _initialization = new();
        private UserCredential _credential;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="credentialsFilePath">Credentials file path.</param>
        /// <param name="applicationName">Application name.</param>
        /// <param name="resourceFactory">Resource creation factory.</param>
        public GoogleDriveApi(string credentialsFilePath, string applicationName, IResourceFactory resourceFactory)
        {
            _credentialsFilePath = credentialsFilePath;
            _applicationName = applicationName;
            _resourceFactory = resourceFactory;

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                using (var stream =
                    new FileStream(_credentialsFilePath, FileMode.Open, FileAccess.Read))
                {
                    // The file token.json stores the user's access and refresh tokens, and is created
                    // automatically when the authorization flow completes for the first time.
                    string credPath = "token.json";
                    _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.Load(stream).Secrets,
                        _scopes,
                        "user",
                        CancellationToken.None,
                        new FileDataStore(credPath, true));
                }

                // Create Drive API service.
                _service = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = _credential,
                    ApplicationName = _applicationName,
                });
                _initialization.TrySetResult(true);
            }
            catch (Exception)
            {
                _initialization.TrySetResult(false);
            }
        }

        ///<inheritdoc/>
        public Task<OperationResult<IResource>> GetRoot()
        {
            return Get("root");
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
                FilesResource.GetRequest getRequest = _service.Files.Get(id);
                getRequest.Fields = "id, name, iconLink, kind, size, mimeType, modifiedTime, fullFileExtension,trashed, parents, webViewLink";

                var file = await getRequest.ExecuteAsync();
                var resource = CreateResourceInternal(file);
                result.SucceedWithResult(resource);
            }
            catch (Exception ex)
            {
                result.FailedWithException(ex);
            }
            return result;
        }

        ///<inheritdoc/>
        public async Task<OperationResult<ObservableCollection<IResource>>> GetNestedResources(string id)
        {
            if (await _initialization.Task == false)
            {
                return OperationResult<ObservableCollection<IResource>>.FailedWithNotInitializeResult();
            }

            var result = new OperationResult<ObservableCollection<IResource>>();

            try
            {
                var resources = new ObservableCollection<IResource>();
                string pagetoken = null;
                do
                {
                    FilesResource.ListRequest listRequest = _service.Files.List();
                    listRequest.Q = $"parents = '{id}'";
                    listRequest.Spaces = "drive";
                    listRequest.Fields = "nextPageToken, files(id, name, iconLink, kind, size, mimeType, modifiedTime, fullFileExtension,trashed, webViewLink)";
                    listRequest.PageToken = pagetoken;

                    IList<File> files = (await listRequest.ExecuteAsync()).Files;
                    if (files != null && files.Count > 0)
                    {
                        foreach (var file in files)
                        {
                            if (file.Trashed.HasValue && file.Trashed.Value)
                            {
                                continue;
                            }
                            var resource = CreateResourceInternal(file);
                            resources.Add(resource);
                        }
                    }
                } while (pagetoken != null);

                result.Status = ResutlStatus.Succeed;
                result.Result = resources;
            }
            catch (Exception ex)
            {
                result.FailedWithException(ex);
            }

            return result;
        }

        ///<inheritdoc/>
        public async Task<OperationResult<IResource>> Move(string id, string parentId, string targetId)
        {
            if (await _initialization.Task == false)
            {
                return OperationResult<IResource>.FailedWithNotInitializeResult();
            }

            var result = new OperationResult<IResource>();

            try
            {
                File newFile = new();
                FilesResource.UpdateRequest request = _service.Files.Update(newFile, id);
                request.AddParents = targetId;
                request.RemoveParents = parentId;

                request.Fields = "id, name, iconLink, kind, size, mimeType, modifiedTime, fullFileExtension,trashed, parents, webViewLink";

                File file = await request.ExecuteAsync();
                result.Result = CreateResourceInternal(file);
                result.Status = ResutlStatus.Succeed;
            }
            catch (Exception ex)
            {
                result.FailedWithException(ex);
            }

            return result;
        }

        ///<inheritdoc/>
        public async Task<OperationResult<IResource>> Copy(string id, string targetId)
        {
            if (await _initialization.Task == false)
            {
                return OperationResult<IResource>.FailedWithNotInitializeResult();
            }
            var result = new OperationResult<IResource>();

            try
            {
                File copiedFile = new();
                copiedFile.Parents = new List<string>() { targetId };
                FilesResource.CopyRequest request = _service.Files.Copy(copiedFile, id);
                request.Fields = "id, name, iconLink, kind, size, mimeType, modifiedTime, fullFileExtension,trashed, parents, webViewLink";

                File file = await request.ExecuteAsync();
                result.Result = CreateResourceInternal(file);
                result.Status = ResutlStatus.Succeed;
            }
            catch (Exception ex)
            {
                result.FailedWithException(ex);
            }

            return result;
        }

        private IResource CreateResourceInternal(File file)
        {
            var isFolder = file.MimeType.Equals(_mimetypeFolder, StringComparison.OrdinalIgnoreCase);
            var resource = _resourceFactory.Create(this, isFolder, file.Id,
                file.IconLink,
                file.ModifiedTime,
                file.Name,
                file.Size,
                file.MimeType,
                file.WebViewLink);
            return resource;
        }

        ///<inheritdoc/>
        public async Task<OperationResult<bool>> Delete(string id)
        {
            if (await _initialization.Task == false)
            {
                return OperationResult<bool>.FailedWithNotInitializeResult();
            }

            var result = new OperationResult<bool>();

            try
            {
                FilesResource.DeleteRequest request = _service.Files.Delete(id);
                var res = await request.ExecuteAsync();
                result.Status = res == string.Empty ? ResutlStatus.Succeed : ResutlStatus.Failed;
                result.Result = res == string.Empty;
            }
            catch (Exception ex)
            {
                result.FailedWithException(ex);
            }

            return result;
        }

        ///<inheritdoc/>
        public async Task<OperationResult<IResource>> Upload(string fileName, string parentId, Stream stream, string contentType)
        {
            if (await _initialization.Task == false)
            {
                return OperationResult<IResource>.FailedWithNotInitializeResult();
            }

            var result = new OperationResult<IResource>();

            try
            {
                File uploaded = new();
                uploaded.Name = fileName;
                uploaded.Parents = new List<string> { parentId };
                uploaded.Id = (await _service.Files.GenerateIds().ExecuteAsync()).Ids[0];
                FilesResource.CreateMediaUpload request = _service.Files.Create(uploaded, stream, contentType);
                var uploadResult = await request.UploadAsync();
                if (uploadResult.Status == Google.Apis.Upload.UploadStatus.Completed)
                {
                    var getUploadedFileResult = await Get(uploaded.Id);// todo fill upload request with fields to remove this extra request.
                    result.CompleteWithResult(getUploadedFileResult);
                }
                else
                {
                    result.Status = ResutlStatus.Failed;
                    if (uploadResult.Exception != null)
                    {
                        result.FailedWithException(uploadResult.Exception);
                    }
                }
            }
            catch (Exception ex)
            {
                result.FailedWithException(ex);
            }

            return result;
        }

        ///<inheritdoc/>
        public async Task SignOut()
        {
            await _credential.RevokeTokenAsync(CancellationToken.None);
        }

        ///<inheritdoc/>
        public void Dispose()
        {
            _initialization.TrySetResult(false);
            _credential = null;
            _service?.Dispose();
            _service = null;
        }

        ///<inheritdoc/>
        public string CloudStorageName => "Google Drive";

        private Operations _supportedOperations = Operations.CopyFile |
                                                  Operations.DeleteFile |
                                                  Operations.DeleteFolder |
                                                  Operations.CutFile |
                                                  Operations.CutFolder |
                                                  Operations.UploadFile;
        ///<inheritdoc/>
        public bool IsOperationSupported(Operations operation)
        {
            return (operation & _supportedOperations) == operation;
        }
    }
}

