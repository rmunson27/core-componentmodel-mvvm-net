using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Rem.Core.ComponentModel.Mvvm;

/// <summary>
/// A base class for objects that notify of nested property changes.
/// </summary>
/// <remarks>
/// This class can be used as a base in lieu of <see cref="NestedObservableObject"/> if there is no need for extending
/// classes to have control over setting properties based on which specific property change notifications they offer.
/// The class may simplify things in such cases, since all nested property change notification subscriptions will be
/// handled automatically by a simple call to <see cref="CachedNestedObservableObject.SetProperty"/>.
/// <para/>
/// This class also can be useful for implementing <see cref="INotifyNestedPropertyChanging"/> and
/// <see cref="INotifyNestedPropertyChanged"/> when the class in question has a lot of nested properties.
/// Namely, it removes the need to define a separate property or method for each required handler manually.
/// </remarks>
public abstract class CachedNestedObservableObject : ObservableObject,
                                                     INotifyNestedPropertyChanging, INotifyNestedPropertyChanged
{
    /// <summary>
    /// Stores the property change handlers for this type.
    /// </summary>
    protected readonly PropertyChangeEventHandlerCache _cache = new();

    /// <summary>
    /// An event that occurs when a nested property is changing.
    /// </summary>
    public event NestedPropertyChangingEventHandler? NestedPropertyChanging;

    /// <summary>
    /// An event that occurs when a nested property has changed.
    /// </summary>
    public event NestedPropertyChangedEventHandler? NestedPropertyChanged;

    /// <summary>
    /// Constructs a new <see cref="CachedHandlerNestedObservableObject"/>.
    /// </summary>
    protected CachedNestedObservableObject()
    {
        // Ensure that nested property changes are fired whenever property changes are
        PropertyChanging += This_PropertyChanging;
        PropertyChanged += This_PropertyChanged;
    }

    #region Event Handlers
    /// <summary>
    /// Fires the <see cref="NestedPropertyChanging"/> event whenever the
    /// <see cref="ObservableObject.PropertyChanging"/> event is triggered (so long as the property name wrapped in
    /// the event arguments is not <see langword="null"/>).
    /// </summary>
    /// <param name="_"></param>
    /// <param name="e"></param>
    private void This_PropertyChanging(object? _, PropertyChangingEventArgs e)
    {
        if (e.PropertyName is not null) OnNestedPropertyChanging(new(e.PropertyName));
    }

    /// <summary>
    /// Fires the <see cref="NestedPropertyChanged"/> event whenever the <see cref="ObservableObject.PropertyChanged"/>
    /// event is triggered (so long as the property name wrapped in the event arguments is not <see langword="null"/>).
    /// </summary>
    /// <param name="_"></param>
    /// <param name="e"></param>
    private void This_PropertyChanged(object? _, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not null) OnNestedPropertyChanged(new(e.PropertyName));
    }
    #endregion

    #region Event-Shuffling
    /// <inheritdoc/>
    protected override void SetUpAfterPropertyChanging<T>(T? oldValue, [CallerMemberName] string? propertyName = null)
    where T : default
    {
        base.SetUpAfterPropertyChanging(oldValue);
        UnsubscribeFromChanges(oldValue, propertyName);
    }

    /// <inheritdoc/>
    protected override void CleanUpBeforePropertyChanged<T>(T? newValue, [CallerMemberName] string? propertyName = null)
    where T : default
    {
        base.CleanUpBeforePropertyChanged(newValue, propertyName);
        SubscribeToChanges(newValue, propertyName);
    }

    /// <summary>
    /// Unsubscribes from relevant change events to the value of the property with the given name.
    /// </summary>
    /// <remarks>
    /// This method will not change any properties, merely unsubscribe from relevant <paramref name="value"/> events.
    /// </remarks>
    /// <typeparam name="T">The type of property being unsubscribed from.</typeparam>
    /// <param name="value">The value to unsubscribe from.</param>
    /// <param name="propertyName">
    /// The name of the property whose events are being unsubscribed from, or <see langword="null"/> to use the caller
    /// member name.
    /// </param>
    protected void UnsubscribeFromChanges<T>(T? value, [CallerMemberName] string? propertyName = null)
    {
        if (value is null) return;

        GetCachedHandlersForType<T>(out var nestedChanging, out var changing,
                                    out var nestedChanged, out var changed,
                                    propertyName);
        PropertyChangeEvents.UnsubscribeFrom(value, nestedChanging, changing, nestedChanged, changed);
    }

    /// <summary>
    /// Subscribes to relevant change events to the value of the property with the given name.
    /// </summary>
    /// <remarks>
    /// This method will not change any properties, merely subscribe to relevant <paramref name="value"/> events.
    /// </remarks>
    /// <typeparam name="T">The type of property being subscribed to.</typeparam>
    /// <param name="value">The value to subscribe to.</param>
    /// <param name="propertyName">
    /// The name of the property whose events are being subscribed to, or <see langword="null"/> to use the caller
    /// member name.
    /// </param>
    protected void SubscribeToChanges<T>(T? value, [CallerMemberName] string? propertyName = null)
    {
        if (value is null) return;

        GetCachedHandlersForType<T>(out var nestedChanging, out var changing,
                                    out var nestedChanged, out var changed,
                                    propertyName);

        PropertyChangeEvents.SubscribeTo(value,
                                         nestedChanging, changing, nestedChangingOnly: true,
                                         nestedChanged, changed, nestedChangedOnly: true);
    }

    /// <summary>
    /// Shuffles relevant change events from the old value of the property with the given name to the new value.
    /// </summary>
    /// <remarks>
    /// This method will not change any properties, merely unsubscribe from relevant <paramref name="oldValue"/>
    /// events and subscribe to relevant <paramref name="newValue"/> events.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    /// <param name="oldValue">The value to unsubscribe from.</param>
    /// <param name="newValue">The value to subscribe to.</param>
    /// <param name="propertyName">
    /// The name of the property events are being shuffled for, or <see langword="null"/> to use the caller
    /// member name.
    /// </param>
    protected void ShuffleChanges<T>(T? oldValue, T? newValue, [CallerMemberName] string? propertyName = null)
    {
        if (oldValue is null && newValue is null) return;

        GetCachedHandlersForType<T>(out var nestedChanging, out var changing,
                                    out var nestedChanged, out var changed,
                                    propertyName);
        PropertyChangeEvents.UnsubscribeFrom(oldValue, nestedChanging, changing, nestedChanged, changed);
        PropertyChangeEvents.SubscribeTo(newValue,
                                         nestedChanging, changing, nestedChangingOnly: true,
                                         nestedChanged, changed, nestedChangedOnly: true);
    }

    /// <summary>
    /// Gets the cached handlers for the property with the specified name, adding them to the cache if they do
    /// not exist.
    /// </summary>
    /// <remarks>
    /// This will never add handlers for types that do not implement any property change notifications (in which case
    /// all <see langword="out"/> parameters will be set to <see langword="null"/>).
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    /// <param name="nestedChanging"></param>
    /// <param name="changing"></param>
    /// <param name="nestedChanged"></param>
    /// <param name="changed"></param>
    /// <param name="propertyName"></param>
    private void GetCachedHandlersForType<T>(out NestedPropertyChangingEventHandler? nestedChanging,
                                             out PropertyChangingEventHandler? changing,
                                             out NestedPropertyChangedEventHandler? nestedChanged,
                                             out PropertyChangedEventHandler? changed,

                                             [CallerMemberName] string? propertyName = null)
    {
        if (PropertyChangeEvents.GetSupportedBy<T>() == PropertyChangeNotifications.None)
        {
            nestedChanging = null;
            changing = null;
            nestedChanged = null;
            changed = null;
        }
        else if (!_cache.TryGetHandlersForProperty<T>(out nestedChanging, out changing,
                                                      out nestedChanged, out changed,
                                                      propertyName))
        {
            CreateHandlersForType<T>(out nestedChanging, out changing,
                                     out nestedChanged, out changed,
                                     propertyName);
            _cache.AddProperty<T>(nestedChanging, changing, nestedChanged, changed, propertyName);
        }
    }

    /// <summary>
    /// Creates new handlers for type <typeparamref name="T"/> with the given property name in
    /// <see langword="out"/> parameters.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="nestedChanging"></param>
    /// <param name="changing"></param>
    /// <param name="nestedChanged"></param>
    /// <param name="changed"></param>
    /// <param name="propertyName"></param>
    private void CreateHandlersForType<T>(out NestedPropertyChangingEventHandler? nestedChanging,
                                          out PropertyChangingEventHandler? changing,
                                          out NestedPropertyChangedEventHandler? nestedChanged,
                                          out PropertyChangedEventHandler? changed,
                                          [CallerMemberName] string? propertyName = null)
    {
        changing = null;
        changed = null;
        nestedChanging = null;
        nestedChanged = null;

        var supportedEvents = PropertyChangeEvents.GetSupportedBy<T>();

        if (supportedEvents.HasNotification(PropertyChangeNotifications.NestedPropertyChanging))
        {
            nestedChanging = CreateNestedChangingHandler(propertyName);
        }
        else if (supportedEvents.HasNotification(PropertyChangeNotifications.PropertyChanging))
        {
            changing = CreateChangingHandler(propertyName);
        }

        if (supportedEvents.HasNotification(PropertyChangeNotifications.NestedPropertyChanged))
        {
            nestedChanged = CreateNestedChangedHandler(propertyName);
        }
        else if (supportedEvents.HasNotification(PropertyChangeNotifications.PropertyChanged))
        {
            changed = CreateChangedHandler(propertyName);
        }
    }
    #endregion

    #region HandlerFactories
    /// <summary>
    /// Creates a handler that handles property changing events of a property with the given name.
    /// </summary>
    /// <remarks>
    /// This allows an appropriate nested property changing notification to be triggered.
    /// </remarks>
    /// <param name="propertyName">The name of the property to create a handler for.</param>
    /// <returns>
    /// A <see cref="PropertyChangingEventHandler"/> that handles property changing events of a property with
    /// name <paramref name="propertyName"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PropertyChangingEventHandler CreateChangingHandler([CallerMemberName] string? propertyName = null)
    {
        return (sender, e) => OnChildPropertyChanging(propertyName!, e);
    }

    /// <summary>
    /// Creates a handler that handles property changed events of a property with the given name.
    /// </summary>
    /// <remarks>
    /// This allows an appropriate nested property changed notification to be triggered.
    /// </remarks>
    /// <param name="propertyName">The name of the property to create a handler for.</param>
    /// <returns>
    /// A <see cref="PropertyChangedEventHandler"/> that handles property changed events of a property with
    /// name <paramref name="propertyName"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PropertyChangedEventHandler CreateChangedHandler([CallerMemberName] string? propertyName = null)
    {
        return (sender, e) => OnChildPropertyChanged(propertyName!, e);
    }

    /// <summary>
    /// Creates a handler that handles nested property changing events of a property with the given name.
    /// </summary>
    /// <remarks>
    /// This allows an appropriate nested property changing notification to be triggered.
    /// </remarks>
    /// <param name="propertyName">The name of the property to create a handler for.</param>
    /// <returns>
    /// A <see cref="NestedPropertyChangingEventHandler"/> that handles nested property changing events of a property
    /// with name <paramref name="propertyName"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private NestedPropertyChangingEventHandler CreateNestedChangingHandler(
        [CallerMemberName] string? propertyName = null)
    {
        return (sender, e) => OnChildNestedPropertyChanging(propertyName!, e);
    }

    /// <summary>
    /// Creates a handler that handles nested property changed events of a property with the given name.
    /// </summary>
    /// <remarks>
    /// This allows an appropriate nested property changed notification to be triggered.
    /// </remarks>
    /// <param name="propertyName">The name of the property to create a handler for.</param>
    /// <returns>
    /// A <see cref="NestedPropertyChangedEventHandler"/> that handles nested property changed events of a property
    /// with name <paramref name="propertyName"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private NestedPropertyChangedEventHandler CreateNestedChangedHandler(
        [CallerMemberName] string? propertyName = null)
    {
        return (sender, e) => OnChildNestedPropertyChanged(propertyName!, e);
    }
    #endregion

    #region Event Handler Building Helpers
    /// <summary>
    /// Raises the <see cref="NestedPropertyChanging"/> event with arguments created by adding the child property name
    /// to the event arguments passed in.
    /// </summary>
    /// <param name="childPropertyName"></param>
    /// <param name="e"></param>
    protected void OnChildNestedPropertyChanging(string childPropertyName, NestedPropertyChangingEventArgs e)
    {
        OnNestedPropertyChanging(new(e.PropertyPath.Push(childPropertyName)));
    }

    /// <summary>
    /// Raises the <see cref="NestedPropertyChanging"/> event with arguments created by adding the child property
    /// name to the event arguments passed in.
    /// </summary>
    /// <param name="childPropertyName"></param>
    /// <param name="e"></param>
    protected void OnChildPropertyChanging(string childPropertyName, PropertyChangingEventArgs e)
    {
        if (e.PropertyName is not null)
        {
            OnNestedPropertyChanging(new(ImmutableStack.Create(e.PropertyName).Push(childPropertyName)));
        }
    }

    /// <summary>
    /// Raises the <see cref="NestedPropertyChanged"/> event with arguments created by adding the child property name
    /// to the event arguments passed in.
    /// </summary>
    /// <param name="childPropertyName"></param>
    /// <param name="e"></param>
    protected void OnChildNestedPropertyChanged(string childPropertyName, NestedPropertyChangedEventArgs e)
    {
        OnNestedPropertyChanged(new(e.PropertyPath.Push(childPropertyName)));
    }

    /// <summary>
    /// Raises the <see cref="NestedPropertyChanged"/> event with arguments created by adding the child property name
    /// to the event arguments passed in.
    /// </summary>
    /// <param name="childPropertyName"></param>
    /// <param name="e"></param>
    protected void OnChildPropertyChanged(string childPropertyName, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not null)
        {
            OnNestedPropertyChanged(new(ImmutableStack.Create(e.PropertyName).Push(childPropertyName)));
        }
    }
    #endregion

    #region Event Triggers
    /// <summary>
    /// Raises the <see cref="NestedPropertyChanging"/> event.
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnNestedPropertyChanging(NestedPropertyChangingEventArgs e)
    {
        NestedPropertyChanging?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the <see cref="NestedPropertyChanged"/> event.
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnNestedPropertyChanged(NestedPropertyChangedEventArgs e)
    {
        NestedPropertyChanged?.Invoke(this, e);
    }
    #endregion
}
