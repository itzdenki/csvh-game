// Feature: tower-defense-vn
// Validates: Requirements 1.1, 1.3 (foundation primitive used by ConfigLoader and other Core modules).

using System;

namespace CSVH.Core.Common
{
    /// <summary>
    /// Discriminated union biểu diễn kết quả thành công (<see cref="Ok"/>) hoặc lỗi (<see cref="Err"/>).
    /// Là <c>readonly struct</c> nên không phát sinh allocation; an toàn dùng trong hot path
    /// (ví dụ <c>ConfigLoader.Load</c>). Theo nguyên tắc Core không ném exception cho đường
    /// dẫn bình thường, các API trả <see cref="Result{T,E}"/> thay vì throw.
    /// </summary>
    /// <typeparam name="T">Kiểu giá trị khi thành công.</typeparam>
    /// <typeparam name="E">Kiểu lỗi khi thất bại.</typeparam>
    public readonly struct Result<T, E> : IEquatable<Result<T, E>>
    {
        private readonly T _value;
        private readonly E _error;

        /// <summary>Cho biết kết quả là <c>Ok</c>.</summary>
        public bool IsOk { get; }

        /// <summary>Cho biết kết quả là <c>Err</c>.</summary>
        public bool IsErr => !IsOk;

        private Result(T value, E error, bool isOk)
        {
            _value = value;
            _error = error;
            IsOk = isOk;
        }

        /// <summary>Truy cập giá trị thành công. Ném <see cref="InvalidOperationException"/> nếu là <c>Err</c>.</summary>
        public T Value =>
            IsOk
                ? _value
                : throw new InvalidOperationException("Result is Err; cannot access Value. Use IsOk/Match/TryGetValue instead.");

        /// <summary>Truy cập giá trị lỗi. Ném <see cref="InvalidOperationException"/> nếu là <c>Ok</c>.</summary>
        public E Error =>
            IsErr
                ? _error
                : throw new InvalidOperationException("Result is Ok; cannot access Error. Use IsErr/Match/TryGetError instead.");

        /// <summary>Tạo kết quả thành công.</summary>
        public static Result<T, E> Ok(T value) => new Result<T, E>(value, default, isOk: true);

        /// <summary>Tạo kết quả lỗi.</summary>
        public static Result<T, E> Err(E error) => new Result<T, E>(default, error, isOk: false);

        /// <summary>Áp dụng một trong hai hàm tương ứng với nhánh hiện tại và trả giá trị.</summary>
        public TResult Match<TResult>(Func<T, TResult> ok, Func<E, TResult> err)
        {
            if (ok is null) throw new ArgumentNullException(nameof(ok));
            if (err is null) throw new ArgumentNullException(nameof(err));
            return IsOk ? ok(_value) : err(_error);
        }

        /// <summary>Phiên bản void của <see cref="Match{TResult}"/>.</summary>
        public void Match(Action<T> ok, Action<E> err)
        {
            if (ok is null) throw new ArgumentNullException(nameof(ok));
            if (err is null) throw new ArgumentNullException(nameof(err));
            if (IsOk) ok(_value); else err(_error);
        }

        /// <summary>Trả về giá trị nếu là <c>Ok</c>; ngược lại trả <paramref name="fallback"/>.</summary>
        public T ValueOr(T fallback) => IsOk ? _value : fallback;

        /// <summary>Try-pattern cho nhánh <c>Ok</c>.</summary>
        public bool TryGetValue(out T value)
        {
            value = IsOk ? _value : default;
            return IsOk;
        }

        /// <summary>Try-pattern cho nhánh <c>Err</c>.</summary>
        public bool TryGetError(out E error)
        {
            error = IsErr ? _error : default;
            return IsErr;
        }

        /// <summary>Map giá trị <c>Ok</c> sang kiểu khác; giữ nguyên nhánh <c>Err</c>.</summary>
        public Result<TNew, E> Map<TNew>(Func<T, TNew> mapper)
        {
            if (mapper is null) throw new ArgumentNullException(nameof(mapper));
            return IsOk ? Result<TNew, E>.Ok(mapper(_value)) : Result<TNew, E>.Err(_error);
        }

        /// <summary>Map giá trị <c>Err</c> sang kiểu khác; giữ nguyên nhánh <c>Ok</c>.</summary>
        public Result<T, ENew> MapErr<ENew>(Func<E, ENew> mapper)
        {
            if (mapper is null) throw new ArgumentNullException(nameof(mapper));
            return IsOk ? Result<T, ENew>.Ok(_value) : Result<T, ENew>.Err(mapper(_error));
        }

        public bool Equals(Result<T, E> other)
        {
            if (IsOk != other.IsOk) return false;
            return IsOk
                ? System.Collections.Generic.EqualityComparer<T>.Default.Equals(_value, other._value)
                : System.Collections.Generic.EqualityComparer<E>.Default.Equals(_error, other._error);
        }

        public override bool Equals(object obj) => obj is Result<T, E> other && Equals(other);

        public override int GetHashCode() =>
            IsOk
                ? HashCode.Combine(true, _value)
                : HashCode.Combine(false, _error);

        public static bool operator ==(Result<T, E> left, Result<T, E> right) => left.Equals(right);
        public static bool operator !=(Result<T, E> left, Result<T, E> right) => !left.Equals(right);

        public override string ToString() => IsOk ? $"Ok({_value})" : $"Err({_error})";
    }

    /// <summary>
    /// Helper tĩnh giúp suy luận kiểu khi dựng <see cref="Result{T,E}"/> mà không cần khai báo
    /// đầy đủ tham số kiểu tại điểm gọi.
    /// </summary>
    public static class Result
    {
        /// <summary>Tạo <see cref="Result{T,E}.Ok"/>.</summary>
        public static Result<T, E> Ok<T, E>(T value) => Result<T, E>.Ok(value);

        /// <summary>Tạo <see cref="Result{T,E}.Err"/>.</summary>
        public static Result<T, E> Err<T, E>(E error) => Result<T, E>.Err(error);
    }
}
