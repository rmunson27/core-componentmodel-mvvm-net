using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ObservableObjectBase = CommunityToolkit.Mvvm.ComponentModel.ObservableObject;

namespace Rem.Core.ComponentModel.Mvvm;

/// <summary>
/// A base class for objects with properties that must be observable.
/// </summary>
/// <remarks>
/// This class provides methods that will be called on the old value immediately after a property changing event and
/// the new value immediately before a property changed event.
/// These methods are <b><i>only</i></b> called on <see cref="SetProperty"/> overloads and not 
/// on <see cref="ObservableObjectBase.SetPropertyAndNotifyOnCompletion"/> overloads. An extending abstract class can
/// implement the remaining methods if desired, hiding the corresponding
/// <see cref="ObservableObjectBase.SetPropertyAndNotifyOnCompletion"/> overloads, or they may be added
/// in an upcoming version.
/// </remarks>
public abstract class ObservableObject : ObservableObjectBase
{
    /// <inheritdoc cref="ObservableObjectBase.SetProperty{T}(ref T, T, string?)"/>
    protected new bool SetProperty<T>(
            [NotNullIfNotNull(nameof(newValue))] ref T? field, T? newValue,
            [CallerMemberName] string? propertyName = null)
        => SetProperty(ref field, newValue, comparer: null, propertyName);

    /// <inheritdoc cref="ObservableObjectBase.SetProperty{T}(ref T, T, IEqualityComparer{T}, string?)"/>
    protected new bool SetProperty<T>(
        [NotNullIfNotNull(nameof(newValue))] ref T? field, T? newValue,
        IEqualityComparer<T>? comparer,
        [CallerMemberName] string? propertyName = null)
    {
        if (comparer.DefaultIfNull().Equals(field!, newValue!)) return false;

        OnPropertyChanging(propertyName);
        SetUpAfterPropertyChanging(field, propertyName);
        field = newValue;
        CleanUpBeforePropertyChanged(field, propertyName);
        OnPropertyChanged(propertyName);

        return true;
    }

    /// <inheritdoc cref="SetProperty{T}(T, T, IEqualityComparer{T}?, Action{T}, string?)"/>
    protected new bool SetProperty<T>(
            T? oldValue, T? newValue, Action<T?> callback, [CallerMemberName] string? propertyName = null)
        => SetProperty(oldValue, newValue, comparer: null, callback, propertyName);

    /// <inheritdoc cref="ObservableObjectBase.SetProperty{T}(T, T, Action{T}, string?)"/>
    protected new bool SetProperty<T>(
        T? oldValue, T? newValue,
        IEqualityComparer<T>? comparer,
        Action<T?> callback,
        [CallerMemberName] string? propertyName = null)
    {
        if (comparer.DefaultIfNull().Equals(oldValue!, newValue!)) return false; // Property is not changing

        OnPropertyChanging(propertyName);
        SetUpAfterPropertyChanging(oldValue, propertyName);
        callback(newValue);
        CleanUpBeforePropertyChanged(newValue, propertyName);
        OnPropertyChanged(propertyName);

        return true;
    }

    /// <inheritdoc cref="SetProperty{TModel, T}(T, T, IEqualityComparer{T}?, TModel, Action{TModel, T}, string?)"/>
    protected new bool SetProperty<TModel, T>(
            T? oldValue, T? newValue,
            TModel model,
            Action<TModel, T?> callback,
            [CallerMemberName] string? propertyName = null)
        => SetProperty(oldValue, newValue, comparer: null, model, callback, propertyName);

    /// <inheritdoc cref="ObservableObjectBase.SetProperty{TModel, T}(T, T, IEqualityComparer{T}?, TModel, Action{TModel, T}, string?)"/>
    protected new bool SetProperty<TModel, T>(
        T? oldValue, T? newValue,
        IEqualityComparer<T>? comparer,
        TModel model,
        Action<TModel, T?> callback,
        [CallerMemberName] string? propertyName = null)
    {
        if (comparer.DefaultIfNull().Equals(oldValue!, newValue!)) return false; // Property is not changing

        OnPropertyChanging(propertyName);
        SetUpAfterPropertyChanging(oldValue, propertyName);
        callback(model, newValue!);
        CleanUpBeforePropertyChanged(newValue, propertyName);
        OnPropertyChanged(propertyName);

        return true;
    }

    /// <summary>
    /// When overridden in a derived class, performs a set-up action immediately after the
    /// <see cref="ObservableObjectBase.PropertyChanging"/> is fired.
    /// </summary>
    /// <remarks>
    /// In order to honor the parent class's override of this method, classes should generally call the
    /// <see langword="base"/> method (although this is not necessary when directly
    /// extending <see cref="ObservableObject"/>).
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    /// <param name="oldValue"></param>
    protected virtual void SetUpAfterPropertyChanging<T>(T? oldValue,
                                                         [CallerMemberName] string? propertyName = null)
    { }

    /// <summary>
    /// When overridden in a derived class, performs a clean-up action immediately before the
    /// <see cref="ObservableObjectBase.PropertyChanged"/> is fired.
    /// </summary>
    /// <remarks>
    /// In order to honor the parent class's override of this method, classes should generally call the
    /// <see langword="base"/> method (although this is not necessary when directly
    /// extending <see cref="ObservableObject"/>).
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    /// <param name="newValue"></param>
    protected virtual void CleanUpBeforePropertyChanged<T>(T? newValue,
                                                           [CallerMemberName] string? propertyName = null)
    { }
}
