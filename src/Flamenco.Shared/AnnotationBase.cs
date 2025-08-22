// This file is part of Flamenco
// Copyright 2024 Canonical Ltd.
// This program is free software: you can redistribute it and/or modify it under the terms of the
// GNU General Public License version 3, as published by the Free Software Foundation.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
// even the implied warranties of MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with this program.
// If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Immutable;

namespace Flamenco;

public abstract class AnnotationBase(
    string identifier, 
    string title, 
    string message, 
    AnnotationSeverity severity,
    uint warningLevel, 
    ImmutableList<Location>? locations = null,
    Uri? helpLink = null, 
    string? description = null,
    ImmutableList<IAnnotation>? innerAnnotations = null,
    ImmutableDictionary<string, object?>? metadata = null)
    : IAnnotation
{
    protected static ImmutableDictionary<string, object?> EmptyMetadata => ImmutableDictionary<string, object?>.Empty;

    /// <inheritdoc />
    public string Identifier { get; } = identifier;

    /// <inheritdoc />
    public string Title { get; } = title;

    /// <inheritdoc />
    public string Message { get; } = message;

    /// <inheritdoc />
    public string? Description { get; } = description;

    /// <inheritdoc />
    public AnnotationSeverity Severity { get; } = severity;

    /// <inheritdoc />
    public uint WarningLevel { get; } = warningLevel;

    /// <inheritdoc />
    public ImmutableList<Location> Locations { get; }
        = locations ?? ImmutableList<Location>.Empty;

    /// <inheritdoc />
    public Uri? HelpLink { get; } = helpLink;

    /// <inheritdoc />
    public ImmutableList<IAnnotation> InnerAnnotations { get; } 
        = innerAnnotations ?? ImmutableList<IAnnotation>.Empty;
    
    /// <inheritdoc />
    public ImmutableList<IAnnotation> InnerErrors { get; } 
        = innerAnnotations
              ?.Where(annotation => annotation.IsError)
              .ToImmutableList() 
          ?? ImmutableList<IAnnotation>.Empty;
    
    /// <inheritdoc />
    public ImmutableList<IAnnotation> InnerWarnings { get; }
        = innerAnnotations
              ?.Where(annotation => annotation.IsWarning)
              .ToImmutableList() 
          ?? ImmutableList<IAnnotation>.Empty;
    
    /// <inheritdoc />
    public ImmutableList<IAnnotation> InnerRemarks { get; }
        = innerAnnotations
              ?.Where(annotation => annotation.IsRemark)
              .ToImmutableList() 
          ?? ImmutableList<IAnnotation>.Empty;
    
    /// <inheritdoc />
    public ImmutableDictionary<string, object?> Metadata { get; } = metadata ?? EmptyMetadata;
    
    protected T FromMetadata<T>(string key) => (T)Metadata[key]!;
}

public abstract class ErrorBase(
    string identifier,
    string title,
    string message,
    ImmutableList<Location>? locations = null,
    Uri? helpLink = null,
    string? description = null,
    ImmutableList<IAnnotation>? innerAnnotations = null,
    ImmutableDictionary<string, object?>? metadata = null)
    : AnnotationBase(
        identifier, 
        title, 
        message, 
        AnnotationSeverity.Error, 
        WarningLevels.Error, 
        locations, 
        helpLink, 
        description, 
        innerAnnotations, 
        metadata)
{
}