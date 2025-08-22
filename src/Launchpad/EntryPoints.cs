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

namespace Canonical.Launchpad;

public enum LaunchpadEnvironment
{
    Unknown,
    Production,
    Staging,
    QaStaging,
    Dogfooding,
    Development,
    DevelopmentTesting,
}

public readonly record struct ApiEntryPoint(
    string Name,
    string RootUri,
    LaunchpadEnvironment Environment)
{
    public override string ToString() => $"{Name} ({RootUri})";
}

public static class ApiEntryPoints
{
    public static readonly ApiEntryPoint Production = new(
        Name: "Launchpad Production API Entry Point",
        RootUri: "https://api.launchpad.net/",
        Environment: LaunchpadEnvironment.Production);
    
    public static readonly ApiEntryPoint Staging = new(
        Name: "Launchpad Staging API Entry Point",
        RootUri: "https://api.staging.launchpad.net/",
        Environment: LaunchpadEnvironment.Staging);
    
    public static readonly ApiEntryPoint QaStaging = new(
        Name: "Launchpad QA Staging API Entry Point",
        RootUri: "https://api.qastaging.launchpad.net/",
        Environment: LaunchpadEnvironment.QaStaging);
    
    public static readonly ApiEntryPoint Dogfood = new(
        Name: "Launchpad Dogfooding API Entry Point",
        RootUri: "https://api.dogfood.paddev.net/",
        Environment: LaunchpadEnvironment.Dogfooding);
    
    public static readonly ApiEntryPoint Development = new(
        Name: "Launchpad Development API Entry Point",
        RootUri: "https://api.launchpad.test/",
        Environment: LaunchpadEnvironment.Development);
    
    public static readonly ApiEntryPoint DevelopmentTesting = new(
        Name: "Launchpad Development Testing API Entry Point",
        RootUri: "http://api.launchpad.test:8085/",
        Environment: LaunchpadEnvironment.DevelopmentTesting);

    public static readonly ImmutableArray<ApiEntryPoint> All =
        [ Production, Staging, QaStaging, Dogfood, Development, DevelopmentTesting ];
}

public readonly record struct WebEntryPoint(
    string Name,
    string RootUri,
    LaunchpadEnvironment Environment)
{
    public override string ToString() => $"{Name} ({RootUri})";
}

public static class WebEntryPoints
{
    public static readonly WebEntryPoint Production = new(
        Name: "Launchpad Production Web Entry Point",
        RootUri: "https://launchpad.net/",
        Environment: LaunchpadEnvironment.Production);
    
    public static readonly WebEntryPoint Staging = new(
        Name: "Launchpad Staging Web Entry Point",
        RootUri: "https://staging.launchpad.net/",
        Environment: LaunchpadEnvironment.Staging);
    
    public static readonly WebEntryPoint QaStaging = new(
        Name: "Launchpad QA Staging Web Entry Point",
        RootUri: "https://qastaging.launchpad.net/",
        Environment: LaunchpadEnvironment.QaStaging);
    
    public static readonly WebEntryPoint Dogfood = new(
        Name: "Launchpad Dogfooding Web Entry Point",
        RootUri: "https://dogfood.paddev.net/",
        Environment: LaunchpadEnvironment.Dogfooding);
    
    public static readonly WebEntryPoint Development = new(
        Name: "Launchpad Development Web Entry Point",
        RootUri: "https://launchpad.test/",
        Environment: LaunchpadEnvironment.Development);
    
    public static readonly WebEntryPoint DevelopmentTesting = new(
        Name: "Launchpad Development Testing Web Entry Point",
        RootUri: "http://launchpad.test:8085/",
        Environment: LaunchpadEnvironment.DevelopmentTesting);

    public static readonly ImmutableArray<WebEntryPoint> All = 
        [ Production, Staging, QaStaging, Dogfood, Development, DevelopmentTesting ];
}