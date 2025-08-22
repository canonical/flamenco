// This file is part of Flamenco
// Copyright 2024 Canonical Ltd.
// This program is free software: you can redistribute it and/or modify it under the terms of the
// GNU General Public License version 3, as published by the Free Software Foundation.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
// even the implied warranties of MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with this program.
// If not, see <http://www.gnu.org/licenses/>.

// NOTE: This is remotely inspired by Roslyn (Copyright .NET Foundation and Contributors).
// See: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/errors-warnings#warninglevel

namespace Flamenco;

public static class WarningLevels
{
    /// <summary>
    /// <see cref="AnnotationSeverity.Error"/>
    /// </summary>
    public const uint Error = 0;
    
    /// <summary>
    /// Severe <see cref="AnnotationSeverity.Warning"/>
    /// </summary>
    public const uint SevereWarning = 1;
    
    /// <summary>
    /// Major <see cref="AnnotationSeverity.Warning"/>
    /// </summary>
    public const uint MajorWarning = 2;
    
    /// <summary>
    /// Minor <see cref="AnnotationSeverity.Warning"/>
    /// </summary>
    public const uint MinorWarning = 3;
    
    /// <summary>
    /// Informational
    /// </summary>
    public const uint Informational = 4;
    
    /// <summary>
    /// Verbose (hidden by default)
    /// </summary>
    public const uint Verbose = 5;
}