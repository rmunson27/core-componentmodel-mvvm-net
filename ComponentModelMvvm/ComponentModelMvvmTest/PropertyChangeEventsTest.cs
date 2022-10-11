using Rem.Core.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemTest.Core.ComponentModel.Mvvm;

#region Test Class
/// <summary>
/// Tests the <see cref="PropertyChangeEvents{T}"/> class.
/// </summary>
[TestClass]
public class PropertyChangeEventsTest
{
    #region Tests
    #region Metadata
    /// <summary>
    /// Tests the <see cref="PropertyChangeEvents.GetSupportedBy{T}"/> method.
    /// </summary>
    [TestMethod]
    public void TestGetSupportedBy()
    {
        Assert.AreEqual(PropertyChangeNotifications.None, PropertyChangeEvents.GetSupportedBy<NonNotifier>());

        Assert.AreEqual(
            PropertyChangeNotifications.PropertyChanging, PropertyChangeEvents.GetSupportedBy<ChangingOnlyNotifier>());
        Assert.AreEqual(
            PropertyChangeNotifications.PropertyChanged, PropertyChangeEvents.GetSupportedBy<ChangedOnlyNotifier>());

        Assert.AreEqual(
            PropertyChangeNotifications.PropertyChanging | PropertyChangeNotifications.PropertyChanged,
            PropertyChangeEvents.GetSupportedBy<ChangeNotifier>());

        Assert.AreEqual(
            PropertyChangeNotifications.NestedPropertyChanging,
            PropertyChangeEvents.GetSupportedBy<NestedChangingOnlyNotifier>());
        Assert.AreEqual(
            PropertyChangeNotifications.NestedPropertyChanged,
            PropertyChangeEvents.GetSupportedBy<NestedChangedOnlyNotifier>());

        Assert.AreEqual(
            PropertyChangeNotifications.NestedPropertyChanging | PropertyChangeNotifications.PropertyChanged,
            PropertyChangeEvents.GetSupportedBy<NestedChangingAndChangedNotifier>());
        Assert.AreEqual(
            PropertyChangeNotifications.PropertyChanging | PropertyChangeNotifications.NestedPropertyChanged,
            PropertyChangeEvents.GetSupportedBy<NestedChangedAndChangingNotifier>());

        Assert.AreEqual(
            PropertyChangeNotifications.NestedPropertyChanging | PropertyChangeNotifications.NestedPropertyChanged,
            PropertyChangeEvents.GetSupportedBy<NestedChangeNotifier>());
    }
    #endregion

    #region Non-Nested
    /// <summary>
    /// Tests the <see cref="PropertyChangeEvents"/> subscription, unsubscription and shuffling behavior for objects
    /// that do not implement any property change notifications.
    /// </summary>
    [TestMethod]
    public void TestNonNotifier() => RunSubscriptionTest(new NonNotifier(), PropertyChangeEventFlags.None);

    /// <summary>
    /// Tests the <see cref="PropertyChangeEvents"/> subscription, unsubscription and shuffling behavior for objects
    /// that implement only non-nested property changing notifications.
    /// </summary>
    [TestMethod]
    public void TestChangingOnly()
        => RunSubscriptionTest(new ChangingOnlyNotifier(), PropertyChangeEventFlags.PropertyChanging);

    /// <summary>
    /// Tests the <see cref="PropertyChangeEvents"/> subscription, unsubscription and shuffling behavior for objects
    /// that implement only non-nested property changed notifications.
    /// </summary>
    [TestMethod]
    public void TestChangedOnly()
        => RunSubscriptionTest(new ChangedOnlyNotifier(), PropertyChangeEventFlags.PropertyChanged);

    /// <summary>
    /// Tests the <see cref="PropertyChangeEvents"/> subscription, unsubscription and shuffling behavior for objects
    /// that implement both non-nested property change notifications.
    /// </summary>
    [TestMethod]
    public void TestChange()
        => RunSubscriptionTest(
            new ChangeNotifier(),
            PropertyChangeEventFlags.PropertyChanging | PropertyChangeEventFlags.PropertyChanged);
    #endregion

    #region Nested
    /// <summary>
    /// Tests the <see cref="PropertyChangeEvents"/> subscription, unsubscription and shuffling behavior for objects
    /// that implement only nested and non-nested property changed notifications.
    /// </summary>
    [TestMethod]
    public void TestNestedChangedOnly()
        => RunNestedTest(
            new NestedChangedOnlyNotifier(),
            PropertyChangeEventFlags.PropertyChanged | PropertyChangeEventFlags.NestedPropertyChanged);

    /// <summary>
    /// Tests the <see cref="PropertyChangeEvents"/> subscription, unsubscription and shuffling behavior for objects
    /// that implement only nested and non-nested property changing notifications.
    /// </summary>
    [TestMethod]
    public void TestNestedChangingOnly()
        => RunNestedTest(
            new NestedChangingOnlyNotifier(),
            PropertyChangeEventFlags.PropertyChanging | PropertyChangeEventFlags.NestedPropertyChanging);

    /// <summary>
    /// Tests the <see cref="PropertyChangeEvents"/> subscription, unsubscription and shuffling behavior for objects
    /// that implement only nested and non-nested property changing and non-nested property changed notifications.
    /// </summary>
    [TestMethod]
    public void TestNestedChangingAndChangedOnly()
        => RunNestedTest(
            new NestedChangingAndChangedNotifier(),
            PropertyChangeEventFlags.PropertyChanging | PropertyChangeEventFlags.PropertyChanged
                | PropertyChangeEventFlags.NestedPropertyChanging);

    /// <summary>
    /// Tests the <see cref="PropertyChangeEvents"/> subscription, unsubscription and shuffling behavior for objects
    /// that implement only nested and non-nested property changed and non-nested property changing notifications.
    /// </summary>
    [TestMethod]
    public void TestNestedChangedAndChangingOnly()
        => RunNestedTest(
            new NestedChangedAndChangingNotifier(),
            PropertyChangeEventFlags.PropertyChanged | PropertyChangeEventFlags.PropertyChanging
                | PropertyChangeEventFlags.NestedPropertyChanged);

    /// <summary>
    /// Tests the <see cref="PropertyChangeEvents"/> subscription, unsubscription and shuffling behavior for objects
    /// that implement all available nested and non-nested property change notifications.
    /// </summary>
    [TestMethod]
    public void TestNestedChange()
        => RunNestedTest(
            new NestedChangeNotifier(),
            PropertyChangeEventFlags.PropertyChanged | PropertyChangeEventFlags.PropertyChanging
                | PropertyChangeEventFlags.NestedPropertyChanged | PropertyChangeEventFlags.NestedPropertyChanging);

    private static void RunNestedTest<TNotifier>(TNotifier notifier, PropertyChangeEventFlags expectedFlags)
        where TNotifier : TestNotifier
    {
        // Run tests for each possible option
        RunSubscriptionTest(notifier, expectedFlags, ChangeSubscriptionOption.AllSupported);
        RunSubscriptionTest(notifier, expectedFlags, ChangeSubscriptionOption.NestedChangingOnly);
        RunSubscriptionTest(notifier, expectedFlags, ChangeSubscriptionOption.NestedChangedOnly);
        RunSubscriptionTest(notifier, expectedFlags, ChangeSubscriptionOption.NestedChangeOnly);
    }
    #endregion
    #endregion

    #region Helpers
    #region Methods
    private static void RunSubscriptionTest<TNotifier>(
        TNotifier notifier, PropertyChangeEventFlags expectedFlags,
        ChangeSubscriptionOption subscriptionOption = ChangeSubscriptionOption.NestedChangeOnly)
        where TNotifier : TestNotifier
    {
        var notifications = new TestNotifications();
        var otherNotifications = new TestNotifications();
        expectedFlags = subscriptionOption.ApplyTo(expectedFlags);

        notifier.B = false; // Make sure the notifier starts in a default state (just for consistency)

        // Subscribe
        notifications.SubscribeTo(notifier, subscriptionOption);
        notifier.B = true;
        Assert.AreEqual(expectedFlags, notifications.FiredFlags);

        // Shuffle
        notifications.ResetFiredFlags();
        notifications.ShuffleToOther(notifier, otherNotifications, subscriptionOption);
        notifier.B = false;
        Assert.AreEqual(PropertyChangeEventFlags.None, notifications.FiredFlags);
        Assert.AreEqual(expectedFlags, otherNotifications.FiredFlags);

        // Unsubscribe
        otherNotifications.ResetFiredFlags();
        otherNotifications.UnsubscribeFrom(notifier);
        notifier.B = true;
        Assert.AreEqual(PropertyChangeEventFlags.None, otherNotifications.FiredFlags);
    }
    #endregion

    #region Types
    #region Test Notifiers
    private sealed class NonNotifier : TestNotifier
    {
        public override bool B { get; set; }
    }

    private sealed class ChangingOnlyNotifier : TestNotifier, INotifyPropertyChanging
    {
        public override bool B
        {
            get => _b;
            set
            {
                PropertyChanging?.Invoke(this, new(nameof(B)));
                _b = value;
            }
        }
        private bool _b;

        public event PropertyChangingEventHandler? PropertyChanging;
    }

    private sealed class ChangedOnlyNotifier : TestNotifier, INotifyPropertyChanged
    {
        public override bool B
        {
            get => _b;
            set
            {
                _b = value;
                PropertyChanged?.Invoke(this, new(nameof(B)));
            }
        }
        private bool _b;

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private sealed class ChangeNotifier : TestNotifier, INotifyPropertyChanging, INotifyPropertyChanged
    {
        public override bool B
        {
            get => _b;
            set
            {
                PropertyChanging?.Invoke(this, new(nameof(B)));
                _b = value;
                PropertyChanged?.Invoke(this, new(nameof(B)));
            }
        }
        private bool _b;

        public event PropertyChangingEventHandler? PropertyChanging;
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private sealed class NestedChangingOnlyNotifier : TestNotifier, INotifyNestedPropertyChanging
    {
        public override bool B
        {
            get => _b;
            set
            {
                NestedPropertyChanging?.Invoke(this, new(nameof(B)));
                PropertyChanging?.Invoke(this, new(nameof(B)));
                _b = value;
            }
        }
        private bool _b;

        public event NestedPropertyChangingEventHandler? NestedPropertyChanging;
        public event PropertyChangingEventHandler? PropertyChanging;
    }

    private sealed class NestedChangedOnlyNotifier : TestNotifier, INotifyNestedPropertyChanged
    {
        public override bool B
        {
            get => _b;
            set
            {
                _b = value;
                NestedPropertyChanged?.Invoke(this, new(nameof(B)));
                PropertyChanged?.Invoke(this, new(nameof(B)));
            }
        }
        private bool _b;

        public event NestedPropertyChangedEventHandler? NestedPropertyChanged;
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private sealed class NestedChangingAndChangedNotifier
        : TestNotifier, INotifyNestedPropertyChanging, INotifyPropertyChanged
    {
        public override bool B
        {
            get => _b;
            set
            {
                NestedPropertyChanging?.Invoke(this, new(nameof(B)));
                PropertyChanging?.Invoke(this, new(nameof(B)));
                _b = value;
                PropertyChanged?.Invoke(this, new(nameof(B)));
            }
        }
        private bool _b;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event NestedPropertyChangingEventHandler? NestedPropertyChanging;
        public event PropertyChangingEventHandler? PropertyChanging;
    }

    private sealed class NestedChangedAndChangingNotifier
        : TestNotifier, INotifyPropertyChanging, INotifyNestedPropertyChanged
    {
        public override bool B
        {
            get => _b;
            set
            {
                PropertyChanging?.Invoke(this, new(nameof(B)));
                _b = value;
                NestedPropertyChanged?.Invoke(this, new(nameof(B)));
                PropertyChanged?.Invoke(this, new(nameof(B)));
            }
        }
        private bool _b;

        public event NestedPropertyChangedEventHandler? NestedPropertyChanged;
        public event PropertyChangedEventHandler? PropertyChanged;
        public event PropertyChangingEventHandler? PropertyChanging;
    }

    private sealed class NestedChangeNotifier
        : TestNotifier, INotifyNestedPropertyChanging, INotifyNestedPropertyChanged
    {
        public override bool B
        {
            get => _b;
            set
            {
                NestedPropertyChanging?.Invoke(this, new(nameof(B)));
                PropertyChanging?.Invoke(this, new(nameof(B)));
                _b = value;
                NestedPropertyChanged?.Invoke(this, new(nameof(B)));
                PropertyChanged?.Invoke(this, new(nameof(B)));
            }
        }
        private bool _b;

        public event NestedPropertyChangedEventHandler? NestedPropertyChanged;
        public event PropertyChangedEventHandler? PropertyChanged;
        public event NestedPropertyChangingEventHandler? NestedPropertyChanging;
        public event PropertyChangingEventHandler? PropertyChanging;
    }

    private abstract class TestNotifier
    {
        public abstract bool B { get; set; }
    }
    #endregion

    #region Test Notifications
    private sealed class TestNotifications
    {
        /// <summary>
        /// Stores flags indicating which events were fired.
        /// </summary>
        public PropertyChangeEventFlags FiredFlags { get; private set; }

        public void PropertyChangingEventHandler(object? sender, PropertyChangingEventArgs e)
        {
            FiredFlags |= PropertyChangeEventFlags.PropertyChanging;
        }

        public void PropertyChangedEventHandler(object? sender, PropertyChangedEventArgs e)
        {
            FiredFlags |= PropertyChangeEventFlags.PropertyChanged;
        }

        public void NestedPropertyChangingEventHandler(object? sender, NestedPropertyChangingEventArgs e)
        {
            FiredFlags |= PropertyChangeEventFlags.NestedPropertyChanging;
        }

        public void NestedPropertyChangedEventHandler(object? sender, NestedPropertyChangedEventArgs e)
        {
            FiredFlags |= PropertyChangeEventFlags.NestedPropertyChanged;
        }

        public void ResetFiredFlags() => FiredFlags = PropertyChangeEventFlags.None;

        public void SubscribeTo<T>(
            T value, ChangeSubscriptionOption option = ChangeSubscriptionOption.NestedChangeOnly)
            => PropertyChangeEvents.SubscribeTo(
                value,
                NestedPropertyChangingEventHandler, PropertyChangingEventHandler,
                option.HasOption(ChangeSubscriptionOption.NestedChangingOnly),
                NestedPropertyChangedEventHandler, PropertyChangedEventHandler,
                option.HasOption(ChangeSubscriptionOption.NestedChangedOnly));

        public void UnsubscribeFrom<T>(T value)
            => PropertyChangeEvents.UnsubscribeFrom(
                value,
                NestedPropertyChangingEventHandler, PropertyChangingEventHandler,
                NestedPropertyChangedEventHandler, PropertyChangedEventHandler);

        public void ShuffleToOther<T>(
            T value,
            TestNotifications other,
            ChangeSubscriptionOption option = ChangeSubscriptionOption.NestedChangeOnly)
            => PropertyChangeEvents.Shuffle(
                value,
                NestedPropertyChangingEventHandler, other.NestedPropertyChangingEventHandler,
                PropertyChangingEventHandler, other.PropertyChangingEventHandler,
                option.HasOption(ChangeSubscriptionOption.NestedChangingOnly),
                NestedPropertyChangedEventHandler, other.NestedPropertyChangedEventHandler,
                PropertyChangedEventHandler, other.PropertyChangedEventHandler,
                option.HasOption(ChangeSubscriptionOption.NestedChangedOnly));
    }
    #endregion
    #endregion
    #endregion
}
#endregion

#region Helpers
/// <summary>
/// Extension methods for the <see cref="ChangeSubscriptionOption"/> enum.
/// </summary>
internal static class ChangeSubscriptionOptions
{
    /// <summary>
    /// Applies the current option to a set of event flags, getting the expected resulting flags.
    /// </summary>
    /// <param name="option"></param>
    /// <param name="flags"></param>
    /// <returns></returns>
    public static PropertyChangeEventFlags ApplyTo(
        this ChangeSubscriptionOption option, PropertyChangeEventFlags flags)
    {
        // Remove options as necessary to describe the expected result
        if (option.HasOption(ChangeSubscriptionOption.NestedChangingOnly)
                && flags.HasEventFlag(PropertyChangeEventFlags.NestedPropertyChanging))
        {
            flags &= ~PropertyChangeEventFlags.PropertyChanging;
        }

        if (option.HasOption(ChangeSubscriptionOption.NestedChangedOnly)
                && flags.HasEventFlag(PropertyChangeEventFlags.NestedPropertyChanged))
        {
            flags &= ~PropertyChangeEventFlags.PropertyChanged;
        }

        return flags;
    }

    /// <summary>
    /// Determines if the given option is present in the current option set.
    /// </summary>
    /// <param name="current"></param>
    /// <param name="option"></param>
    /// <returns></returns>
    public static bool HasOption(this ChangeSubscriptionOption current, ChangeSubscriptionOption option)
        => (current & option) == option;
}

/// <summary>
/// Extension methods for the <see cref="PropertyChangeEventFlags"/> enum.
/// </summary>
internal static class PropertyChangeEventFlagsExtensions
{
    /// <summary>
    /// Determines if the given flag is present in the current flag set.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="flag"></param>
    /// <returns></returns>
    public static bool HasEventFlag(this PropertyChangeEventFlags value, PropertyChangeEventFlags flag)
        => (value & flag) == flag;
}

/// <summary>
/// Flags representing property change events.
/// </summary>
/// <remarks>
/// This differs from <see cref="PropertyChangeNotifications"/> because this type provides info about the actual
/// events fired rather than the notifications implemented by a given type.
/// </remarks>
[Flags]
internal enum PropertyChangeEventFlags
{
    /// <summary>
    /// Indicates that no property change events were fired.
    /// </summary>
    None = 0,

    /// <summary>
    /// Indicates that a non-nested property changed event was fired.
    /// </summary>
    PropertyChanged = 1,

    /// <summary>
    /// Indicates that a non-nested property changing event was fired.
    /// </summary>
    PropertyChanging = 2,

    /// <summary>
    /// Indicates that a nested property changed event was fired.
    /// </summary>
    NestedPropertyChanged = 4,

    /// <summary>
    /// Indicates that a nested property changing event was fired.
    /// </summary>
    NestedPropertyChanging = 8,
}

/// <summary>
/// Represents the options for handling of subscriptions to nested change events.
/// </summary>
[Flags]
internal enum ChangeSubscriptionOption : byte
{
    /// <summary>
    /// Indicates that all available events will be subscribed to.
    /// </summary>
    AllSupported = 0,

    /// <summary>
    /// Indicates that only nested changed events will be subscribed to, ignoring non-nested changed events.
    /// </summary>
    NestedChangedOnly = 1,

    /// <summary>
    /// Indicates that only nested changing events will be subscribed to, ignoring non-nested changing events.
    /// </summary>
    NestedChangingOnly = 2,

    /// <summary>
    /// Indicates that only nested change events will be subscribed to, ignoring their non-nested counterparts.
    /// </summary>
    NestedChangeOnly = NestedChangingOnly | NestedChangedOnly,
}
#endregion
