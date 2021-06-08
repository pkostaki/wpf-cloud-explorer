using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace StorageLib.CloudStorage.Implementation
{
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
    }

    public enum ResutlStatus
    {
        None = 0,
        Succeed = 1,
        Failed = 2,
    }

    /// <summary>
    /// Class that describe result of operation.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class OperationResult<T>
    {
        /// <summary>
        /// Status of operation.
        /// </summary>
        public ResutlStatus Status { get; set; } = ResutlStatus.None;

        /// <summary>
        /// Error code if available.
        /// </summary>
        public int ErrorCode { get;  set; }

        /// <summary>
        /// Error message if available.
        /// </summary>
        public string ErrorMessage { get;  set; }

        /// <summary>
        /// Result.
        /// </summary>
        public T Result { get;  set; }
        
        /// <summary>
        /// Succeed with result.
        /// </summary>
        /// <param name="result">Result.</param>
        /// <returns></returns>
        public OperationResult<T> SucceedWithResult(T result)
        {
            Status = ResutlStatus.Succeed;
            Result = result;
            return this;
        }

        /// <summary>
        /// Failed with exception.
        /// </summary>
        /// <param name="ex">Exception.</param>
        /// <returns></returns>
        public OperationResult<T> FailedWithException(Exception ex)
        {
            Status = ResutlStatus.Failed;
            ErrorCode = ex.HResult;
            ErrorMessage = ex.Message;
            return this;
        }      
        
        /// <summary>
        /// Failed with result.
        /// </summary>
        /// <param name="failedResult">result</param>
        /// <returns></returns>
        public OperationResult<T> FailedWithResult(OperationResult<T> failedResult)
        {
            Status = ResutlStatus.Failed;
            ErrorCode = failedResult.ErrorCode;
            ErrorMessage = failedResult.ErrorMessage;
            return this;
        }    
        
        /// <summary>
        /// Complete with depending on request result.
        /// </summary>
        /// <param name="result">Request result.</param>
        /// <returns></returns>
        public OperationResult<T> CompleteAsResult(OperationResult<T> result)
        {
            Status = result.Status;
            ErrorCode = result.ErrorCode;
            ErrorMessage = result.ErrorMessage;
            Result = result.Result;
            return this;
        }     
        
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
        
    }
}

