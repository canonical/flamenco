namespace Flamenco.Packaging;

internal sealed class FlamencoFileInfo(bool flamencoFile, FileInfo fileInfo)
{
    public bool FlamencoFile { get; } = flamencoFile;
    public FileInfo FileInfo { get; } = fileInfo;
}
