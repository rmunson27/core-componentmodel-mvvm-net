using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Rem.Core.ComponentModel.Mvvm;

/// <summary>
/// A base class for objects that notify of nested property changes.
/// </summary>
/// <remarks>
/// The <see cref="NestedPropertyChanging"/> and <see cref="NestedPropertyChanged"/> events
/// implemented by this class will be triggered whenever the <see cref="ObservableObject.PropertyChanging"/> and
/// <see cref="ObservableObject.PropertyChanged"/> events are triggered, respectively.
/// </remarks>
public abstract class NestedObservableObject
    : ObservableObject, INotifyNestedPropertyChanged, INotifyNestedPropertyChanging
{
    #region Events
    /// <inheritdoc cref="INotifyNestedPropertyChanged.NestedPropertyChanged"/>
    public event NestedPropertyChangedEventHandler? NestedPropertyChanged;

    /// <inheritdoc cref="INotifyNestedPropertyChanging.NestedPropertyChanging"/>
    public event NestedPropertyChangingEventHandler? NestedPropertyChanging;
    #endregion

    #region Constructor
    /// <summary>
    /// Constructs a new instance of the <see cref="NestedObservableObject"/> class.
    /// </summary>
    protected NestedObservableObject()
    {
        // Ensure that nested property changes are fired whenever property changes are
        PropertyChanging += This_PropertyChanging;
        PropertyChanged += This_PropertyChanged;
    }
    #endregion

    #region Methods
    #region Property Mutators
    #region Setters
    #region Nested
    /// <summary>
    /// Sets a property that notifies of nested changes, handling the shuffling of events before and after setting
    /// the value and firing appropriate property change and nested property change events.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="field"></param>
    /// <param name="newValue"></param>
    /// <param name="changing"></param>
    /// <param name="changed"></param>
    /// <param name="propertyName"></param>
    protected void SetNestedChangeProperty<T>(
        [NotNullIfNotNull(nameof(newValue))] ref T? field, T? newValue,
        NestedPropertyChangingEventHandler changing, NestedPropertyChangedEventHandler changed,
        [CallerMemberName] string? propertyName = null)
        where T : INotifyNestedPropertyChanging, INotifyNestedPropertyChanged
    {
        OnPropertyChanging(propertyName);
        InitializeNestedChangeProperty(ref field, newValue, changing, changed);
        OnPropertyChanged(propertyName);
    }

    /// <summary>
    /// Sets a property that notifies of nested changes, handling the shuffling of events before and after setting
    /// the value and firing appropriate property change and nested property change events.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="field"></param>
    /// <param name="newValue"></param>
    /// <param name="changing"></param>
    /// <param name="propertyName"></param>
    protected void SetNestedChangeProperty<T>(
        [NotNullIfNotNull(nameof(newValue))] ref T? field, T? newValue,
        NestedPropertyChangingEventHandler changing,
        [CallerMemberName] string? propertyName = null)
        where T : INotifyNestedPropertyChanging
    {
        OnPropertyChanging(propertyName);
        InitializeNestedChangeProperty(ref field, newValue, changing);
        OnPropertyChanged(propertyName);
    }

    /// <summary>
    /// Sets a property that notifies of nested changes, handling the shuffling of events before and after setting
    /// the value and firing appropriate property change and nested property change events.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="field"></param>
    /// <param name="newValue"></param>
    /// <param name="changed"></param>
    /// <param name="propertyName"></param>
    protected void SetNestedChangeProperty<T>(
        [NotNullIfNotNull(nameof(newValue))] ref T? field, T? newValue,
        NestedPropertyChangedEventHandler changed,
        [CallerMemberName] string? propertyName = null)
        where T : INotifyNestedPropertyChanged
    {
        OnPropertyChanging(propertyName);
        InitializeNestedChangeProperty(ref field, newValue, changed);
        OnPropertyChanged(propertyName);
    }
    #endregion

    #region Single
    /// <summary>
    /// Sets a property that notifies of changes, handling the shuffling of events before and after setting the value
    /// and firing appropriate property change and nested property change events.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="field"></param>
    /// <param name="newValue"></param>
    /// <param name="changing"></param>
    /// <param name="changed"></param>
    /// <param name="propertyName"></param>
    protected void SetChangeProperty<T>(
        [NotNullIfNotNull(nameof(newValue))] ref T? field, T? newValue,
        PropertyChangingEventHandler changing, PropertyChangedEventHandler changed,
        [CallerMemberName] string? propertyName = null)
        where T : INotifyPropertyChanging, INotifyPropertyChanged
    {
        OnPropertyChanging(propertyName);
        InitializeChangeProperty(ref field, newValue, changing, changed);
        OnPropertyChanged(propertyName);
    }

    /// <summary>
    /// Sets a property that notifies of changes, handling the shuffling of events before and after setting the value
    /// and firing appropriate property change and nested property change events.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="field"></param>
    /// <param name="newValue"></param>
    /// <param name="changing"></param>
    /// <param name="propertyName"></param>
    protected void SetChangeProperty<T>(
        [NotNullIfNotNull(nameof(newValue))] ref T? field, T? newValue,
        PropertyChangingEventHandler changing,
        [CallerMemberName] string? propertyName = null)
        where T : INotifyPropertyChanging
    {
        OnPropertyChanging(propertyName);
        InitializeChangeProperty(ref field, newValue, changing);
        OnPropertyChanged(propertyName);
    }

    /// <summary>
    /// Sets a property that notifies of changes, handling the shuffling of events before and after setting the value
    /// and firing appropriate property change and nested property change events.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="field"></param>
    /// <param name="newValue"></param>
    /// <param name="changed"></param>
    /// <param name="propertyName"></param>
    protected void SetChangeProperty<T>(
        [NotNullIfNotNull(nameof(newValue))] ref T? field, T? newValue,
        PropertyChangedEventHandler changed,
        [CallerMemberName] string? propertyName = null)
        where T : INotifyPropertyChanged
    {
        OnPropertyChanging(propertyName);
        InitializeChangeProperty(ref field, newValue, changed);
        OnPropertyChanged(propertyName);
    }
    #endregion
    #endregion

    #region Initializers
    #region Nested
    /// <summary>
    /// Initializes a property that notifies of nested changes, handling the shuffling of events before and after
    /// setting the value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="field"></param>
    /// <param name="newValue"></param>
    /// <param name="changing"></param>
    /// <param name="changed"></param>
    protected static void InitializeNestedChangeProperty<T>(
			[NotNullIfNotNull(nameof(newValue))] ref T? field, T? newValue,
			NestedPropertyChangingEventHandler changing, NestedPropertyChangedEventHandler changed)
	where T : INotifyNestedPropertyChanging, INotifyNestedPropertyChanged
    {
        // Remove old events
        if (field is not null)
        {
            field.NestedPropertyChanging -= changing;
            field.NestedPropertyChanged -= changed;
        }

        // Set the field
        field = newValue;

        // Add new events
        if (field is not null)
        {
            field.NestedPropertyChanging += changing;
            field.NestedPropertyChanged += changed;
        }
    }

    /// <summary>
    /// Initializes a property that notifies of nested changes, handling the shuffling of events before and after
    /// setting the value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="field"></param>
    /// <param name="newValue"></param>
    /// <param name="changing"></param>
    protected static void InitializeNestedChangeProperty<T>(
			[NotNullIfNotNull(nameof(newValue))] ref T? field, T? newValue,
			NestedPropertyChangingEventHandler changing)
	where T : INotifyNestedPropertyChanging
    {
        // Remove old events
        if (field is not null)
        {
            field.NestedPropertyChanging -= changing;
        }

        // Set the field
        field = newValue;

        // Add new events
        if (field is not null)
        {
            field.NestedPropertyChanging += changing;
        }
    }

    /// <summary>
    /// Initializes a property that notifies of nested changes, handling the shuffling of events before and after
    /// setting the value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="field"></param>
    /// <param name="newValue"></param>
    /// <param name="changed"></param>
    protected static void InitializeNestedChangeProperty<T>(
			[NotNullIfNotNull(nameof(newValue))] ref T? field, T? newValue,
			NestedPropertyChangedEventHandler changed)
	where T : INotifyNestedPropertyChanged
    {
        // Remove old events
        if (field is not null)
        {
            field.NestedPropertyChanged -= changed;
        }

        // Set the field
        field = newValue;

        // Add new events
        if (field is not null)
        {
            field.NestedPropertyChanged += changed;
        }
    }
    #endregion

    #region Single
    /// <summary>
    /// Initializes a property that notifies of changes, handling the shuffling of events before and after setting
    /// the value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="field"></param>
    /// <param name="newValue"></param>
    /// <param name="changing"></param>
    /// <param name="changed"></param>
    protected static void InitializeChangeProperty<T>(
			[NotNullIfNotNull(nameof(newValue))] ref T? field, T? newValue,
			PropertyChangingEventHandler changing, PropertyChangedEventHandler changed)
	where T : INotifyPropertyChanging, INotifyPropertyChanged
    {
        // Remove old events
        if (field is not null)
        {
            field.PropertyChanging -= changing;
            field.PropertyChanged -= changed;
        }

        // Set the field
        field = newValue;

        // Add new events
        if (field is not null)
        {
            field.PropertyChanging += changing;
            field.PropertyChanged += changed;
        }
    }

    /// <summary>
    /// Initializes a property that notifies of changes, handling the shuffling of events before and after setting
    /// the value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="field"></param>
    /// <param name="newValue"></param>
    /// <param name="changing"></param>
    protected static void InitializeChangeProperty<T>(
			[NotNullIfNotNull(nameof(newValue))] ref T? field, T? newValue,
		    PropertyChangingEventHandler changing)
    where T : notnull, INotifyPropertyChanging
    {
        // Remove old events
        if (field is not null)
        {
            field.PropertyChanging -= changing;
        }

        // Set the field
        field = newValue;

        // Add new events
        if (field is not null)
        {
            field.PropertyChanging += changing;
        }
    }

    /// <summary>
    /// Initializes a property that notifies of changes, handling the shuffling of events before and after setting
    /// the value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="field"></param>
    /// <param name="newValue"></param>
    /// <param name="changed"></param>
    protected static void InitializeChangeProperty<T>(
			[NotNullIfNotNull(nameof(newValue))] ref T? field, T? newValue,
			PropertyChangedEventHandler changed)
	where T : INotifyPropertyChanged
    {
        // Remove old events
        if (field is not null)
        {
            field.PropertyChanged -= changed;
        }

        // Set the field
        field = newValue;

        // Add new events
        if (field is not null)
        {
            field.PropertyChanged += changed;
        }
    }
    #endregion
    #endregion
    #endregion

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
            OnNestedPropertyChanging(new(ImmutableStack.CreateRange(new[] { e.PropertyName, childPropertyName })));
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
            OnNestedPropertyChanged(new(ImmutableStack.CreateRange(new[] { e.PropertyName, childPropertyName })));
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
    #endregion
}
