// This file is part of Flamenco
// Copyright 2024 Canonical Ltd.
// This program is free software: you can redistribute it and/or modify it under the terms of the
// GNU General Public License version 3, as published by the Free Software Foundation.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
// even the implied warranties of MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with this program.
// If not, see <http://www.gnu.org/licenses/>.

using Microsoft.CodeAnalysis;

namespace Flamenco.Distro.ReleaseInfo.SourceGenerator;

public static class DiagnosticDescriptors
{
    private const string Category = "FlamencoDistroInfoSourceGenerator";
    
    public static readonly DiagnosticDescriptor UbuntuCsvFileNotFound = new (
        id: "FLDISRC001",
        category: Category,
        title: "distro-info ubuntu.csv file not found",
        messageFormat: "No distro-info ubuntu.csv file could be found",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    
    public static readonly DiagnosticDescriptor MultipleUbuntuCsvFilesFound = new (
        id: "FLDISRC002",
        category: Category,
        title: "multiple distro-info ubuntu.csv files found",
        messageFormat: "More than one distro-info ubuntu.csv file were found",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    
    public static readonly DiagnosticDescriptor DebianCsvFileNotFound = new (
        id: "FLDISRC003",
        category: Category,
        title: "distro-info debian.csv file not found",
        messageFormat: "No distro-info debian.csv file could be found",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    
    public static readonly DiagnosticDescriptor MultipleDebianCsvFilesFound = new (
        id: "FLDISRC004",
        category: Category,
        title: "multiple distro-info debian.csv files found",
        messageFormat: "More than one distro-info debian.csv file were found",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    
    public static readonly DiagnosticDescriptor CsvUnreadable = new (
        id: "FLDISRC005",
        category: Category,
        title: "CSV file is unreadable",
        messageFormat: "The CSV file '{0}' could not be read",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    
    public static readonly DiagnosticDescriptor UndefinedColumn = new (
        id: "FLDISRC006",
        category: Category,
        title: "CSV column with undefined name",
        messageFormat: "Row {0} of '{1}' contains more columns than defined in the header",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
    
    public static readonly DiagnosticDescriptor RowIsMissingRequiredColumn = new (
        id: "FLDISRC007",
        category: Category,
        title: "CSV row is missing required column",
        messageFormat: "Row {0} of '{1}' is missing a value for the required column '{2}'",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}

public static class DiagnosticsHelper
{
    public static void ReportUbuntuCsvFileNotFound(this SourceProductionContext context)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.UbuntuCsvFileNotFound,
            Location.None));
    }
    
    public static void ReportMultipleUbuntuCsvFilesFound(this SourceProductionContext context)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.MultipleUbuntuCsvFilesFound,
            Location.None));
    }
    
    public static void ReportDebianCsvFileNotFound(this SourceProductionContext context)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.DebianCsvFileNotFound,
            Location.None));
    }
    
    public static void ReportMultipleDebianCsvFilesFound(this SourceProductionContext context)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.MultipleDebianCsvFilesFound,
            Location.None));
    }
    
    public static void ReportCsvUnreadable(this SourceProductionContext context, string filePath)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.CsvUnreadable,
            Location.None,
            filePath));
    }
    
    public static void ReportUndefinedColumn(this SourceProductionContext context, string filePath, int lineNumber)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.UndefinedColumn,
            Location.None,
            lineNumber, filePath));
    }

    public static void ReportRowIsMissingRequiredColumn(
        this SourceProductionContext context, 
        int lineNumber,
        string filePath, 
        string missingColumn)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.RowIsMissingRequiredColumn ,
            Location.None,
            lineNumber, filePath, missingColumn));
    }
}