using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Diagnostics.CodeAnalysis;

#if NET461 || NETSTANDARD2_0
/// <summary>
/// Specifies that an output may be <see langword="null"/> even if the corresponding type disallows it.
/// </summary>
[ExcludeFromCodeCoverage, DebuggerNonUserCode]
internal sealed class MaybeNullAttribute : Attribute { }

/// <summary>
/// Specifies that <see langword="null"/> is allowed as an input even if the corresponding type disallows it.
/// </summary>
[ExcludeFromCodeCoverage, DebuggerNonUserCode]
internal sealed class AllowNullAttribute : Attribute { }

/// <summary>
/// Specifies that <see langword="null"/> is disallowed as an output even if the corresponding type allows it.
/// </summary>
[ExcludeFromCodeCoverage, DebuggerNonUserCode]
internal sealed class DisallowNullAttribute : Attribute { }

/// <summary>
/// Specifies that an output may be <see langword="null"/> even if the corresponding type disallows it.
/// Specifies that an input argument was not <see langword="null"/> when the call returns.
/// </summary>
[ExcludeFromCodeCoverage, DebuggerNonUserCode]
internal sealed class NotNullAttribute : Attribute { }

/// <summary>
/// Specifies that the output will be non-null if the named parameter is non-null.
/// </summary>
/// <remarks>
/// Internal class for offering nullability attributes in .NET standard and framework.
/// </remarks>
[ExcludeFromCodeCoverage, DebuggerNonUserCode]
internal sealed class NotNullIfNotNullAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the named parameter indicating the nullability of the output.
    /// </summary>
    public string ParameterName { get; }

    public NotNullIfNotNullAttribute(string parameterName)
    {
        ParameterName = parameterName;
    }
}

#if DEBUG
/// <summary>
///     Specifies that the method or property will ensure that the listed field and property members have
///     non-<see langword="null"/> values when returning with the specified return value condition.
/// </summary>
#endif
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
#if !NULLABLE_ATTRIBUTES_INCLUDE_IN_CODE_COVERAGE
[ExcludeFromCodeCoverage, DebuggerNonUserCode]
#endif
internal sealed class MemberNotNullWhenAttribute : Attribute
{
#if DEBUG
    /// <summary>
    ///     Gets the return value condition.
    /// </summary>
#endif
    public bool ReturnValue { get; }

#if DEBUG
    /// <summary>
    ///     Gets field or property member names.
    /// </summary>
#endif
    public string[] Members { get; }

#if DEBUG
    /// <summary>
    ///     Initializes the attribute with the specified return value condition and a field or property member.
    /// </summary>
    /// <param name="returnValue">
    ///     The return value condition. If the method returns this value,
    ///     the associated parameter will not be <see langword="null"/>.
    /// </param>
    /// <param name="member">
    ///     The field or property member that is promised to be not-<see langword="null"/>.
    /// </param>
#endif
    public MemberNotNullWhenAttribute(bool returnValue, string member)
    {
        ReturnValue = returnValue;
        Members = new[] { member };
    }

#if DEBUG
    /// <summary>
    ///     Initializes the attribute with the specified return value condition and list
    ///     of field and property members.
    /// </summary>
    /// <param name="returnValue">
    ///     The return value condition. If the method returns this value,
    ///     the associated parameter will not be <see langword="null"/>.
    /// </param>
    /// <param name="members">
    ///     The list of field and property members that are promised to be not-null.
    /// </param>
#endif
    public MemberNotNullWhenAttribute(bool returnValue, params string[] members)
    {
        ReturnValue = returnValue;
        Members = members;
    }
}
#endif
