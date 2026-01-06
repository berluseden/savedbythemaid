namespace netcore.Models
{
    /// <summary>
    /// Resultado genérico para operaciones de servicio
    /// </summary>
    public class Result
    {
        public bool IsSuccess { get; protected set; }
        public bool IsFailure => !IsSuccess;
        public string Error { get; protected set; } = string.Empty;
        public List<string> Errors { get; protected set; } = new();

        public static Result Ok()
            => new() { IsSuccess = true };

        public static Result Success()
            => Ok();

        public static Result Fail(string error)
            => new() { IsSuccess = false, Error = error };

        public static Result Fail(List<string> errors)
            => new() { IsSuccess = false, Errors = errors, Error = string.Join(", ", errors) };

        public static Result Failure(string error)
            => Fail(error);

        // Métodos genéricos estáticos para Result<T>
        public static Result<T> Success<T>(T value)
            => Result<T>.Ok(value);

        public static Result<T> Failure<T>(string error)
            => Result<T>.Fail(error);
    }

    /// <summary>
    /// Resultado genérico con datos
    /// </summary>
    public class Result<T> : Result
    {
        public T? Value { get; private set; }

        public static Result<T> Ok(T value)
            => new() { IsSuccess = true, Value = value };

        public static new Result<T> Fail(string error)
            => new() { IsSuccess = false, Error = error };

        public static new Result<T> Fail(List<string> errors)
            => new() { IsSuccess = false, Errors = errors, Error = string.Join(", ", errors) };

        public static new Result<T> Failure(string error)
            => Fail(error);

        // Implicit conversion from T to Result<T>
        public static implicit operator Result<T>(T value)
            => Ok(value);
    }

    /// <summary>
    /// DTO para ComboBox/Select con propiedades adicionales para servicios
    /// </summary>
    public class ComboBoxOutPutDto
    {
        public string Value { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}
