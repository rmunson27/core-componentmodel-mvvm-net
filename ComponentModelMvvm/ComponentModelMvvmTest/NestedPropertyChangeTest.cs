using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rem.Core.ComponentModel;
using Rem.Core.ComponentModel.Mvvm;
using Rem.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Rem.CoreTest.ComponentModel.Mvvm;

using NotifierTypeDescription = NestedPropertyChangeTest.GenericPropertyChange.NotifierTypeDescription;

/// <summary>
/// Tests of the <see cref="INotifyNestedPropertyChanging"/> and <see cref="INotifyNestedPropertyChanged"/> interfaces,
/// particularly when using the <see cref="NestedObservableObject"/> base class.
/// </summary>
[TestClass]
public class NestedPropertyChangeTest
{
    #region Tests
    /// <summary>
    /// Tests the nested property change events raised by instances of the <see cref="NestedObservableObject"/> class.
    /// </summary>
    [TestMethod]
    public void TestNestedObservableObjects()
    {
        #region Variable Setup
        ImmutableStack<string> changingPath = ImmutableStack<string>.Empty, changedPath = ImmutableStack<string>.Empty;
        #endregion

        #region Event Setup
        var a = new A();
        a.NestedPropertyChanging += ANestedChanging;
        a.NestedPropertyChanged += ANestedChanged;
        #endregion

        #region Assertions
        var b = new B();
        a.BValue = b;
        Assert.IsTrue(changingPath.SequenceEqual(new[] { nameof(A.BValue) }));
        Assert.IsTrue(changedPath.SequenceEqual(new[] { nameof(A.BValue) }));

        b.CValue = new C();
        Assert.IsTrue(changingPath.SequenceEqual(new[] { nameof(A.BValue), nameof(B.CValue) }));
        Assert.IsTrue(changedPath.SequenceEqual(new[] { nameof(A.BValue), nameof(B.CValue) }));

        b.CValue.BoolValue = true;
        Assert.IsTrue(changingPath.SequenceEqual(new[] { nameof(A.BValue), nameof(B.CValue), nameof(C.BoolValue) }));
        Assert.IsTrue(changedPath.SequenceEqual(new[] { nameof(A.BValue), nameof(B.CValue), nameof(C.BoolValue) }));

        // The events should have been unsubscribed to, so the change in b should not be picked up
        a.BValue = new B();
        b.CValue.BoolValue = false;
        Assert.IsTrue(changingPath.SequenceEqual(new[] { nameof(A.BValue) }));
        Assert.IsTrue(changingPath.SequenceEqual(new[] { nameof(A.BValue) }));
        #endregion

        #region Event Handlers
        void ANestedChanging(object? sender, NestedPropertyChangingEventArgs e)
        {
            changingPath = e.PropertyPath;
        }

        void ANestedChanged(object? sender, NestedPropertyChangedEventArgs e)
        {
            changedPath = e.PropertyPath;
        }
        #endregion
    }
    #endregion

    #region Classes
    #region Test
    private sealed class A : NestedObservableObject
    {
        public B? BValue
        {
            get => _bValue;
            set => SetNestedChangeProperty(ref _bValue, value, BValueChanging, BValueChanged);
        }
        private B? _bValue;

        private void BValueChanged(object? _, NestedPropertyChangedEventArgs e)
            => OnChildNestedPropertyChanged(nameof(BValue), e);
        private void BValueChanging(object? _, NestedPropertyChangingEventArgs e)
            => OnChildNestedPropertyChanging(nameof(BValue), e);
    }

    private sealed class B : NestedObservableObject
    {
        public C? CValue
        {
            get => _cValue;
            set => SetChangeProperty(ref _cValue, value, CValueChanging, CValueChanged);
        }
        private C? _cValue;

        private void CValueChanging(object? _, PropertyChangingEventArgs e)
            => OnChildPropertyChanging(nameof(CValue), e);
        private void CValueChanged(object? _, PropertyChangedEventArgs e)
            => OnChildPropertyChanged(nameof(CValue), e);
    }

    private sealed class C : ObservableObject
    {
        public bool BoolValue
        {
            get => _boolValue;
            set => SetProperty(ref _boolValue, value);
        }
        private bool _boolValue;
    }
    #endregion

    #region Helpers
    /// <summary>
    /// The internal generic class providing functionality for the generic methods in the non-generic
    /// <see cref="GenericPropertyChange"/> class.
    /// </summary>
    /// 
    /// <typeparam name="TNotifier">
    /// The generic reference type that potentially exposes property change events this class can be used to subscribe to
    /// and unsubscribe from.
    /// </typeparam>
    private static class GenericPropertyChangeNotifier<TNotifier> where TNotifier : class
    {
        private static readonly NotifierTypeDescription TNotifierTypeDescription;

        static GenericPropertyChangeNotifier()
        {
            if (typeof(TNotifier).IsSubclassOf(typeof(NestedObservableObject))
                    || typeof(TNotifier) == typeof(NestedObservableObject))
            {
                TNotifierTypeDescription = NotifierTypeDescription.NestedObservableObject;
            }
            else // Need to look at the interfaces implemented by TNotifier
            {
                TNotifierTypeDescription = NotifierTypeDescription.None;
                var tNotifierInterfaces = new HashSet<Type>(typeof(TNotifier).GetInterfaces());

                if (tNotifierInterfaces.Contains(typeof(INotifyNestedPropertyChanging))
                        || typeof(TNotifier) == typeof(INotifyNestedPropertyChanging))
                {
                    TNotifierTypeDescription |= NotifierTypeDescription.NestedPropertyChanging;
                }
                else if (tNotifierInterfaces.Contains(typeof(INotifyPropertyChanging))
                            || typeof(TNotifier) == typeof(INotifyPropertyChanging))
                {
                    TNotifierTypeDescription |= NotifierTypeDescription.PropertyChanging;
                }

                if (tNotifierInterfaces.Contains(typeof(INotifyNestedPropertyChanged))
                        || typeof(TNotifier) == typeof(INotifyNestedPropertyChanged))
                {
                    TNotifierTypeDescription |= NotifierTypeDescription.NestedPropertyChanged;
                }
                else if (tNotifierInterfaces.Contains(typeof(INotifyPropertyChanged))
                            || typeof(TNotifier) == typeof(INotifyPropertyChanged))
                {
                    TNotifierTypeDescription |= NotifierTypeDescription.PropertyChanged;
                }
            }
        }

        /// <summary>
        /// Subscribes to any relevant property change events on the <typeparamref name="TNotifier"/> value passed in.
        /// </summary>
        /// 
        /// <param name="value"></param>
        /// <param name="propertyChanging"></param>
        /// <param name="propertyChanged"></param>
        /// <param name="nestedPropertyChanging"></param>
        /// <param name="nestedPropertyChanged"></param>
        /// <param name="ignoreSingularIfNested">
        /// Whether or not to ignore singular (<see cref="INotifyPropertyChanging"/>, <see cref="INotifyPropertyChanged"/>)
        /// interfaces when subscribing if their nested analogs are implemented.
        /// </param>
        public static void SubscribeTo(
            TNotifier? value,
            PropertyChangingEventHandler? propertyChanging,
            PropertyChangedEventHandler? propertyChanged,
            NestedPropertyChangingEventHandler? nestedPropertyChanging,
            NestedPropertyChangedEventHandler? nestedPropertyChanged,
            bool ignoreSingularIfNested)
        {
            if (value is not null)
            {
                if (Enums.HasFlag(TNotifierTypeDescription, NotifierTypeDescription.NestedPropertyChanging))
                {
                    Unsafe.As<INotifyNestedPropertyChanging>(value).NestedPropertyChanging += nestedPropertyChanging;
                    if (!ignoreSingularIfNested)
                    {
                        Unsafe.As<INotifyPropertyChanging>(value).PropertyChanging += propertyChanging;
                    }
                }
                else if (Enums.HasFlag(TNotifierTypeDescription, NotifierTypeDescription.PropertyChanging))
                {
                    Unsafe.As<INotifyPropertyChanging>(value).PropertyChanging += propertyChanging;
                }

                if (Enums.HasFlag(TNotifierTypeDescription, NotifierTypeDescription.NestedPropertyChanged))
                {
                    Unsafe.As<INotifyNestedPropertyChanged>(value).NestedPropertyChanged += nestedPropertyChanged;
                    if (!ignoreSingularIfNested)
                    {
                        Unsafe.As<INotifyPropertyChanged>(value).PropertyChanged += propertyChanged;
                    }
                }
                else if (Enums.HasFlag(TNotifierTypeDescription, NotifierTypeDescription.PropertyChanged))
                {
                    Unsafe.As<INotifyPropertyChanged>(value).PropertyChanged += propertyChanged;
                }
            }
        }

        /// <summary>
        /// Unsubscribes from any relevant property change events on the <typeparamref name="TNotifier"/> value passed in.
        /// </summary>
        /// 
        /// <param name="value"></param>
        /// <param name="propertyChanging"></param>
        /// <param name="propertyChanged"></param>
        /// <param name="nestedPropertyChanging"></param>
        /// <param name="nestedPropertyChanged"></param>
        public static void UnsubscribeFrom(
            TNotifier? value,
            PropertyChangingEventHandler? propertyChanging,
            PropertyChangedEventHandler? propertyChanged,
            NestedPropertyChangingEventHandler? nestedPropertyChanging,
            NestedPropertyChangedEventHandler? nestedPropertyChanged)
        {
            if (value is not null)
            {
                if (Enums.HasFlag(TNotifierTypeDescription, NotifierTypeDescription.NestedPropertyChanging))
                {
                    Unsafe.As<INotifyNestedPropertyChanging>(value).NestedPropertyChanging -= nestedPropertyChanging;
                    Unsafe.As<INotifyPropertyChanging>(value).PropertyChanging -= propertyChanging;
                }
                else if (Enums.HasFlag(TNotifierTypeDescription, NotifierTypeDescription.PropertyChanging))
                {
                    Unsafe.As<INotifyPropertyChanging>(value).PropertyChanging -= propertyChanging;
                }

                if (Enums.HasFlag(TNotifierTypeDescription, NotifierTypeDescription.NestedPropertyChanged))
                {
                    Unsafe.As<INotifyNestedPropertyChanged>(value).NestedPropertyChanged -= nestedPropertyChanged;
                    Unsafe.As<INotifyPropertyChanged>(value).PropertyChanged -= propertyChanged;
                }
                else if (Enums.HasFlag(TNotifierTypeDescription, NotifierTypeDescription.PropertyChanged))
                {
                    Unsafe.As<INotifyPropertyChanged>(value).PropertyChanged -= propertyChanged;
                }
            }
        }
    }

    /// <summary>
    /// A class containing helper methods for subscribing to and unsubscribing from property change notification events
    /// implemented by a given generic reference type.
    /// </summary>
    /// 
    /// <remarks>
    /// This class can be useful when extending <see cref="NestedObservableObject"/> and attempting to yield accurate
    /// nested property change notifications on a generic reference type property that may not be known to implement
    /// property change notifications.
    /// </remarks>
    internal static class GenericPropertyChange
    {
        /// <summary>
        /// Shuffles the events subscribed to from an old <typeparamref name="TNotifier"/> value to a new
        /// <typeparamref name="TNotifier"/> value, according to the property change notifications implemented by the type.
        /// </summary>
        /// 
        /// <remarks>
        /// This method assumes that it is sufficient to subscribe to nested events rather than their singular
        /// counterparts if possible; e.g. if <typeparamref name="TNotifier"/> implements
        /// <see cref="INotifyNestedPropertyChanged"/>, then the method will subscribe to the
        /// <see cref="INotifyNestedPropertyChanged.NestedPropertyChanged"/> event and ignore the
        /// <see cref="INotifyPropertyChanged.PropertyChanged"/> event inherited by that interface.
        /// </remarks>
        /// 
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        /// <param name="propertyChanging"></param>
        /// <param name="propertyChanged"></param>
        /// <param name="nestedPropertyChanging"></param>
        /// <param name="nestedPropertyChanged"></param>
        /// <param name="ignoreSingularIfNested">
        /// Whether or not to ignore singular (<see cref="INotifyPropertyChanging"/>, <see cref="INotifyPropertyChanged"/>)
        /// interfaces when subscribing if their nested analogs are implemented.
        /// </param>
        /// 
        /// <typeparam name="TNotifier">
        /// The generic reference type that potentially exposes property change events that this method can be used to
        /// subscribe to and unsubscribe from.
        /// </typeparam>
        public static void ShuffleHandlers<TNotifier>(
            TNotifier? oldValue, TNotifier? newValue,
            PropertyChangingEventHandler? propertyChanging,
            PropertyChangedEventHandler? propertyChanged,
            NestedPropertyChangingEventHandler? nestedPropertyChanging,
            NestedPropertyChangedEventHandler? nestedPropertyChanged,
            bool ignoreSingularIfNested = true)
            where TNotifier : class
        {
            // Unsubscribe from any relevant event handlers on the old value
            UnsubscribeFrom(oldValue, propertyChanging, propertyChanged, nestedPropertyChanging, nestedPropertyChanged);

            // Subscribe to any relevant event handlers on the new value
            SubscribeTo(
                newValue,
                propertyChanging, propertyChanged, nestedPropertyChanging, nestedPropertyChanged,
                ignoreSingularIfNested);
        }

        /// <summary>
        /// Subscribes to any relevant property change events on the <typeparamref name="TNotifier"/> value passed in.
        /// </summary>
        /// 
        /// <param name="value"></param>
        /// <param name="propertyChanging"></param>
        /// <param name="propertyChanged"></param>
        /// <param name="nestedPropertyChanging"></param>
        /// <param name="nestedPropertyChanged"></param>
        /// <param name="ignoreSingularIfNested">
        /// Whether or not to ignore singular (<see cref="INotifyPropertyChanging"/>, <see cref="INotifyPropertyChanged"/>)
        /// interfaces when subscribing if their nested analogs are implemented.
        /// </param>
        /// 
        /// <typeparam name="TNotifier">
        /// The generic reference type that potentially exposes property change events that this method can be used to
        /// subscribe to.
        /// </typeparam>
        public static void SubscribeTo<TNotifier>(
            TNotifier? value,
            PropertyChangingEventHandler? propertyChanging,
            PropertyChangedEventHandler? propertyChanged,
            NestedPropertyChangingEventHandler? nestedPropertyChanging,
            NestedPropertyChangedEventHandler? nestedPropertyChanged,
            bool ignoreSingularIfNested = true)
            where TNotifier : class
            => GenericPropertyChangeNotifier<TNotifier>.SubscribeTo(
                value,
                propertyChanging, propertyChanged, nestedPropertyChanging, nestedPropertyChanged,
                ignoreSingularIfNested);

        /// <summary>
        /// Unsubscribes from any relevant property change events on the <typeparamref name="TNotifier"/> value passed in.
        /// </summary>
        /// 
        /// <param name="value"></param>
        /// <param name="propertyChanging"></param>
        /// <param name="propertyChanged"></param>
        /// <param name="nestedPropertyChanging"></param>
        /// <param name="nestedPropertyChanged"></param>
        /// 
        /// <typeparam name="TNotifier">
        /// The generic reference type that potentially exposes property change events that this method can be used to
        /// unsubscribe from.
        /// </typeparam>
        public static void UnsubscribeFrom<TNotifier>(
            TNotifier? value,
            PropertyChangingEventHandler? propertyChanging,
            PropertyChangedEventHandler? propertyChanged,
            NestedPropertyChangingEventHandler? nestedPropertyChanging,
            NestedPropertyChangedEventHandler? nestedPropertyChanged)
            where TNotifier : class
            => GenericPropertyChangeNotifier<TNotifier>.UnsubscribeFrom(
                value,
                propertyChanging, propertyChanged, nestedPropertyChanging, nestedPropertyChanged);

        /// <summary>
        /// Describes the type of property changes implemented by a given type.
        /// </summary>
        /// <remarks>
        /// This enum respects the interface design of nested property change notifications; i.e. the
        /// <see cref="NestedPropertyChanged"/> value has the <see cref="PropertyChanged"/> flag set because the
        /// <see cref="INotifyNestedPropertyChanged"/> interface extends the <see cref="INotifyPropertyChanged"/>
        /// interface.
        /// </remarks>
        internal enum NotifierTypeDescription : byte
        {
            /// <summary>
            /// The type does not implement any property change notifications.
            /// </summary>
            None = 0,

            /// <summary>
            /// The type implements <see cref="INotifyPropertyChanging"/>.
            /// </summary>
            PropertyChanging = 1,

            /// <summary>
            /// The type implements <see cref="INotifyPropertyChanged"/>.
            /// </summary>
            PropertyChanged = 2,

            /// <summary>
            /// The type implements <see cref="INotifyNestedPropertyChanging"/> (and therefore
            /// also <see cref="INotifyPropertyChanging"/>).
            /// </summary>
            NestedPropertyChanging = PropertyChanging | 4,

            /// <summary>
            /// The type implements <see cref="INotifyNestedPropertyChanged"/> (and therefore
            /// also <see cref="INotifyPropertyChanged"/>).
            /// </summary>
            NestedPropertyChanged = PropertyChanged | 8,

            /// <summary>
            /// The type extends <see cref="NestedObservableObject"/>, and therefore implements
            /// both <see cref="INotifyNestedPropertyChanging"/> and <see cref="INotifyNestedPropertyChanged"/>, and the
            /// <see cref="ObservableObject.PropertyChanging"/> and <see cref="ObservableObject.PropertyChanged"/> events
            /// are guaranteed to fire <see cref="NestedObservableObject.NestedPropertyChanging"/> and
            /// <see cref="NestedObservableObject.NestedPropertyChanged"/> events, respectively.
            /// </summary>
            /// <remarks>
            /// This is useful information, as a generic subscriber can subscribe to only the object's nested property
            /// change handlers and trust that these will yield complete information about the changes happening in
            /// the object.
            /// </remarks>
            NestedObservableObject = NestedPropertyChanging | NestedPropertyChanged | 16,
        }
    }
    #endregion
    #endregion
}