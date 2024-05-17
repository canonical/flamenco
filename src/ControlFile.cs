namespace Flamenco;

class ControlFile
{
    private const string SourcePackagePrefix = "Source:";
    private const string BinaryPackagePrefix = "Package:";
    
    private readonly FileInfo  _controlFile;
    private readonly Lazy<IEnumerable<string>> _binaryPackages;

    public ControlFile(string path)
    {
        _controlFile = new FileInfo(path);

        if (!_controlFile.Exists)
        {
            throw new FileNotFoundException(message: "Could not find control file at specified path.", fileName: path);
        }
        
        _binaryPackages = new Lazy<IEnumerable<string>>(ParseBinaryPackages);
    }

    public string Path => _controlFile.FullName;

    public string SourcePackage => throw new NotImplementedException();

    public IEnumerable<string> BinaryPackages => _binaryPackages.Value;

    private IEnumerable<string> ParseBinaryPackages()
    {
        using var fileContent = _controlFile.OpenText();

        var binaryPackages = new List<string>();
        
        while (true)
        {
            var line = fileContent.ReadLine();
            
            if (line is null) break;
            if (!line.StartsWith(BinaryPackagePrefix)) continue;

            var binaryPackage = line.Substring(BinaryPackagePrefix.Length).Trim();
            binaryPackages.Add(binaryPackage);
        }

        return binaryPackages;
    }
}