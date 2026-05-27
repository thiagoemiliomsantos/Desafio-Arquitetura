namespace CashFlow.SharedKernel.Results;

/// <summary>Encapsula o resultado de uma operação que pode falhar por razões de domínio esperadas.</summary>
/// <typeparam name="T">Tipo do valor retornado em caso de sucesso.</typeparam>
public sealed class Result<T>
{
    /// <summary>Indica se a operação foi concluída com sucesso.</summary>
    public bool IsSuccess { get; }

    /// <summary>Indica se a operação falhou.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Valor retornado em caso de sucesso. Nulo em caso de falha.</summary>
    public T? Value { get; }

    /// <summary>Mensagem de erro em caso de falha. Nula em caso de sucesso.</summary>
    public string? Error { get; }

    private Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    /// <summary>Cria um resultado de sucesso com o valor informado.</summary>
    public static Result<T> Ok(T value) => new(true, value, null);

    /// <summary>Cria um resultado de falha com a mensagem de erro informada.</summary>
    public static Result<T> Fail(string error) => new(false, default, error);
}
