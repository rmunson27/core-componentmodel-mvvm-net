using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Rem.Core.ComponentModel.Mvvm;

/// <summary>
/// A <see cref="NestedObservableObject"/> subclass that caches its accessed event handlers by name in order to allow
/// the class to automatically create them on demand.
/// </summary>
/// <remarks>
/// This class can be useful for implementing <see cref="INotifyNestedPropertyChanging"/> and
/// <see cref="INotifyNestedPropertyChanged"/> when the class in question has a lot of nested properties.
/// Namely, it removes the need to define a separate property or method for each required handler manually.
/// </remarks>
public abstract class CachedHandlerNestedObservableObject : NestedObservableObject
{
    private Dictionary<string, HandlerBucket> Handlers { get; } = new();

    /// <summary>
    /// Constructs a new <see cref="CachedHandlerNestedObservableObject"/>.
    /// </summary>
    protected CachedHandlerNestedObservableObject() { }

    #region Event-Shuffling Helpers
    /// <summary>
    /// Sets an observable property of any type to a new value, performing all shuffling of internal events
    /// as necessary.
    /// </summary>
    /// <typeparam name="T">The type of property to set.</typeparam>
    /// <param name="field">A reference to the field being set.</param>
    /// <param name="newValue">The new value to set.</param>
    /// <param name="propertyName">
    /// The name of the property being set.
    /// This can be set to <see langword="null"/> when called from within a property or method to use the name of the
    /// property or method.
    /// </param>
    protected void SetObservableProperty<T>(
        [NotNullIfNotNull(nameof(newValue))] ref T? field, T? newValue, [CallerMemberName] string? propertyName = null)
    where T : INotifyPropertyChanging
    {
        if (field is null && newValue is null) return;

        var (nestedChanging, changing, nestedChanged, changed) = GetHandlersForType<T>(propertyName);
        OnPropertyChanging(propertyName);
        PropertyChangeEvents.UnsubscribeFrom(field, nestedChanging, changing, nestedChanged, changed);
        field = newValue;
        PropertyChangeEvents.SubscribeTo(field,
                                         nestedChanging, changing, nestedChangingOnly: true,
                                         nestedChanged, changed, nestedChangedOnly: true);
        OnPropertyChanged(propertyName);
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

        var (nestedChanging, changing, nestedChanged, changed) = GetHandlersForType<T>(propertyName);
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

        var (nestedChanging, changing, nestedChanged, changed) = GetHandlersForType<T>(propertyName);
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

        var (nestedChanging, changing, nestedChanged, changed) = GetHandlersForType<T>(propertyName);
        PropertyChangeEvents.UnsubscribeFrom(oldValue, nestedChanging, changing, nestedChanged, changed);
        PropertyChangeEvents.SubscribeTo(newValue,
                                         nestedChanging, changing, nestedChangingOnly: true,
                                         nestedChanged, changed, nestedChangedOnly: true);
    }

    private HandlerBucket GetHandlersForType<T>([CallerMemberName] string? propertyName = null)
    {
        PropertyChangingEventHandler? changing = null;
        PropertyChangedEventHandler? changed = null;
        NestedPropertyChangingEventHandler? nestedChanging = null;
        NestedPropertyChangedEventHandler? nestedChanged = null;

        var supportedEvents = PropertyChangeEvents.GetSupportedBy<T>();

        if (supportedEvents.HasNotification(PropertyChangeNotifications.NestedPropertyChanging))
        {
            nestedChanging = NestedChangingHandlerFor(propertyName);
        }
        else if (supportedEvents.HasNotification(PropertyChangeNotifications.PropertyChanging))
        {
            changing = ChangingHandlerFor(propertyName);
        }

        if (supportedEvents.HasNotification(PropertyChangeNotifications.NestedPropertyChanged))
        {
            nestedChanged = NestedChangedHandlerFor(propertyName);
        }
        else if (supportedEvents.HasNotification(PropertyChangeNotifications.PropertyChanged))
        {
            changed = ChangedHandlerFor(propertyName);
        }

        return new()
        {
            Changed = changed,
            Changing = changing,
            NestedChanged = nestedChanged,
            NestedChanging = nestedChanging
        };
    }
    #endregion

    #region Handler Getters
    /// <summary>
    /// Gets the existing <see cref="PropertyChangingEventHandler"/> for the property with the given name or
    /// creates it if it does not exist.
    /// </summary>
    /// <param name="propertyName"></param>
    /// <returns></returns>
    protected PropertyChangingEventHandler ChangingHandlerFor([CallerMemberName] string? propertyName = null)
    {
        if (Handlers.TryGetValue(propertyName!, out var bucket))
        {
            if (bucket.Changing is PropertyChangingEventHandler handler) return handler;
            else
            {
                handler = CreateChangingHandler(propertyName);
                Handlers[propertyName!] = bucket with { Changing = handler };
                return handler;
            }
        }
        else
        {
            var handler = CreateChangingHandler(propertyName);
            Handlers[propertyName!] = new() { Changing = handler };
            return handler;
        }
    }

    /// <summary>
    /// Gets the existing <see cref="PropertyChangedEventHandler"/> for the property with the given name or
    /// creates it if it does not exist.
    /// </summary>
    /// <param name="propertyName"></param>
    /// <returns></returns>
    protected PropertyChangedEventHandler ChangedHandlerFor([CallerMemberName] string? propertyName = null)
    {
        if (Handlers.TryGetValue(propertyName!, out var bucket))
        {
            if (bucket.Changed is PropertyChangedEventHandler handler) return handler;
            else
            {
                handler = CreateChangedHandler(propertyName);
                Handlers[propertyName!] = bucket with { Changed = handler };
                return handler;
            }
        }
        else
        {
            var handler = CreateChangedHandler(propertyName);
            Handlers[propertyName!] = new() { Changed = handler };
            return handler;
        }
    }

    /// <summary>
    /// Gets the existing <see cref="PropertyChangingEventHandler"/> for the property with the given name or
    /// creates it if it does not exist.
    /// </summary>
    /// <param name="propertyName"></param>
    /// <returns></returns>
    protected NestedPropertyChangingEventHandler NestedChangingHandlerFor(
        [CallerMemberName] string? propertyName = null)
    {
        if (Handlers.TryGetValue(propertyName!, out var bucket))
        {
            if (bucket.NestedChanging is NestedPropertyChangingEventHandler handler) return handler;
            else
            {
                handler = CreateNestedChangingHandler(propertyName);
                Handlers[propertyName!] = bucket with { NestedChanging = handler };
                return handler;
            }
        }
        else
        {
            var handler = CreateNestedChangingHandler(propertyName);
            Handlers[propertyName!] = new() { NestedChanging = handler };
            return handler;
        }
    }

    /// <summary>
    /// Gets the existing <see cref="NestedPropertyChangedEventHandler"/> for the property with the given name or
    /// creates it if it does not exist.
    /// </summary>
    /// <param name="propertyName"></param>
    /// <returns></returns>
    protected NestedPropertyChangedEventHandler NestedChangedHandlerFor([CallerMemberName] string? propertyName = null)
    {
        if (Handlers.TryGetValue(propertyName!, out var bucket))
        {
            if (bucket.NestedChanged is NestedPropertyChangedEventHandler handler) return handler;
            else
            {
                handler = CreateNestedChangedHandler(propertyName);
                Handlers[propertyName!] = bucket with { NestedChanged = handler };
                return handler;
            }
        }
        else
        {
            var handler = CreateNestedChangedHandler(propertyName);
            Handlers[propertyName!] = new() { NestedChanged = handler };
            return handler;
        }
    }
    #endregion

    private readonly struct HandlerBucket
    {
        public PropertyChangingEventHandler? Changing { get; init; }
        public PropertyChangedEventHandler? Changed { get; init; }
        public NestedPropertyChangingEventHandler? NestedChanging { get; init; }
        public NestedPropertyChangedEventHandler? NestedChanged { get; init; }

        public void Deconstruct(out NestedPropertyChangingEventHandler? NestedChanging,
                                out PropertyChangingEventHandler? Changing,
                                out NestedPropertyChangedEventHandler? NestedChanged,
                                out PropertyChangedEventHandler? Changed)
        {
            NestedChanging = this.NestedChanging;
            NestedChanged = this.NestedChanged;
            Changing = this.Changing;
            Changed = this.Changed;
        }
    }

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
}
