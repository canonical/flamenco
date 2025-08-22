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

namespace Flamenco;

public static class ResultExtensions
{
    public static Result WithAnnotation(this Result result, IAnnotation annotation) 
    {
        if (annotation.IsRemark)
        {
            return new Result(
                result.Annotations.Add(annotation),
                result.Remarks.Add(annotation),
                result.Warnings,
                result.Errors);
        }
        
        if (annotation.IsWarning)
        {
            return new Result(
                result.Annotations.Add(annotation),
                result.Remarks,
                result.Warnings.Add(annotation),
                result.Errors);
        }
        
        if (annotation.IsError)
        {
            return new Result(
                result.Annotations.Add(annotation),
                result.Remarks,
                result.Warnings,
                result.Errors.Add(annotation));
        }

        throw new ArgumentException(paramName: nameof(annotation),
            message: $"Invalid annotation severity '{annotation.Severity}'. " +
                     $"Annotations can only be of type {nameof(AnnotationSeverity.Remark)}, " +
                     $"{nameof(AnnotationSeverity.Warning)} or {nameof(AnnotationSeverity.Error)}.");
    }
    
    public static Result<T> WithAnnotation<T>(this Result<T> result, IAnnotation annotation) 
        => new (WithAnnotation((Result)result, annotation), result.Value);
    
    public static Result<T> WithValue<T>(this Result result, T value) => new (result, value);
    
    public static Result<T2> WithValue<T1, T2>(this Result<T1> result, T2 value) => new (result, value);

    public static Result Merge(this Result first, Result second)
    {
        if (first.Annotations.IsEmpty) return second;
        if (second.Annotations.IsEmpty) return first;
        
        return new (
            first.Annotations.AddRange(second.Annotations),
            first.Remarks.AddRange(second.Remarks),
            first.Warnings.AddRange(second.Warnings),
            first.Errors.AddRange(second.Errors));
    }
    
    public static Result<T> Merge<T>(this Result<T> first, Result second)
    {
        if (second.Annotations.IsEmpty) return first;

        Result mergedResult = ((Result)first).Merge(second);
        return first.TryGetValue(out var value) 
            ? new Result<T>(mergedResult, value)
            : new Result<T>(mergedResult);
    }
    
    public static Result<T> Merge<T>(this Result first, Result<T> second)
    {
        if (first.Annotations.IsEmpty) return second;

        Result mergedResult = first.Merge((Result)second);
        return second.TryGetValue(out var value) 
            ? new Result<T>(mergedResult, value)
            : new Result<T>(mergedResult);
    }
    
    public static Result<T2> Merge<T1,T2>(this Result<T1> first, Result<T2> second)
    {
        if (first.Annotations.IsEmpty) return second;

        Result mergedResult = first.Merge((Result)second);
        return second.TryGetValue(out var value) 
            ? new Result<T2>(mergedResult, value)
            : new Result<T2>(mergedResult);
    }

    public static async Task<Result> MergeWhenAll(this Result result, IEnumerable<Task<Result>> resultTasks)
    {
        foreach (var taskResult in await Task.WhenAll(resultTasks).ConfigureAwait(false))
        {
            result = result.Merge(taskResult);
        }

        return result;
    }
    
    public static Result Then(this Result result, Func<Result> action)
    {
        if (result.IsFailure) return result;

        var actionResult = action();
        return result.Merge(actionResult);
    }
    
    public static Result Then<T>(this Result<T> result, Func<T, Result> action)
    {
        if (result.IsFailure) return result;
        
        var actionResult = action(result.Value);
        return result.Merge(actionResult);
    }

    public static Result<T> Then<T>(this Result result, Func<Result<T>> action)
    {
        if (result.IsFailure) return result;

        var actionResult = action();
        return actionResult.HasValue
            ? new Result<T>(result.Merge(actionResult), actionResult.Value)
            : new Result<T>(result.Merge(actionResult));
    }
    
    public static Result<T2> Then<T1, T2>(this Result<T1> result, Func<T1, Result<T2>> action)
    {
        if (result.IsFailure) return new Result<T2>(result);

        var actionResult = action(result.Value);
        return actionResult.HasValue
            ? new Result<T2>(result.Merge(actionResult), actionResult.Value)
            : new Result<T2>(result.Merge(actionResult));
    }

    public static Result<T2> Then<T1, T2>(
        this Result<T1> result, 
        Func<T1, T2> action)
    {
        if (result.IsFailure) return new Result<T2>(result);
        return new Result<T2>(result, action(result.Value));
    }
    
    public static async ValueTask<Result> Then(
        this Result result, 
        Func<ValueTask<Result>> action)
    {
        if (result.IsFailure) return result;
        
        var actionResult = await action().ConfigureAwait(false);
        return result.Merge(actionResult);
    }
    
    public static async Task<Result> Then<T>(
        this Result<T> result, 
        Func<T, ValueTask<Result>> action)
    {
        if (result.IsFailure) return result;
        
        var actionResult = await action(result.Value).ConfigureAwait(false);
        return result.Merge(actionResult);
    }
    
    public static async Task<Result> Then<T>(
        this Result<T> result, 
        Func<T, Task<Result>> action)
    {
        if (result.IsFailure) return result;
        
        var actionResult = await action(result.Value).ConfigureAwait(false);
        return result.Merge(actionResult);
    }

    public static async Task<Result<T>> Then<T>(
        this Result result,
        Func<Task<Result<T>>> action)
    {
        if (result.IsFailure) return result;

        var actionResult = await action().ConfigureAwait(false);
        return result.Merge(actionResult);
    }

    public static async Task<Result> Then<T>(
        this Task<Result<T>> resultTask,
        Func<T, Task<Result>> action)
    {
        var result = await resultTask.ConfigureAwait(false);
        return await result.Then(action).ConfigureAwait(false);
    }
    
    public static async Task<Result<T2>> Then<T1, T2>(
        this Result<T1> result, 
        Func<T1, ValueTask<Result<T2>>> action)
    {
        if (result.IsFailure) return new Result<T2>(result);
        
        var actionResult = await action(result.Value).ConfigureAwait(false);
        return result.Merge(actionResult);
    }
    
    public static async Task<Result<T2>> Then<T1, T2>(
        this Result<T1> result, 
        Func<T1, Task<Result<T2>>> action)
    {
        if (result.IsFailure) return new Result<T2>(result);
        
        var actionResult = await action(result.Value).ConfigureAwait(false);
        return result.Merge(actionResult);
    }

    public static async ValueTask<Result> Then(
        this ValueTask<Result> action1, 
        Func<ValueTask<Result>> action2)
    {
        var result = await action1.ConfigureAwait(false);
        return await result.Then(action2).ConfigureAwait(false);
    }
    
    public static async Task<Result> Then(
        this Task<Result> action1, 
        Func<Task<Result>> action2)
    {
        var result = await action1.ConfigureAwait(false);
        return await result.Then(action2).ConfigureAwait(false);
    }
    
    public static async Task<Result> Then(
        this Result result, 
        Func<Task<Result>> action)
    {
        if (result.IsFailure) return result;
        
        var actionResult = await action().ConfigureAwait(false);
        return result.Merge(actionResult);
    }
    
    public static Result<TNew> Map<TOld, TNew>(this Result<TOld> result, Func<TOld, TNew> valueMapper)
        => result.TryGetValue(out var value)
            ? new Result<TNew>(result, valueMapper(value))
            : new Result<TNew>(result);
    
    public static Result<TNew> Map<TOld, TNew>(this Result<TOld> result, Func<Result<TOld>, TNew> valueMapper)
        => result.HasValue
            ? new Result<TNew>(result, valueMapper(result))
            : new Result<TNew>(result);

    public static void ThrowIfError(this Result result, Func<IAnnotation, Exception>? exceptionMapper)
    {
        if (result.IsSuccess) return;
        
        exceptionMapper ??= DefaultExceptionMapper;
        if (result.Errors.Count == 1) throw exceptionMapper(result.Errors[0]);
        throw new AggregateException(result.Errors.Select(exceptionMapper));
        
        static Exception DefaultExceptionMapper(IAnnotation error) => new Exception(error.Message);
    }

    public static void ThrowIfError<T>(this Result<T> result, Func<IAnnotation, Exception>? exceptionMapper = null)
        => ThrowIfError((Result)result, exceptionMapper);

    public static T ThrowIfErrorOrReturnValue<T>(
        this Result<T> result,
        Func<IAnnotation, Exception>? exceptionMapper = null)
    {
        result.ThrowIfError(exceptionMapper);
        return result.Value;
    }

}
