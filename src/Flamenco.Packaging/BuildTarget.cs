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
using System.Text;

namespace Flamenco.Packaging;

public record BuildTarget(string PackageName, string SeriesName)
{
    public override string ToString() => $"{PackageName}:{SeriesName}";
}

public class BuildTargetCollection : HashSet<BuildTarget>
{
    public IImmutableSet<string> PackageNames => this.Select(target => target.PackageName).ToImmutableHashSet();
    
    public IImmutableSet<string> SeriesNames => this.Select(target => target.SeriesName).ToImmutableHashSet();
    
    public IImmutableSet<string> GetSeriesOfPackage(string packageName) => 
        this.Where(target => target.PackageName == packageName)
            .Select(target => target.SeriesName)
            .ToImmutableHashSet();
    
    public IImmutableSet<string> GetPackagesOfSeries(string series) => 
        this.Where(target => target.SeriesName == series)
            .Select(target => target.PackageName)
            .ToImmutableHashSet();

    public override string ToString()
    {
        if (Count == 0) return "None";
        
        StringBuilder value = new StringBuilder();

        foreach (var target in this)
        {
            if (value.Length > 0) value.Append(' ');
            
            value.Append(target);
        }

        return value.ToString();
    }
}