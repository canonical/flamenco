using System.Collections.Immutable;
using System.Text;

namespace Flamenco;

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