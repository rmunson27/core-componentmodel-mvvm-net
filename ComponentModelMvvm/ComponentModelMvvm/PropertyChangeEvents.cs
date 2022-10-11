using Rem.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Rem.Core.ComponentModel.Mvvm;

/// <summary>
/// Static helper functionality relating to property change events for a given type.
/// </summary>
/// <remarks>
/// The functionality in this class can be used to subscribe to, and unsubscribe from, property change events of
/// a given type when the type (and therefore the property change notifications supported by the type) may not be
/// known, for instance when designing a generic class.
/// </remarks>
public static class PropertyChangeEvents
{
    #region Notification Subscription And Unsubscription
    /// <summary>
    /// Subscribes to events of <paramref name="value"/> for any property change notifications supported by
    /// type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value to subscribe to.</typeparam>
    /// <param name="value">The value to subscribe to.</param>
    /// <param name="nestedPropertyChanging">
    /// The (optional) <see cref="NestedPropertyChangingEventHandler"/> to add to the
    /// <see cref="INotifyNestedPropertyChanging.NestedPropertyChanging"/> event.
    /// </param>
    /// <param name="propertyChanging">
    /// The (optional) <see cref="PropertyChangingEventHandler"/> to add to the
    /// <see cref="INotifyPropertyChanging.PropertyChanging"/> event.
    /// </param>
    /// <param name="nestedChangingOnly">
    /// Whether or not to ignore the <paramref name="propertyChanging"/> parameter if <typeparamref name="T"/>
    /// implements the <see cref="INotifyNestedPropertyChanging"/> interface.
    /// <para/>
    /// Defaults to <see langword="true"/>, as property changing notifications should cause a nested property changing
    /// notification to be fired if implementations are designed correctly.
    /// </param>
    /// <param name="nestedPropertyChanged">
    /// The (optional) <see cref="NestedPropertyChangedEventHandler"/> to add to the
    /// <see cref="INotifyNestedPropertyChanged.NestedPropertyChanged"/> event.
    /// </param>
    /// <param name="propertyChanged">
    /// The (optional) <see cref="PropertyChangedEventHandler"/> to add to the
    /// <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
    /// </param>
    /// <param name="nestedChangedOnly">
    /// Whether or not to ignore the <paramref name="propertyChanged"/> parameter if <typeparamref name="T"/>
    /// implements the <see cref="INotifyNestedPropertyChanged"/> interface.
    /// <para/>
    /// Defaults to <see langword="true"/>, as property changed notifications should cause a nested property changed
    /// notification to be fired if implementations are designed correctly.
    /// </param>
    public static void SubscribeTo<T>(
        T value,

        NestedPropertyChangingEventHandler? nestedPropertyChanging = null,
        PropertyChangingEventHandler? propertyChanging = null,
        bool nestedChangingOnly = true,

        NestedPropertyChangedEventHandler? nestedPropertyChanged = null,
        PropertyChangedEventHandler? propertyChanged = null,
        bool nestedChangedOnly = true)
    {
        // Handle property changing
        if (GetSupportedBy<T>().HasNotification(PropertyChangeNotifications.NestedPropertyChanging))
        {
            var notifier = CastToInterface<T, INotifyNestedPropertyChanging>(value);
            SubscribeToNestedPropertyChanging(notifier, nestedPropertyChanging);
            if (!nestedChangingOnly) SubscribeToPropertyChanging(notifier, propertyChanging);
        }
        else if (GetSupportedBy<T>().HasNotification(PropertyChangeNotifications.PropertyChanging))
        {
            SubscribeToPropertyChanging(CastToInterface<T, INotifyPropertyChanging>(value), propertyChanging);
        }

        // Handle property changed
        if (GetSupportedBy<T>().HasNotification(PropertyChangeNotifications.NestedPropertyChanged))
        {
            var notifier = CastToInterface<T, INotifyNestedPropertyChanged>(value);
            SubscribeToNestedPropertyChanged(notifier, nestedPropertyChanged);
            if (!nestedChangedOnly) SubscribeToPropertyChanged(notifier, propertyChanged);
        }
        else if (GetSupportedBy<T>().HasNotification(PropertyChangeNotifications.PropertyChanged))
        {
            SubscribeToPropertyChanged(CastToInterface<T, INotifyPropertyChanged>(value), propertyChanged);
        }
    }

    /// <summary>
    /// Unsubscribes from events of <paramref name="value"/> for any property change notifications supported by
    /// type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value to unsubscribe from.</typeparam>
    /// <param name="value">The value to unsubscribe from.</param>
    /// <param name="nestedPropertyChanging">
    /// The (optional) <see cref="NestedPropertyChangingEventHandler"/> to remove from the
    /// <see cref="INotifyNestedPropertyChanging.NestedPropertyChanging"/> event.
    /// </param>
    /// <param name="propertyChanging">
    /// The (optional) <see cref="PropertyChangingEventHandler"/> to remove from the
    /// <see cref="INotifyPropertyChanging.PropertyChanging"/> event.
    /// </param>
    /// <param name="nestedPropertyChanged">
    /// The (optional) <see cref="NestedPropertyChangedEventHandler"/> to remove from the
    /// <see cref="INotifyNestedPropertyChanged.NestedPropertyChanged"/> event.
    /// </param>
    /// <param name="propertyChanged">
    /// The (optional) <see cref="PropertyChangedEventHandler"/> to remove from the
    /// <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
    /// </param>
    public static void UnsubscribeFrom<T>(
        T value,

        NestedPropertyChangingEventHandler? nestedPropertyChanging = null,
        PropertyChangingEventHandler? propertyChanging = null,

        NestedPropertyChangedEventHandler? nestedPropertyChanged = null,
        PropertyChangedEventHandler? propertyChanged = null)
    {
        // Handle property changing
        if (GetSupportedBy<T>().HasNotification(PropertyChangeNotifications.NestedPropertyChanging))
        {
            var notifier = CastToInterface<T, INotifyNestedPropertyChanging>(value);
            UnsubscribeFromNestedPropertyChanging(notifier, nestedPropertyChanging);
            UnsubscribeFromPropertyChanging(notifier, propertyChanging);
        }
        else if (GetSupportedBy<T>().HasNotification(PropertyChangeNotifications.PropertyChanging))
        {
            UnsubscribeFromPropertyChanging(CastToInterface<T, INotifyPropertyChanging>(value), propertyChanging);
        }

        // Handle property changed
        if (GetSupportedBy<T>().HasNotification(PropertyChangeNotifications.NestedPropertyChanged))
        {
            var notifier = CastToInterface<T, INotifyNestedPropertyChanged>(value);
            UnsubscribeFromNestedPropertyChanged(notifier, nestedPropertyChanged);
            UnsubscribeFromPropertyChanged(notifier, propertyChanged);
        }
        else if (GetSupportedBy<T>().HasNotification(PropertyChangeNotifications.PropertyChanged))
        {
            UnsubscribeFromPropertyChanged(CastToInterface<T, INotifyPropertyChanged>(value), propertyChanged);
        }
    }

    /// <summary>
    /// Shuffles events of <paramref name="value"/> for any property change notifications supported by
    /// type <typeparamref name="T"/>, subscribing to new events and unsubscribing from old events.
    /// </summary>
    /// <typeparam name="T">The type of the value to shuffle events for.</typeparam>
    /// <param name="value">The value to shuffle events for.</param>
    /// <param name="oldNestedPropertyChanging">
    /// The (optional) old <see cref="NestedPropertyChangingEventHandler"/> to remove from the
    /// <see cref="INotifyNestedPropertyChanging.NestedPropertyChanging"/> event.
    /// </param>
    /// <param name="newNestedPropertyChanging">
    /// The (optional) new <see cref="NestedPropertyChangingEventHandler"/> to add to the
    /// <see cref="INotifyNestedPropertyChanging.NestedPropertyChanging"/> event.
    /// </param>
    /// <param name="oldPropertyChanging">
    /// The (optional) old <see cref="PropertyChangingEventHandler"/> to remove from the
    /// <see cref="INotifyPropertyChanging.PropertyChanging"/> event.
    /// </param>
    /// <param name="newPropertyChanging">
    /// The (optional) new <see cref="PropertyChangingEventHandler"/> to add to the
    /// <see cref="INotifyPropertyChanging.PropertyChanging"/> event.
    /// </param>
    /// <param name="nestedChangingOnly">
    /// Whether or not to ignore the <paramref name="newPropertyChanging"/> parameter if <typeparamref name="T"/>
    /// implements the <see cref="INotifyNestedPropertyChanging"/> interface.
    /// </param>
    /// <param name="oldNestedPropertyChanged">
    /// The (optional) old <see cref="NestedPropertyChangedEventHandler"/> to remove from the
    /// <see cref="INotifyNestedPropertyChanged.NestedPropertyChanged"/> event.
    /// </param>
    /// <param name="newNestedPropertyChanged">
    /// The (optional) new <see cref="NestedPropertyChangedEventHandler"/> to add to the
    /// <see cref="INotifyNestedPropertyChanged.NestedPropertyChanged"/> event.
    /// </param>
    /// <param name="oldPropertyChanged">
    /// The (optional) old <see cref="PropertyChangedEventHandler"/> to remove from the
    /// <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
    /// </param>
    /// <param name="newPropertyChanged">
    /// The (optional) new <see cref="PropertyChangedEventHandler"/> to add to the
    /// <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
    /// </param>
    /// <param name="nestedChangedOnly">
    /// Whether or not to ignore the <paramref name="newPropertyChanged"/> parameter if <typeparamref name="T"/>
    /// implements the <see cref="INotifyNestedPropertyChanged"/> interface.
    /// </param>
    public static void Shuffle<T>(
        T value,

        NestedPropertyChangingEventHandler? oldNestedPropertyChanging = null,
        NestedPropertyChangingEventHandler? newNestedPropertyChanging = null,
        PropertyChangingEventHandler? oldPropertyChanging = null,
        PropertyChangingEventHandler? newPropertyChanging = null,
        bool nestedChangingOnly = true,

        NestedPropertyChangedEventHandler? oldNestedPropertyChanged = null,
        NestedPropertyChangedEventHandler? newNestedPropertyChanged = null,
        PropertyChangedEventHandler? oldPropertyChanged = null,
        PropertyChangedEventHandler? newPropertyChanged = null,
        bool nestedChangedOnly = true)
    {
        // Handle property changing
        if (GetSupportedBy<T>().HasNotification(PropertyChangeNotifications.NestedPropertyChanging))
        {
            var notifier = CastToInterface<T, INotifyNestedPropertyChanging>(value);
            ShuffleNestedPropertyChanging(notifier, oldNestedPropertyChanging, newNestedPropertyChanging);
            if (!nestedChangingOnly) ShufflePropertyChanging(notifier, oldPropertyChanging, newPropertyChanging);
        }
        else if (GetSupportedBy<T>().HasNotification(PropertyChangeNotifications.PropertyChanging))
        {
            ShufflePropertyChanging(
                CastToInterface<T, INotifyPropertyChanging>(value), oldPropertyChanging, newPropertyChanging);
        }

        // Handle property changed
        if (GetSupportedBy<T>().HasNotification(PropertyChangeNotifications.NestedPropertyChanged))
        {
            var notifier = CastToInterface<T, INotifyNestedPropertyChanged>(value);
            ShuffleNestedPropertyChanged(notifier, oldNestedPropertyChanged, newNestedPropertyChanged);
            if (!nestedChangedOnly) ShufflePropertyChanged(notifier, oldPropertyChanged, newPropertyChanged);
        }
        else if (GetSupportedBy<T>().HasNotification(PropertyChangeNotifications.PropertyChanged))
        {
            ShufflePropertyChanged(
                CastToInterface<T, INotifyPropertyChanged>(value), oldPropertyChanged, newPropertyChanged);
        }
    }
    #endregion

    #region Metadata
    /// <summary>
    /// Gets a value describing the property change notifications supported by <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    [return: NameableEnum]
    public static PropertyChangeNotifications GetSupportedBy<T>() => PropertyChangeEvents<T>.SupportedNotifications;
    #endregion

    #region Helper Methods
    #region Nullable Notifier Helpers
    #region Subscription
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SubscribeToNestedPropertyChanging(
        INotifyNestedPropertyChanging? notifier, NestedPropertyChangingEventHandler? eventHandler)
    {
        if (notifier is not null)
        {
            notifier.NestedPropertyChanging += eventHandler;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SubscribeToNestedPropertyChanged(
        INotifyNestedPropertyChanged? notifier, NestedPropertyChangedEventHandler? eventHandler)
    {
        if (notifier is not null)
        {
            notifier.NestedPropertyChanged += eventHandler;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SubscribeToPropertyChanging(
        INotifyPropertyChanging? notifier, PropertyChangingEventHandler? eventHandler)
    {
        if (notifier is not null)
        {
            notifier.PropertyChanging += eventHandler;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SubscribeToPropertyChanged(
        INotifyPropertyChanged? notifier, PropertyChangedEventHandler? eventHandler)
    {
        if (notifier is not null)
        {
            notifier.PropertyChanged += eventHandler;
        }
    }
    #endregion

    #region Unsubscription
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void UnsubscribeFromNestedPropertyChanging(
        INotifyNestedPropertyChanging? notifier, NestedPropertyChangingEventHandler? eventHandler)
    {
        if (notifier is not null)
        {
            notifier.NestedPropertyChanging -= eventHandler;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void UnsubscribeFromNestedPropertyChanged(
        INotifyNestedPropertyChanged? notifier, NestedPropertyChangedEventHandler? eventHandler)
    {
        if (notifier is not null)
        {
            notifier.NestedPropertyChanged -= eventHandler;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void UnsubscribeFromPropertyChanging(
        INotifyPropertyChanging? notifier, PropertyChangingEventHandler? eventHandler)
    {
        if (notifier is not null)
        {
            notifier.PropertyChanging -= eventHandler;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void UnsubscribeFromPropertyChanged(
        INotifyPropertyChanged? notifier, PropertyChangedEventHandler? eventHandler)
    {
        if (notifier is not null)
        {
            notifier.PropertyChanged -= eventHandler;
        }
    }
    #endregion

    #region Shuffling (Both Subscription and Unsubscription)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ShuffleNestedPropertyChanging(
        INotifyNestedPropertyChanging? notifier,
        NestedPropertyChangingEventHandler? oldEventHandler,
        NestedPropertyChangingEventHandler? newEventHandler)
    {
        if (notifier is not null)
        {
            notifier.NestedPropertyChanging -= oldEventHandler;
            notifier.NestedPropertyChanging += newEventHandler;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ShuffleNestedPropertyChanged(
        INotifyNestedPropertyChanged? notifier,
        NestedPropertyChangedEventHandler? oldEventHandler,
        NestedPropertyChangedEventHandler? newEventHandler)
    {
        if (notifier is not null)
        {
            notifier.NestedPropertyChanged -= oldEventHandler;
            notifier.NestedPropertyChanged += newEventHandler;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ShufflePropertyChanging(
        INotifyPropertyChanging? notifier,
        PropertyChangingEventHandler? oldEventHandler,
        PropertyChangingEventHandler? newEventHandler)
    {
        if (notifier is not null)
        {
            notifier.PropertyChanging -= oldEventHandler;
            notifier.PropertyChanging += newEventHandler;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ShufflePropertyChanged(
        INotifyPropertyChanged? notifier,
        PropertyChangedEventHandler? oldEventHandler,
        PropertyChangedEventHandler? newEventHandler)
    {
        if (notifier is not null)
        {
            notifier.PropertyChanged -= oldEventHandler;
            notifier.PropertyChanged += newEventHandler;
        }
    }
    #endregion
    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TInterface? CastToInterface<T, TInterface>(T value) where TInterface : class
        => PropertyChangeEvents<T>.IsValueType ? (TInterface?)(object?)value : Unsafe.As<T, TInterface?>(ref value);
    #endregion
}

/// <summary>
/// Static helper functionality relating to property change events for a given type.
/// </summary>
/// <remarks>
/// This class contains statically loaded information about the generic type <typeparamref name="T"/> needed for the
/// functionality of the <see cref="PropertyChangeEvents"/> class.
/// </remarks>
/// <typeparam name="T">The type for which helper functionality is provided.</typeparam>
internal static class PropertyChangeEvents<T>
{
    #region Fields
    /// <summary>
    /// A value representing the property change notifications supported by type <typeparamref name="T"/>.
    /// </summary>
    [NameableEnum] public static readonly PropertyChangeNotifications SupportedNotifications;

    public static readonly bool IsValueType;
    #endregion

    #region Constructor
    static PropertyChangeEvents()
    {
        var type = typeof(T);
        IsValueType = type.IsValueType;

        // Determine which property change notifications are supported by the type and save on the class
        SupportedNotifications = PropertyChangeNotifications.None;

        if (typeof(INotifyPropertyChanging).IsAssignableFrom(type))
        {
            SupportedNotifications |= PropertyChangeNotifications.PropertyChanging;
        }
        if (typeof(INotifyPropertyChanged).IsAssignableFrom(type))
        {
            SupportedNotifications |= PropertyChangeNotifications.PropertyChanged;
        }
        if (typeof(INotifyNestedPropertyChanging).IsAssignableFrom(type))
        {
            SupportedNotifications |= PropertyChangeNotifications.NestedPropertyChanging;
        }
        if (typeof(INotifyNestedPropertyChanged).IsAssignableFrom(type))
        {
            SupportedNotifications |= PropertyChangeNotifications.NestedPropertyChanged;
        }
    }
    #endregion
}

/// <summary>
/// Extension methods for the <see cref="PropertyChangeNotifications"/> enum.
/// </summary>
public static class PropertyChangeNotificationsMethods
{
    /// <summary>
    /// Determines whether or not the current instance contains the notification passed in.
    /// </summary>
    /// <param name="p"></param>
    /// <param name="notification"></param>
    /// <returns></returns>
    public static bool HasNotification(this PropertyChangeNotifications p, PropertyChangeNotifications notification)
        => (p & notification) == notification;
}

/// <summary>
/// Represents the various types of property change notifications supported by a given type.
/// </summary>
[Flags]
public enum PropertyChangeNotifications : byte
{
    /// <summary>
    /// Indicates that the type implements no property change notifications.
    /// </summary>
    None = 0,

    /// <summary>
    /// Indicates that the type implements <see cref="INotifyPropertyChanging"/>.
    /// </summary>
    PropertyChanging = 1,

    /// <summary>
    /// Indicates that the type implements <see cref="INotifyPropertyChanged"/>.
    /// </summary>
    PropertyChanged = 2,

    /// <summary>
    /// Indicates that the type implements <see cref="INotifyNestedPropertyChanging"/>
    /// </summary>
    /// <remarks>
    /// This flag contains <see cref="PropertyChanging"/>, since <see cref="INotifyPropertyChanging"/> is extended
    /// by <see cref="INotifyNestedPropertyChanging"/>.
    /// </remarks>
    NestedPropertyChanging = PropertyChanging | 4,

    /// <summary>
    /// Indicates that the type implements <see cref="INotifyNestedPropertyChanged"/>.
    /// </summary>
    /// <remarks>
    /// This flag contains <see cref="PropertyChanged"/>, since <see cref="INotifyPropertyChanged"/> is extended
    /// by <see cref="INotifyNestedPropertyChanged"/>.
    /// </remarks>
    NestedPropertyChanged = PropertyChanged | 8,
}
