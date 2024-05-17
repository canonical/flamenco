using System.Text.RegularExpressions;

namespace Flamenco;

/*
public record Series
{
    // .NET Regex Language reference:
    // https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference#anchors
    private static readonly Regex SeriesNamePattern = new Regex(
        pattern: "\A([a-z]+)\z", 
        options: RegexOptions.Compiled);
    
    public string Name { get; private init; }

    public Series? Parse(string value)
    {
        if (!SeriesNamePattern.IsMatch(value))
        {
            Log.Error($"'{value}' is an invalid series name.");
            return null;
        }
        
        return new Series
        {
            Name = value
        };
    }
}

public record Package
{
    // usable characters for each component in the Debian package names:
    // https://www.debian.org/doc/manuals/debian-reference/ch02.en.html#theusablecharactbianpackagenames
    //
    // .NET Regex Language reference:
    // https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference#anchors
    private static readonly Regex PackageNamePattern = new Regex(
        pattern: "\A([a-z0-9][-a-z0-9.+]+)\z", 
        options: RegexOptions.Compiled);
    public string Name { get; private init; }

    public Package? Parse(string value)
    {
        if (!PackageNamePattern.IsMatch(value))
        {
            Log.Error($"'{value}' is an invalid package name.");
            return null;
        }
        
        return new Package
        {
            Name = value
        };
    }
}
*/