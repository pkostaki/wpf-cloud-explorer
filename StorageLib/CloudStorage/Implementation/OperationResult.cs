using System;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace StorageLib.CloudStorage.Implementation
{
    /// <summary>
    /// Operations.
    /// </summary>
    [Flags]
    public enum Operations
    {
        None = 0,
        DeleteFile = 1,
        DeleteFolder = 2,
        CopyFile = 4,
        CopyFolder = 8,
        CutFile = 16,
        CutFolder = 32,
        UploadFile = 64
    }

    /// <summary>
    /// Operation result.
    /// </summary>
    public enum ResutlStatus
    {
        None = 0,
        Succeed = 1,
        Failed = 2,
    }

    /// <summary>
    /// Represent the result of operation.
    /// </summary>
    /// <typeparam name="T">Type of result.</typeparam>
    public class OperationResult<T> : INotifyPropertyChanged
    {
        private ResutlStatus _status = ResutlStatus.None;
        private int _errorCode;
        private string _errorMessage;

        /// <summary>
        /// Status of operation.
        /// </summary>
        public ResutlStatus Status { get => _status; set => Set(ref _status, value); }

        /// <summary>
        /// Error code if available.
        /// </summary>
        public int ErrorCode { get => _errorCode; set => Set(ref _errorCode, value); }

        /// <summary>
        /// Error message if available.
        /// </summary>
        public string ErrorMessage { get => _errorMessage; set => Set( ref  _errorMessage, value); }

        /// <summary>
        /// Result.
        /// </summary>
        public T Result { get; set; }
        
        ///<inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;

        protected void Set<K>(ref K property, K value, [CallerMemberName] string propName = null)
        {
            property = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        /// <summary>
        /// Succeed operation with <paramref name="result"/>.
        /// </summary>
        /// <param name="result">Result.</param>
        /// <returns>Operation result.</returns>
        public OperationResult<T> SucceedWithResult(T result)
        {
            Status = ResutlStatus.Succeed;
            Result = result;
            return this;
        }

        /// <summary>
        /// Failed operation with <paramref name="ex"/>.
        /// </summary>
        /// <param name="ex">Exception.</param>
        /// <returns>Operation result.</returns>
        public OperationResult<T> FailedWithException(Exception ex)
        {
            Status = ResutlStatus.Failed;
            ErrorCode = ex.HResult;
            ErrorMessage = ex.Message;
            return this;
        }

        /// <summary>
        /// Failed operation with <paramref name="result"/>.
        /// </summary>
        /// <param name="result">result</param>
        /// <returns>Operation result.</returns>
        public OperationResult<T> FailedWithResult(OperationResult<T> result)
        {
            Status = ResutlStatus.Failed;
            ErrorCode = result.ErrorCode;
            ErrorMessage = result.ErrorMessage;
            return this;
        }

        /// <summary>
        /// Repeat operation of <paramref name="result"/>.
        /// </summary>
        /// <param name="result">Request result.</param>
        /// <returns>Operation result.</returns>
        public OperationResult<T> CompleteWithResult(OperationResult<T> result)
        {
            Status = result.Status;
            ErrorCode = result.ErrorCode;
            ErrorMessage = result.ErrorMessage;
            Result = result.Result;
            return this;
        }

        /// <summary>
        /// Failed operation based on <paramref name="response"/>
        /// </summary>
        /// <param name="response"><see cref="HttpResponseMessage"/></param>
        /// <returns>Operation result.</returns>
        public async Task<OperationResult<T>> FailedBasedHttpResponce(HttpResponseMessage response)
        {
            Status = ResutlStatus.Failed;
            ErrorCode = (int)response.StatusCode;
            if (response.RequestMessage.Content != null)
            {
                ErrorMessage = await response.RequestMessage.Content.ReadAsStringAsync();
            }
            return this;
        }

        /// <summary>
        /// Create not initialize operation result.
        /// </summary>
        /// <typeparam name="T">Type of result.</typeparam>
        /// <returns></returns>
        public static OperationResult<T> FailedWithNotInitializeResult()
        {
            return new OperationResult<T>
            {
                Status = ResutlStatus.Failed,
                ErrorMessage = "Storage not initialized.",
                Result = default(T)
            };
        }

    }
}

