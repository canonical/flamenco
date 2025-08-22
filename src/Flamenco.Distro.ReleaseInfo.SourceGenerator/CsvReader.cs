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
using Microsoft.CodeAnalysis;

namespace Flamenco.Distro.ReleaseInfo.SourceGenerator;

public static class CsvReader
{
    public static IEnumerable<(int LineNumber, ImmutableDictionary<string, string> Row)> ReadRows(
        AdditionalText csv, 
        SourceProductionContext context)
    {
        var sourceText = csv.GetText(context.CancellationToken);

        if (sourceText is null)
        {
            context.ReportCsvUnreadable(csv.Path);
            yield break;
        }
        
        var textReader = new StringReader(sourceText.ToString());

        string? line = textReader.ReadLine();
        var columns = ReadColumns(line);
        
        var row = ImmutableDictionary.CreateBuilder<string, string>();
        for (int lineNumber = 1; textReader.ReadLine() is { } currentLine; ++lineNumber)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            int columnIndex = 0;
            foreach (var value in currentLine.Split(','))    
            {
                if (columnIndex < columns.Length)
                {
                    row.Add(columns[columnIndex++], value);
                }
                else
                {
                    context.ReportUndefinedColumn(csv.Path, lineNumber);
                    break;
                }
            }

            yield return (lineNumber, row.ToImmutable());
            row.Clear();
        }
    }

    private static ImmutableArray<string> ReadColumns(string? header)
    {
        if (header is null) return ImmutableArray<string>.Empty;
        return ImmutableArray.Create(header.Split(','));
    }
}