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

public class OperationCanceled(
    ImmutableList<Location>? locations = null,
    ImmutableDictionary<string, object?>? metadata = null) 
    : ErrorBase(
    identifier: "FL9000",
    title: "Operation cancelled",
    message: "The current operation was canceled.",
    locations: locations,
    helpLink: new ("https://learn.microsoft.com/en-us/dotnet/api/system.operationcanceledexception?view=net-8.0"),
    metadata: metadata)
{
}

public class ExceptionalAnnotation(
    Exception exception,
    ImmutableList<Location>? locations = null,
    ImmutableDictionary<string, object?>? metadata = null)
    : AnnotationBase(
    identifier: "FL9001",
    title: exception.GetType().FullName ?? nameof(System.Exception),
    message: exception.Message,
    description: "This error represents a captured exception.",
    severity: AnnotationSeverity.Error,
    warningLevel: WarningLevels.Error,
    locations: locations,
    helpLink: new ("https://learn.microsoft.com/en-us/dotnet/api/system.operationcanceledexception?view=net-8.0"),
    metadata: (metadata ?? ImmutableDictionary<string, object?>.Empty)
              .Add(key: nameof(Exception), value: exception))
{
    public Exception Exception => (Exception)Metadata[nameof(Exception)]!;
}