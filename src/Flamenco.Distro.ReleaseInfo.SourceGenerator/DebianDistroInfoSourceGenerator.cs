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
public class DebianDistroInfoSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var debianCsvFile = context.AdditionalTextsProvider
            .Where(additionalText => Path.GetFileName(additionalText.Path).Equals("debian.csv"))
            .Collect();
        
        context.RegisterSourceOutput(debianCsvFile, GenerateSource);
    }
    
    private static void GenerateSource(
        SourceProductionContext context, 
        ImmutableArray<AdditionalText> debianCsvFiles)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        
        if (debianCsvFiles.Length == 0)
        {
            context.ReportDebianCsvFileNotFound();
            return;
        }
        
        if (debianCsvFiles.Length > 1)
        {
            context.ReportMultipleDebianCsvFilesFound();
            return;
        }

        var stringBuilder = new StringBuilder();
        
        string path = debianCsvFiles[0].Path;
        var names = new List<string>();
        foreach (var (lineNumber, row) in CsvReader.ReadRows(debianCsvFiles[0], context))
        {
            if (!row.TryGetValue("version", out var version))
            {
                context.ReportRowIsMissingRequiredColumn(lineNumber, path, missingColumn: "version");
                return;
            }
            else if (string.IsNullOrWhiteSpace(version))
            {
                version = null;
            }

            if (!row.TryGetValue("codename", out var codename) || string.IsNullOrWhiteSpace(codename))
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

            row.TryGetValue("release", out var release);
            bool isStable = !string.IsNullOrWhiteSpace(release);
            row.TryGetValue("eol", out var eol);
            row.TryGetValue("eol-lts", out var eolLts);
            row.TryGetValue("eol-elts", out var eolELts);

            string name = codename.Replace(" ", "");
            names.Add(name);

            stringBuilder.Clear();
            stringBuilder.Append("Debian ");
            if (!string.IsNullOrWhiteSpace(version)) stringBuilder.Append(version).Append(' ');
            stringBuilder.Append('(').Append(codename).Append(')');
            
            context.AddSource(hintName: $"DebianReleases.{name}.g.cs", $$"""
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
                
                public static partial class DebianReleases
                {
                    public static DebianRelease {{name}} = new DebianRelease( 
                        version: {{( version is null ? "null" : FormatLiteral(version, quote: true))}},
                        isStable: {{(isStable ? "true" : "false")}},
                        codename: {{FormatLiteral(codename, quote: true)}},
                        series: {{FormatLiteral(series, quote: true)}},
                        created: {{created.AsDateOnlyLiteral()}},
                        released: {{release.AsDateOnlyLiteral()}},
                        endOfStandardSupport: {{eol.AsDateOnlyLiteral()}},
                        endOfLongTermSupport: {{eolLts.AsDateOnlyLiteral()}},
                        endOfExtendedLongTermSupport: {{eolELts.AsDateOnlyLiteral()}},
                        endOfLife: {{ (eolELts ?? eolLts ?? eol).AsDateOnlyLiteral()}},
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
            
            public static partial class DebianReleases
            {
                public static readonly ImmutableArray<DebianRelease> All = Initialize();
                
                private static ImmutableArray<DebianRelease> Initialize()
                {
                    var all = ImmutableArray.CreateBuilder<DebianRelease>();
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
        
        context.AddSource(hintName: "DebianReleases.g.cs", stringBuilder.ToString());
    }
}
