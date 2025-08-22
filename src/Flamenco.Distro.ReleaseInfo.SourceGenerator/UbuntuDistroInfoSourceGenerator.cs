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
using Microsoft.CodeAnalysis;
using static Microsoft.CodeAnalysis.CSharp.SymbolDisplay;

namespace Flamenco.Distro.ReleaseInfo.SourceGenerator;

[Generator]
public class UbuntuDistroInfoSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var ubuntuCsvFile = context.AdditionalTextsProvider
            .Where(additionalText => Path.GetFileName(additionalText.Path).Equals("ubuntu.csv"))
            .Collect();
        
        context.RegisterSourceOutput(ubuntuCsvFile, GenerateSource);
    }
    
    private static void GenerateSource(
        SourceProductionContext context, 
        ImmutableArray<AdditionalText> ubuntuCsvFiles)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        
        switch (ubuntuCsvFiles.Length)
        {
            case 0:
                context.ReportUbuntuCsvFileNotFound();
                return;
            case > 1:
                context.ReportMultipleUbuntuCsvFilesFound();
                return;
        }

        var stringBuilder = new StringBuilder();
        
        string path = ubuntuCsvFiles[0].Path;
        var names = new List<string>();
        foreach ((int lineNumber, var row) in CsvReader.ReadRows(ubuntuCsvFiles[0], context))
        {
            if (!row.TryGetValue("version", out var version) || string.IsNullOrWhiteSpace(version))
            {
                context.ReportRowIsMissingRequiredColumn(lineNumber, path, missingColumn: "version");
                return;
            }

            bool isLts = false;
            if (version.EndsWith(" LTS"))
            {
                isLts = true;
                version = version.Substring(startIndex: 0, length: version.Length - 4);
            }
            
            if (!row.TryGetValue("codename", out var codename)|| string.IsNullOrWhiteSpace(codename))
            {
                context.ReportRowIsMissingRequiredColumn(lineNumber, path, missingColumn: "codename");
                return;
            }
            
            if (!row.TryGetValue("series", out var series) || string.IsNullOrWhiteSpace(series))
            {
                context.ReportRowIsMissingRequiredColumn(lineNumber, path, missingColumn: "series");
                return;
            }
            
            if (!row.TryGetValue("created", out var created) || string.IsNullOrWhiteSpace(created))
            {
                context.ReportRowIsMissingRequiredColumn(lineNumber, path, missingColumn: "created");
                return;
            }
            
            if (!row.TryGetValue("release", out var release) || string.IsNullOrWhiteSpace(release))
            {
                context.ReportRowIsMissingRequiredColumn(lineNumber, path, missingColumn: "release");
                return;
            }
            
            if (!row.TryGetValue("eol", out var eol) || string.IsNullOrWhiteSpace(eol))
            {
                context.ReportRowIsMissingRequiredColumn(lineNumber, path, missingColumn: "eol");
                return;
            }

            row.TryGetValue("eol-server", out var eolServer);

            if (eol.Equals(eolServer))
            {
                eolServer = null;
            }
            
            row.TryGetValue("eol-esm", out var eolEsm);

            string name = codename.Replace(" ", "");
            names.Add(name);

            stringBuilder.Clear();
            stringBuilder.Append("Ubuntu ").Append(version);
            if (isLts) stringBuilder.Append(" LTS");
            stringBuilder.Append(" (").Append(codename).Append(')');
            
            context.AddSource(hintName: $"UbuntuReleases.{name}.g.cs", $$"""
                // This file is part of Flamenco
                // Copyright 2024 Canonical Ltd.
                // This program is free software: you can redistribute it and/or modify it under the terms of the
                // GNU General Public License version 3, as published by the Free Software Foundation.
                // This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
                // even the implied warranties of MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR PURPOSE.
                // See the GNU General Public License for more details.
                // You should have received a copy of the GNU General Public License along with this program.
                // If not, see <http://www.gnu.org/licenses/>.
                
                namespace Flamenco.Distro.ReleaseInfo;
                
                public static partial class UbuntuReleases
                {
                    public static UbuntuRelease {{name}} = new UbuntuRelease( 
                        version: {{FormatLiteral(version, quote: true)}},
                        isLts: {{(isLts ? "true" : "false")}},
                        codename: {{FormatLiteral(codename, quote: true)}},
                        series: {{FormatLiteral(series, quote: true)}},
                        created: {{created.AsDateOnlyLiteral()}},
                        released: {{release.AsDateOnlyLiteral()}},
                        endOfStandardSupport: {{eol.AsDateOnlyLiteral()}},
                        endOfServerStandardSupport: {{eolServer.AsDateOnlyLiteral()}},
                        endOfExpandedSecurityMaintenance: {{eolEsm.AsDateOnlyLiteral()}},
                        endOfLife: {{(eolEsm ?? eolServer ?? eol).AsDateOnlyLiteral()}},
                        stringRepresentation: {{FormatLiteral(stringBuilder.ToString(), quote: true)}});
                }                                
                """);
        }

        context.CancellationToken.ThrowIfCancellationRequested();

        stringBuilder.Clear();
        stringBuilder.AppendLine($$"""
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
            
            namespace Flamenco.Distro.ReleaseInfo;
            
            public static partial class UbuntuReleases
            {
                public static readonly ImmutableArray<UbuntuRelease> All = Initialize();
                
                private static ImmutableArray<UbuntuRelease> Initialize()
                {
                    var all = ImmutableArray.CreateBuilder<UbuntuRelease>();
            """);
        
        foreach (var name in names)
        {
            stringBuilder.Append("        all.Add(").Append(name).AppendLine(");");
        }
        
        stringBuilder.AppendLine("""
                    return all.ToImmutable();
                } 
            }
            """);
        
        context.AddSource(hintName: "UbuntuReleases.g.cs", stringBuilder.ToString());
    }
}