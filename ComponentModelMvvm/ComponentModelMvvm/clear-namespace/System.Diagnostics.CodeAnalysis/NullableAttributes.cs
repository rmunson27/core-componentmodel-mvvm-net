﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Diagnostics.CodeAnalysis;

#if NET461 || NETSTANDARD2_0
/// <summary>
/// Specifies that an output may be <see langword="null"/> even if the corresponding type disallows it.
/// </summary>
internal sealed class MaybeNullAttribute : Attribute { }

#if false
/// <summary>
/// Specifies that the output will be non-null if the named parameter is non-null.
/// </summary>
/// <remarks>
/// Internal class for offering nullability attributes in .NET standard and framework.
/// </remarks>
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
#endif
#endif
