using Rem.Core.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;

namespace RemTest.Core.ComponentModel.Mvvm;

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

    /// <summary>
    /// Tests the <see cref="CachedHandlerNestedObservableObject"/> class handler caching.
    /// </summary>
    [TestMethod]
    public void TestCachedHandlerNestedObservableObject()
    {
        #region Variable Setup
        ImmutableStack<string> changingPath = ImmutableStack<string>.Empty, changedPath = ImmutableStack<string>.Empty;
        #endregion

        #region Event Setup
        var cached = new CachedHandlerObject();
        cached.NestedPropertyChanging += CachedNestedChanging;
        cached.NestedPropertyChanged += CachedNestedChanged;
        #endregion

        #region Event Handlers
        void CachedNestedChanging(object? sender, NestedPropertyChangingEventArgs e)
        {
            changingPath = e.PropertyPath;
        }

        void CachedNestedChanged(object? sender, NestedPropertyChangedEventArgs e)
        {
            changedPath = e.PropertyPath;
        }
        #endregion

        var a = new A();
        cached.AValue = a;
        Assert.IsTrue(changingPath.SequenceEqual(new[] { nameof(CachedHandlerObject.AValue) }));
        Assert.IsTrue(changedPath.SequenceEqual(new[] { nameof(CachedHandlerObject.AValue) }));

        a.BValue = new();
        Assert.IsTrue(changingPath.SequenceEqual(new[] { nameof(CachedHandlerObject.AValue), nameof(A.BValue) }));
        Assert.IsTrue(changedPath.SequenceEqual(new[] { nameof(CachedHandlerObject.AValue), nameof(A.BValue) }));

        cached.AValue = new();
        Assert.IsTrue(changingPath.SequenceEqual(new[] { nameof(CachedHandlerObject.AValue) }));
        Assert.IsTrue(changedPath.SequenceEqual(new[] { nameof(CachedHandlerObject.AValue) }));

        // Shouldn't run again - should have shuffled the event handlers internally
        a.BValue = new();
        Assert.IsTrue(changingPath.SequenceEqual(new[] { nameof(CachedHandlerObject.AValue) }));
        Assert.IsTrue(changedPath.SequenceEqual(new[] { nameof(CachedHandlerObject.AValue) }));

        // Should run now - should have shuffled the event handlers internally
        cached.AValue.BValue = new();
        Assert.IsTrue(changingPath.SequenceEqual(new[] { nameof(CachedHandlerObject.AValue), nameof(A.BValue) }));
        Assert.IsTrue(changedPath.SequenceEqual(new[] { nameof(CachedHandlerObject.AValue), nameof(A.BValue) }));

        // Shouldn't pick up change if unsubscribed
        cached.UnsubscribeFromA();
        cached.AValue.BValue.CValue = new();
        Assert.IsTrue(changingPath.SequenceEqual(new[] { nameof(CachedHandlerObject.AValue), nameof(A.BValue) }));
        Assert.IsTrue(changedPath.SequenceEqual(new[] { nameof(CachedHandlerObject.AValue), nameof(A.BValue) }));

        // Should pick up the change if resubscribed
        cached.ResubscribeToA();
        cached.AValue.BValue.CValue = new();
        Assert.IsTrue(changingPath.SequenceEqual(
                                    new[] { nameof(CachedHandlerObject.AValue), nameof(A.BValue), nameof(B.CValue) }));
        Assert.IsTrue(changedPath.SequenceEqual(
                                    new[] { nameof(CachedHandlerObject.AValue), nameof(A.BValue), nameof(B.CValue) }));
    }

    /// <summary>
    /// Tests the <see cref="CachedNestedObservableObject"/> class handler caching.
    /// </summary>
    [TestMethod]
    public void TestCachedNestedObservableObject()
    {
        #region Variable Setup
        ImmutableStack<string> changingPath = ImmutableStack<string>.Empty, changedPath = ImmutableStack<string>.Empty;
        #endregion

        #region Event Setup
        var cached = new CachedObject();
        cached.NestedPropertyChanging += CachedNestedChanging;
        cached.NestedPropertyChanged += CachedNestedChanged;
        #endregion

        #region Event Handlers
        void CachedNestedChanging(object? sender, NestedPropertyChangingEventArgs e)
        {
            changingPath = e.PropertyPath;
        }

        void CachedNestedChanged(object? sender, NestedPropertyChangedEventArgs e)
        {
            changedPath = e.PropertyPath;
        }
        #endregion

        var a = new A();
        cached.AValue = a;
        Assert.IsTrue(changingPath.SequenceEqual(new[] { nameof(CachedObject.AValue) }));
        Assert.IsTrue(changedPath.SequenceEqual(new[] { nameof(CachedObject.AValue) }));

        a.BValue = new();
        Assert.IsTrue(changingPath.SequenceEqual(new[] { nameof(CachedObject.AValue), nameof(A.BValue) }));
        Assert.IsTrue(changedPath.SequenceEqual(new[] { nameof(CachedObject.AValue), nameof(A.BValue) }));

        cached.AValue = new();
        Assert.IsTrue(changingPath.SequenceEqual(new[] { nameof(CachedObject.AValue) }));
        Assert.IsTrue(changedPath.SequenceEqual(new[] { nameof(CachedObject.AValue) }));

        // Shouldn't run again - should have shuffled the event handlers internally
        a.BValue = new();
        Assert.IsTrue(changingPath.SequenceEqual(new[] { nameof(CachedObject.AValue) }));
        Assert.IsTrue(changedPath.SequenceEqual(new[] { nameof(CachedObject.AValue) }));

        // Should run now - should have shuffled the event handlers internally
        cached.AValue.BValue = new();
        Assert.IsTrue(changingPath.SequenceEqual(new[] { nameof(CachedObject.AValue), nameof(A.BValue) }));
        Assert.IsTrue(changedPath.SequenceEqual(new[] { nameof(CachedObject.AValue), nameof(A.BValue) }));

        // Shouldn't pick up change if unsubscribed
        cached.UnsubscribeFromA();
        cached.AValue.BValue.CValue = new();
        Assert.IsTrue(changingPath.SequenceEqual(new[] { nameof(CachedObject.AValue), nameof(A.BValue) }));
        Assert.IsTrue(changedPath.SequenceEqual(new[] { nameof(CachedObject.AValue), nameof(A.BValue) }));

        // Should pick up the change if resubscribed
        cached.ResubscribeToA();
        cached.AValue.BValue.CValue = new();
        Assert.IsTrue(changingPath.SequenceEqual(
                                    new[] { nameof(CachedObject.AValue), nameof(A.BValue), nameof(B.CValue) }));
        Assert.IsTrue(changedPath.SequenceEqual(
                                    new[] { nameof(CachedObject.AValue), nameof(A.BValue), nameof(B.CValue) }));
    }

    #endregion

    #region Classes
    private sealed class CachedObject : CachedNestedObservableObject
    {
        public A? AValue
        {
            get => _aValue;
            set => SetProperty(ref _aValue, value);
        }
        private A? _aValue;

        public void UnsubscribeFromA() { UnsubscribeFromChanges(_aValue, nameof(AValue)); }
        public void ResubscribeToA() { SubscribeToChanges(_aValue, nameof(AValue)); }
    }

#pragma warning disable CS0618 // Still needs testing
    private sealed class CachedHandlerObject : CachedHandlerNestedObservableObject
#pragma warning restore CS0618
    {
        public A? AValue
        {
            get => _aValue;
            set => SetObservableProperty(ref _aValue, value);
        }
        private A? _aValue;

        public void UnsubscribeFromA() { UnsubscribeFromChanges(_aValue, nameof(AValue)); }
        public void ResubscribeToA() { SubscribeToChanges(_aValue, nameof(AValue)); }
    }

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
}