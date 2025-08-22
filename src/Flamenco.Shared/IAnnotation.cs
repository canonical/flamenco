// This file is part of Flamenco
// Copyright 2024 Canonical Ltd.
// This program is free software: you can redistribute it and/or modify it under the terms of the
// GNU General Public License version 3, as published by the Free Software Foundation.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
// even the implied warranties of MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with this program.
// If not, see <http://www.gnu.org/licenses/>.

// NOTE: This is inspired by:
// - FluentResults (Copyright FluentResults); see https://github.com/altmann/FluentResults
// - Roslyn (Copyright .NET Foundation and Contributors); see https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.diagnostic?view=roslyn-dotnet-4.7.0 

using System.Collections.Immutable;

namespace Flamenco;

/// <summary>
/// Describes how severe an <see cref="IAnnotation"/> is.
/// </summary>
public enum AnnotationSeverity
{
    /// <summary>
    /// Information that does not indicate a problem.
    /// </summary>
    Remark,
    
    /// <summary>
    /// A circumstance that is suspicious but that does not interrupt the normal control flow.
    /// </summary>
    Warning,
    
    /// <summary>
    /// A circumstance that interrupts the normal control flow is not recoverable without external intervention.
    /// </summary>
    Error,
}

/// <summary>
/// Represents information that provides further context for an <see cref="Result"/>.
/// </summary>
public interface IAnnotation
{
    /// <summary>
    /// A unique identifier for the type of <see cref="IAnnotation"/>.
    /// </summary>
    string Identifier { get; }
    
    /// <summary>
    /// A short title describing the <see cref="IAnnotation"/> type without context specific content.
    /// </summary>
    string Title { get; }
    
    /// <summary>
    /// A message that describing the <see cref="IAnnotation"/> instance and may contain context specific content.
    /// </summary>
    string Message { get; }
    
    /// <summary>
    /// A message that describing the <see cref="IAnnotation"/> type in detail without context specific content
    /// and may suggest actions to proceed.
    /// </summary>
    string? Description { get; }
    
    /// <summary>
    /// How severe this <see cref="IAnnotation"/> is.
    /// </summary>
    AnnotationSeverity Severity { get; }
    
    /// <summary>
    /// Represents the importance with which the user needs to be notified about this <see cref="IAnnotation"/> instance: 
    /// <list type="bullet" >
    /// <item><c>0</c>: <see cref="AnnotationSeverity.Error"/></item>
    /// <item><c>1</c>: Severe <see cref="AnnotationSeverity.Warning"/></item>
    /// <item><c>2</c>: Major <see cref="AnnotationSeverity.Warning"/></item>
    /// <item><c>3</c>: Minor <see cref="AnnotationSeverity.Warning"/></item>
    /// <item><c>4</c>: Informational</item>
    /// <item><c>5</c>+: Verbose (hidden by default)</item>
    /// </list>
    /// </summary>
    uint WarningLevel { get; }
    
    /// <summary>
    /// <see cref="Location"/> where the <see cref="IAnnotation"/> refers to.
    /// </summary>
    ImmutableList<Location> Locations { get; }
    
    /// <summary>
    /// An optional hyperlink that provides more detailed information regarding the <see cref="IAnnotation"/>.
    /// </summary>
    Uri? HelpLink { get; }
    
    /// <summary>
    /// Collection of all annotations, which are related to this instance.
    /// </summary>
    /// <remarks>
    /// This collection is the union of <see cref="InnerErrors"/>, <see cref="InnerWarnings"/>
    /// and <see cref="InnerRemarks"/>.
    /// </remarks>
    ImmutableList<IAnnotation> InnerAnnotations { get; }
    
    /// <summary>
    /// Subset of <see cref="InnerAnnotations"/> which are <see cref="AnnotationSeverity.Error"/>.
    /// </summary>
    ImmutableList<IAnnotation> InnerErrors { get; }
    
    /// <summary>
    /// Subset of <see cref="InnerAnnotations"/> which are <see cref="AnnotationSeverity.Warning"/>.
    /// </summary>
    ImmutableList<IAnnotation> InnerWarnings { get; }
    
    /// <summary>
    /// Subset of <see cref="InnerAnnotations"/> which are <see cref="AnnotationSeverity.Remark"/>.
    /// </summary>
    ImmutableList<IAnnotation> InnerRemarks { get; }
    
    /// <summary>
    /// A set of name-value pairs that convey more detailed information.
    /// </summary>
    ImmutableDictionary<string, object?> Metadata { get; }

    /// <summary>
    /// <see langword="true"/> if <see cref="Severity"/> is an <see cref="AnnotationSeverity.Error"/>;
    /// otherwise <see langword="false"/>.
    /// </summary>
    bool IsError => Severity == AnnotationSeverity.Error;
    
    /// <summary>
    /// <see langword="true"/> if <see cref="Severity"/> is a <see cref="AnnotationSeverity.Warning"/>;
    /// otherwise <see langword="false"/>.
    /// </summary>
    bool IsWarning => Severity == AnnotationSeverity.Warning;
    
    /// <summary>
    /// <see langword="true"/> if <see cref="Severity"/> is a <see cref="AnnotationSeverity.Remark"/>;
    /// otherwise <see langword="false"/>.
    /// </summary>
    bool IsRemark => Severity == AnnotationSeverity.Remark;
}
