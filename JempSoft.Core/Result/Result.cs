using System;
using System.Collections.Generic;
using System.Linq;

namespace JempSoft.Core.Result
{
    /// <summary>
    /// Represents the result of an operation that can succeed or fail
    /// </summary>
    public class Result
    {
        protected Result(bool isSuccess, string? error)
        {
            if (isSuccess && !string.IsNullOrEmpty(error))
                throw new InvalidOperationException("A successful result cannot have an error message.");
            
            if (!isSuccess && string.IsNullOrEmpty(error))
                throw new InvalidOperationException("A failed result must have an error message.");

            IsSuccess = isSuccess;
            Error = error;
        }

        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public string? Error { get; }

        public static Result Success() => new(true, null);
        public static Result Failure(string error) => new(false, error);
        
        public static Result<T> Success<T>(T value) => Result<T>.Success(value);
        public static Result<T> Failure<T>(string error) => Result<T>.Failure(error);

        public static Result Combine(params Result[] results)
        {
            var failedResults = results.Where(r => r.IsFailure).ToList();
            
            return failedResults.Count == 0 
                ? Success() 
                : Failure(string.Join("; ", failedResults.Select(r => r.Error)));
        }
    }

    /// <summary>
    /// Represents the result of an operation that returns a value and can succeed or fail
    /// </summary>
    public class Result<T> : Result
    {
        private readonly T? _value;

        protected Result(T? value, bool isSuccess, string? error) 
            : base(isSuccess, error)
        {
            _value = value;
        }

        public T Value
        {
            get
            {
                if (IsFailure)
                    throw new InvalidOperationException("Cannot access Value on a failed result. Check IsSuccess before accessing Value.");
                
                return _value!;
            }
        }

        public T? ValueOrDefault => _value;

        public static new Result<T> Success(T value) => new(value, true, null);
        public static new Result<T> Failure(string error) => new(default, false, error);

        public static implicit operator Result<T>(T value) => Success(value);

        /// <summary>
        /// Maps the value to another type if successful
        /// </summary>
        public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
        {
            return IsSuccess 
                ? Result<TNew>.Success(mapper(Value)) 
                : Result<TNew>.Failure(Error!);
        }

        /// <summary>
        /// Executes action if successful and returns the same result
        /// </summary>
        public Result<T> OnSuccess(Action<T> action)
        {
            if (IsSuccess)
                action(Value);
            return this;
        }

        /// <summary>
        /// Executes action if failed and returns the same result
        /// </summary>
        public Result<T> OnFailure(Action<string> action)
        {
            if (IsFailure)
                action(Error!);
            return this;
        }
    }

    /// <summary>
    /// Represents a paginated result
    /// </summary>
    public class PagedResult<T>
    {
        public List<T> Items { get; }
        public int TotalCount { get; }
        public int Page { get; }
        public int PageSize { get; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;

        public PagedResult(List<T> items, int totalCount, int page, int pageSize)
        {
            Items = items ?? throw new ArgumentNullException(nameof(items));
            TotalCount = totalCount;
            Page = page;
            PageSize = pageSize;
        }

        public static PagedResult<T> Create(List<T> items, int totalCount, int page, int pageSize)
            => new(items, totalCount, page, pageSize);
    }
}
