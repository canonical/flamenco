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
using System.Globalization;

namespace Flamenco.Packaging.Dpkg;

public readonly record struct ChangelogEntry(
    DpkgName PackageName,
    DpkgVersion Version,
    ImmutableArray<DpkgSuite> Distributions,
    ImmutableDictionary<string, string> Metadata,
    string Description,
    MaintainerInfo Maintainer,
    DateTimeOffset Date,
    Location Location = default)
{
    /// <summary>
    /// The date-time format of a changelog entry trailer, as defined in the man page deb-changelog(5).
    /// </summary>
    /// <remarks>
    /// Used for both parsing and formatting. Note that <c>zzz</c> parses both <c>+0000</c> and
    /// <c>+00:00</c>, but always formats as <c>+00:00</c>, while deb-changelog(5) requires
    /// <c>+0000</c>. <see cref="ToChangelogString"/> therefore removes the colon again.
    /// </remarks>
    public const string DateFormat = "ddd, dd MMM yyyy HH':'mm':'ss zzz";
    
    public string? Urgency => CollectionExtensions.GetValueOrDefault(Metadata, key: "urgency");
    
    public string? BinaryOnly => CollectionExtensions.GetValueOrDefault(Metadata, key: "binary-only");
    
    /// <summary>
    /// Serializes this entry into the changelog file format defined by the man page deb-changelog(5).
    /// </summary>
    /// <returns>
    /// The textual representation of this changelog entry, including a trailing line break, so that
    /// entries can be concatenated to form a changelog file.
    /// </returns>
    /// <remarks>
    /// <see cref="Description"/> is emitted verbatim. It is expected to begin and end with a line
    /// break, which is how <see cref="DpkgChangelogReader"/> reports it.
    /// </remarks>
    public string ToChangelogString()
    {
        // ponytail: Metadata is unordered, so multiple keys serialize in arbitrary order. Wrap in
        // OrderBy(key) if an entry ever carries more than the usual single 'urgency' key.
        var metadata = string.Join(separator: ", ", Metadata.Select(entry => $"{entry.Key}={entry.Value}"));
        var distributions = string.Join(separator: ' ', Distributions);
        var date = Date.ToString(DateFormat, CultureInfo.InvariantCulture);
        
        // "Thu, 03 Sep 2026 12:19:04 +02:00" -> "Thu, 03 Sep 2026 12:19:04 +0200"
        date = string.Concat(date.AsSpan(start: 0, length: date.Length - 3), date.AsSpan(start: date.Length - 2));
        
        return $"{PackageName} ({Version}) {distributions}; {metadata}\n" +
               Description +
               $" -- {Maintainer.Name} <{Maintainer.EmailAddress}>  {date}\n";
    }
}

public readonly record struct MaintainerInfo(string Name, string EmailAddress);