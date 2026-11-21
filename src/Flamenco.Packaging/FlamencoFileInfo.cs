namespace Flamenco.Packaging;

internal sealed class FlamencoFileInfo(bool isFlamencoFile, FileInfo fileInfo)
{
    public bool IsFlamencoFile { get; } = isFlamencoFile;
    public FileInfo FileInfo { get; } = fileInfo;
}
