// This file is part of Flamenco
// Copyright 2024 Canonical Ltd.
// This program is free software: you can redistribute it and/or modify it under the terms of the
// GNU General Public License version 3, as published by the Free Software Foundation.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
// even the implied warranties of MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with this program.
// If not, see <http://www.gnu.org/licenses/>.

// NOTE: This is inspired by FluentResults (Copyright FluentResults).
// See: https://github.com/altmann/FluentResults

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Flamenco;

/// <summary>
/// Represents the result of an operation.
/// </summary>
public readonly struct Result
{
    public static Result Success = new Result();
    
    public Result()
    {
        Annotations = ImmutableList<IAnnotation>.Empty;
        Remarks = ImmutableList<IAnnotation>.Empty;
        Warnings = ImmutableList<IAnnotation>.Empty;
        Errors = ImmutableList<IAnnotation>.Empty;
    }

    internal Result(
        ImmutableList<IAnnotation> annotations,
        ImmutableList<IAnnotation> remarks,
        ImmutableList<IAnnotation> warnings,
        ImmutableList<IAnnotation> errors)
    {
        Annotations = annotations;
        Remarks = remarks;
        Warnings = warnings;
        Errors = errors;
    }

    /// <summary>
    /// Collection of all annotations, which are related to this result.
    /// </summary>
    /// <remarks>
    /// This collection is the union of <see cref="Errors"/>, <see cref="Warnings"/>
    /// and <see cref="Remarks"/>.
    /// </remarks>
    public ImmutableList<IAnnotation> Annotations { get; }

    /// <summary>
    /// Subset of <see cref="Annotations"/> which are <see cref="AnnotationSeverity.Error"/>.
    /// </summary>
    public ImmutableList<IAnnotation> Errors { get; }

    /// <summary>
    /// Subset of <see cref="Annotations"/> which are <see cref="AnnotationSeverity.Warning"/>.
    /// </summary>
    public ImmutableList<IAnnotation> Warnings { get; }

    /// <summary>
    /// Subset of <see cref="Annotations"/> which are <see cref="AnnotationSeverity.Remark"/>.
    /// </summary>
    public ImmutableList<IAnnotation> Remarks { get; }

    /// <summary>
    /// The operation, represented by this <see cref="Result"/> instance, succeeded without any errors.
    /// </summary>
    public bool IsSuccess => Errors.IsEmpty;

    /// <summary>
    /// The operation, represented by this <see cref="Result"/> instance, succeeded without any errors.
    /// </summary>
    public bool IsFailure => !Errors.IsEmpty;

    public static implicit operator Result(AnnotationBase annotation) => Result.Success.WithAnnotation(annotation);
}

/// <summary>
/// Represents the result of an operation that may hold a value.
/// </summary>
public readonly struct Result<T>
{
    private readonly Result _result;
    private readonly bool _hasValue;
    private readonly T _value;

    public Result() : this(Result.Success)
    {
    }

    public Result(Result result)
    {
        _result = result;
        _hasValue = false;
        _value = default!;
    }

    public Result(Result result, T value)
    {
        _result = result;
        _hasValue = true;
        _value = value;
    }

    /// <inheritdoc cref="Result.Annotations" />
    public ImmutableList<IAnnotation> Annotations => _result.Annotations;

    /// <inheritdoc cref="Result.Errors" />
    public ImmutableList<IAnnotation> Errors => _result.Errors;

    /// <inheritdoc cref="Result.Warnings" />
    public ImmutableList<IAnnotation> Warnings => _result.Warnings;

    /// <inheritdoc cref="Result.Remarks" />
    public ImmutableList<IAnnotation> Remarks => _result.Remarks;

    /// <inheritdoc cref="Result.Success" />
    public bool IsSuccess => _result.IsSuccess && _hasValue;

    /// <inheritdoc cref="Result.IsFailure" />
    public bool IsFailure => _result.IsFailure;

    /// <summary>
    /// <see langword="true"/> if this result has a value; otherwise <see langword="false"/>.
    /// </summary>
    public bool HasValue => _hasValue;

    /// <summary>
    /// Gets the value of the result.
    /// </summary>
    /// <exception cref="InvalidOperationException">When <see cref="HasValue"/> is <see langword="false"/>.</exception>
    public T Value => _hasValue ? _value : throw new InvalidOperationException("Result does not have a value");

    /// <summary>
    ///
    /// </summary>
    /// <param name="value">
    ///
    /// </param>
    /// <returns></returns>
    public bool TryGetValue([MaybeNullWhen(returnValue: false)] out T value)
    {
        value = _value;
        return _hasValue;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="value">
    /// Will contain <see cref="Value"/> if returned <see langword="true"/>;
    /// otherwise will contain <paramref name="defaultValue"/>.
    /// </param>
    /// <param name="defaultValue">
    /// The value that should be returned if this result does not have a value.
    /// </param>
    /// <returns><see langword="true"/> if this result has a value; otherwise <see langword="false"/>.</returns>
    public bool GetValueOrDefault(out T? value, T? defaultValue)
    {
        value = GetValueOrDefault(defaultValue);
        return _hasValue;
    }

    /// <summary>
    /// Gets the value of this result if this result has a value; otherwise returns a specified default value.
    /// </summary>
    /// <param name="defaultValue">The value that should be returned if <see cref="HasValue"/> is <see langword="false"/>.</param>
    /// <returns><see cref="Value"/> if <see cref="HasValue"/> is <see langword="true"/>; otherwise <paramref name="defaultValue"/>.</returns>
    public T? GetValueOrDefault(T? defaultValue = default)
        => _hasValue ? _value : defaultValue;

    public static implicit operator Result(Result<T> result) => result._result;
    public static implicit operator Result<T>(T value) => new (Result.Success, value);
    public static implicit operator Result<T>(Result result) => new (result);
    public static implicit operator Result<T>(AnnotationBase annotation) => new Result<T>(annotation);
}
