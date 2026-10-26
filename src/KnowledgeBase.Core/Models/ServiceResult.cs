namespace KnowledgeBase.Core.Models;

public enum ServiceResultStatus
{
    Success,
    NotFound,
    ValidationError
}

public class ServiceResult<T>
{
    public ServiceResultStatus Status { get; }
    public T? Data { get; }
    public string? ErrorMessage { get; }

    public bool IsSuccess => Status == ServiceResultStatus.Success;

    private ServiceResult(
        ServiceResultStatus status,
        T? data,
        string? errorMessage)
    {
        Status = status;
        Data = data;
        ErrorMessage = errorMessage;
    }

    public static ServiceResult<T> Success(T data) =>
        new(ServiceResultStatus.Success, data, null);

    public static ServiceResult<T> NotFound(string message) =>
        new(ServiceResultStatus.NotFound, default, message);

    public static ServiceResult<T> ValidationError(string message) =>
        new(ServiceResultStatus.ValidationError, default, message);
}
