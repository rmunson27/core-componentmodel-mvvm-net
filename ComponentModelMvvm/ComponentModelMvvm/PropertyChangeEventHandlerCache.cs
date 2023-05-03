using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Rem.Core.Attributes;

namespace Rem.Core.ComponentModel.Mvvm;

/// <summary>
/// Maintains a cache of property change event handlers for various properties.
/// </summary>
/// <remarks>
/// This is used internally to implement nested observable objects, but is exposed here in case it is useful in a
/// context when using that class is not possible (such as inheriting from a class that does not extend that type).
/// <para/>
/// If a given property provides both a nested and non-nested version of a property change event, the nested version
/// will be preferred.
/// </remarks>
public struct PropertyChangeEventHandlerCache : IDefaultableStruct
{
    /// <summary>
    /// The default capacity used for new <see cref="PropertyChangeEventHandlerCache"/> instances.
    /// </summary>
    public const int DefaultCapacity = 4;

    /// <summary>
    /// Determines if this instance is the invalid <see langword="default"/> value.
    /// </summary>
    [MemberNotNullWhen(false, nameof(_buckets))]
    [MemberNotNullWhen(false, nameof(_bucketMap))]
    public bool IsDefault => _buckets is null;

    /// <summary>
    /// Maps string property names to the indices of buckets in <see cref="_buckets"/>.
    /// <para/>
    /// This way the buckets can be accessed by reference (via <see cref="_buckets"/>) temporarily in order to get
    /// their contents and allow quick property changes.
    /// </summary>
    /// <remarks>
    /// This class will lock on this object internally to avoid interfering with its own internal operations.
    /// It is not possible to lock on <see cref="_buckets"/> as it may be replaced during the normal functioning of
    /// this class.
    /// </remarks>
    private readonly Dictionary<string, int> _bucketMap;

    /// <summary>
    /// Stores the contents of the cache, with buckets for each property.
    /// </summary>
    private Bucket[] _buckets;

    /// <summary>
    /// Constructs a new cache with the default initial capacity.
    /// This constructor does <b><i>not</i></b> construct the <see langword="default"/> instance.
    /// </summary>
    public PropertyChangeEventHandlerCache() : this(DefaultCapacity) { }

    /// <summary>
    /// Constructs a new cache with the specified initial capacity.
    /// </summary>
    [NotDefault]
    public PropertyChangeEventHandlerCache(int capacity)
    {
        _bucketMap = new(capacity);
        _buckets = new Bucket[capacity];
    }

    /// <summary>
    /// Gets the property change handlers for the given property in <see langword="out"/> parameters if possible.
    /// </summary>
    /// <param name="propertyName"></param>
    /// <param name="nestedChanging"></param>
    /// <param name="changing"></param>
    /// <param name="nestedChanged"></param>
    /// <param name="changed"></param>
    /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> was <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="propertyName"/> was not handled by the cache at the time this method was called.
    /// </exception>
    /// <returns>
    /// Whether or not handlers for the property with the given name are stored in the cache.
    /// If this method returns <see langword="false"/>, all <see langword="out"/> parameters will be
    /// <see langword="null"/> when the call returns.
    /// </returns>
    public bool TryGetHandlersForProperty<T>(out NestedPropertyChangingEventHandler? nestedChanging,
                                             out PropertyChangingEventHandler? changing,
                                             out NestedPropertyChangedEventHandler? nestedChanged,
                                             out PropertyChangedEventHandler? changed,

                                             [CallerMemberName] string? propertyName = null)
    {
        if (propertyName is null) throw new ArgumentNullException(nameof(propertyName));

        var stored = PropertyChangeEvents.GetSupportedBy<T>();

        nestedChanging = null;
        changing = null;
        nestedChanged = null;
        changed = null;

        lock (_bucketMap)
        {
            if (!_bucketMap.ContainsKey(propertyName)) return false;
            else
            {
                var index = _bucketMap[propertyName];
                ref var bucket = ref _buckets[index];

                if (stored.HasNotification(PropertyChangeNotifications.NestedPropertyChanging))
                {
                    nestedChanging = Unsafe.As<NestedPropertyChangingEventHandler>(bucket.Changing);
                }
                else changing = Unsafe.As<PropertyChangingEventHandler>(bucket.Changing);

                if (stored.HasNotification(PropertyChangeNotifications.NestedPropertyChanged))
                {
                    nestedChanged = Unsafe.As<NestedPropertyChangedEventHandler>(bucket.Changed);
                }
                else changed = Unsafe.As<PropertyChangedEventHandler>(bucket.Changed);

                return true;
            }
        }
    }

    /// <summary>
    /// Adds the property with the given name to the cache.
    /// </summary>
    /// <param name="propertyName"></param>
    /// <param name="nestedChanging"></param>
    /// <param name="changing"></param>
    /// <param name="nestedChanged"></param>
    /// <param name="changed"></param>
    public void AddProperty<T>(NestedPropertyChangingEventHandler? nestedChanging,
                               PropertyChangingEventHandler? changing,
                               NestedPropertyChangedEventHandler? nestedChanged,
                               PropertyChangedEventHandler? changed,

                               [CallerMemberName] string? propertyName = null)
    {
        var currentCount = _bucketMap.Count;

        var stored = PropertyChangeEvents.GetSupportedBy<T>();

        if (stored == PropertyChangeNotifications.None) return;

        lock (_bucketMap)
        {
            if (currentCount == _buckets.Length) Expand(); // Resize the array, if necessary
            _bucketMap.Add(propertyName!, currentCount);

            ref var bucket = ref _buckets[currentCount];

            bucket.Changing = stored.HasNotification(PropertyChangeNotifications.NestedPropertyChanging)
                                ? nestedChanging
                                : changing;
            bucket.Changed = stored.HasNotification(PropertyChangeNotifications.NestedPropertyChanged)
                                ? nestedChanged
                                : changed;
        }
    }

    /// <summary>
    /// Initializes or re-initializes the property with the given name in the cache.
    /// </summary>
    /// <param name="propertyName"></param>
    public void InitializeProperty([CallerMemberName] string? propertyName = null)
    {
        var currentCount = _bucketMap.Count;

        lock (_bucketMap)
        {
            if (currentCount == _buckets.Length) Expand();
            _bucketMap.Add(propertyName!, currentCount);
            _buckets[currentCount] = default;
        }
    }

    /// <summary>
    /// Removes the property with the given name.
    /// </summary>
    /// <param name="propertyName"></param>
    /// <returns></returns>
    public bool RemoveProperty([CallerMemberName] string? propertyName = null)
    {
        if (propertyName is null || !_bucketMap.ContainsKey(propertyName)) return false;

        var oldIndex = _bucketMap[propertyName];
        var maxIndex = _bucketMap.Count - 1;

        lock (_bucketMap)
        {
            // Need to move the last bucket to the position of the old one so it will not be deleted
            if (oldIndex != maxIndex) _buckets[oldIndex] = _buckets[maxIndex];

            _bucketMap.Remove(propertyName); // The lower count implies the array is 1 shorter
        }

        return true;
    }

    /// <summary>
    /// Trims excess unused storage from the cache if any exists.
    /// </summary>
    public void TrimExcess()
    {
        var count = _bucketMap.Count;
        if (count != _buckets.Length)
        {
            lock (_bucketMap) Array.Resize(ref _buckets, count);
        }
    }

    /// <summary>
    /// Expands the array to accommodate new elements.
    /// This method does not lock.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Expand()
        // Add at least 10 more buckets, and more depending on how many
        => Array.Resize(ref _buckets, _buckets.Length + _buckets.Length / 10 + 10);

    /// <summary>
    /// Stores the property change event handlers for a given property.
    /// </summary>
    private struct Bucket
    {
        /// <summary>
        /// The handler for the changing event for the observable property this bucket represents, or
        /// <see langword="null"/> if the property does not provide such an event.
        /// </summary>
        /// <remarks>
        /// This is typed as a <see cref="Delegate"/> because this could be a nested or non-nested event.
        /// The <see cref="PropertyChangeEventHandlerCache"/> struct will be responsible for keeping track of which
        /// delegate is used.
        /// </remarks>
        public Delegate? Changing;

        /// <summary>
        /// The handler for the changed event for the observable property this bucket represents, or
        /// <see langword="null"/> if the property does not provide such an event.
        /// </summary>
        /// <remarks>
        /// This is typed as a <see cref="Delegate"/> because this could be a nested or non-nested event.
        /// The <see cref="PropertyChangeEventHandlerCache"/> struct will be responsible for keeping track of which
        /// delegate is used.
        /// </remarks>
        public Delegate? Changed;
    }
}
