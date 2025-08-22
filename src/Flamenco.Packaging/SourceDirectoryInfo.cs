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
using Flamenco.Packaging.Dpkg;

namespace Flamenco.Packaging;

public class SourceDirectoryInfo
{
    public static Result<SourceDirectoryInfo> FromDirectory(DirectoryInfo sourceDirectory)
    {
        return ValidateSourceDirectoryExists(sourceDirectory)
            .Then(DiscoverTargets)
            .Then(buildableTargets => new SourceDirectoryInfo(sourceDirectory, buildableTargets));
        
        static Result<DirectoryInfo> ValidateSourceDirectoryExists(DirectoryInfo sourceDirectory)
        {
            return sourceDirectory.Exists 
                ? sourceDirectory
                : new SourceDirectoryNotFound(sourceDirectory);
        }
        
        static Result<BuildTargetCollection> DiscoverTargets(DirectoryInfo sourceDirectory)
        {
            var result = new Result();
            var invalidChangelogFileNames = ImmutableList<Location>.Empty;
            var targetCollection = new BuildTargetCollection();
        
            foreach (var changelogFile in sourceDirectory.EnumerateFiles(searchPattern: "changelog*"))
            {
                var extensions = changelogFile.Name.Split('.')[1..];

                if (extensions.Length != 2)
                {
                    invalidChangelogFileNames = invalidChangelogFileNames.Add(
                        new Location
                        {
                            ResourceLocator = changelogFile.FullName
                        });
                
                    continue;
                }
            
                targetCollection.Add(new BuildTarget(PackageName: extensions[0], SeriesName: extensions[1]));
            }

            if (invalidChangelogFileNames.Count > 0)
            {
                result = result.WithAnnotation(new MalformedChangelogFilenames(invalidChangelogFileNames));
            }
        
            return result.WithValue(targetCollection);
        }
    }

    private SourceDirectoryInfo(
        DirectoryInfo directoryInfo,
        BuildTargetCollection buildableTargets)
    {
        DirectoryInfo = directoryInfo;
        BuildableTargets = buildableTargets;
    }
    
    public DirectoryInfo DirectoryInfo { get; }
    
    public BuildTargetCollection BuildableTargets { get; }

    public async ValueTask<Result<ChangelogEntry>> ReadFirstChangelogEntryAsync(
        BuildTarget buildTarget,
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(
            DirectoryInfo.FullName,
            $"changelog.{buildTarget.PackageName}.{buildTarget.SeriesName}");

        return await DpkgChangelogReader.ReadFirstChangelogEntryAsync(path, cancellationToken).ConfigureAwait(false);
    }
    
    public class SourceDirectoryNotFound(
        DirectoryInfo sourceDirectory) 
    : ErrorBase(
        identifier: "FL0022",
        title: "Source directory not found",
        message: $"The source directory '{sourceDirectory}' could not be found",
        locations: ImmutableList.Create(new Location { ResourceLocator = sourceDirectory.ToString() })) {}
    
    public class MalformedChangelogFilenames(
        ImmutableList<Location> changelogFiles) 
    : AnnotationBase(
        identifier: "FL0023",
        title: "Source directory contains changelogs with a malformed filename",
        message: "Some changelog file(s) do not follow the format 'changelog.PACKAGE.SERIES'",
        severity: AnnotationSeverity.Warning,
        warningLevel: WarningLevels.MajorWarning,
        locations: changelogFiles) {}
}
