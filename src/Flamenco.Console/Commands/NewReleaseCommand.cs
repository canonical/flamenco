// This file is part of Flamenco
// Copyright 2025 Canonical Ltd.
// This program is free software: you can redistribute it and/or modify it under the terms of the
// GNU General Public License version 3, as published by the Free Software Foundation.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
// even the implied warranties of MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with this program.
// If not, see <http://www.gnu.org/licenses/>.

using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

namespace Flamenco.Console.Commands;

public class NewReleaseCommand : Command
{
    public NewReleaseCommand() : base(
        name: "new-release",
        description: "Prepare the changelogs for a new release.")
    {
        AddOption(CommonOptions.SourceDirectoryOption);
        Handler = CommandHandler.Create(Run);
    }

    private static async Task<int> Run(
        DirectoryInfo? sourceDirectory,
        CancellationToken cancellationToken = default)
    {
        if (!EnvironmentVariables.TryGetSourceDirectoryInfoFromEnvironmentOrDefaultIfNull(ref sourceDirectory))
        {
            return -1;
        }

        Log.Debug("Source Directory: " + sourceDirectory.FullName);

        return 0;
    }
}
