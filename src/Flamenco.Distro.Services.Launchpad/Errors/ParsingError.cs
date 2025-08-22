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

namespace Flamenco.Distro.Services.Launchpad.Errors;

public class ParsingError(
    Uri? uri,
    Exception? exception = null,
    ImmutableDictionary<string, object?>? metadata = null)
    : AnnotationBase(
        identifier: "FL0103",
        title: "Response parsing error",
        message: "The response content could not get parsed.",
        severity: AnnotationSeverity.Error,
        warningLevel: WarningLevels.Error,
        metadata: (metadata ?? ImmutableDictionary<string, object?>.Empty)
        .Add(key: nameof(Uri), value: uri)
        .Add(key: nameof(Exception), value: exception))
{
    public Uri? Uri => (Uri?)Metadata[nameof(Uri)];
    public Exception? Exception => (Exception?)Metadata[nameof(Exception)];
}
