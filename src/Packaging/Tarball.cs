using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Flamenco.Packaging;

public static class Tarball
{
    public class StartingProcessFailedException(Process process, string humanFriendlyName, Exception innerException)
        : Exception(
            message: $"Starting the process '{humanFriendlyName}' unexpectedly failed " +
                     $"(executable path: '{process.StartInfo.FileName}'; "+
                     $"arguments: '{string.Join(separator: ' ', values: process.StartInfo.ArgumentList)}'). " +
                     "See the inner exception for more details.",
            innerException)
    {
        public string Executable { get; } = process.StartInfo.FileName;
        public Collection<string> ArgumentList { get; } = process.StartInfo.ArgumentList;
    }
    
    public class ProcessExistsUnsuccessfulException(Process process, string humanFriendlyName)
        : Exception(message: $"The process '{humanFriendlyName}' exited with a non-zero exit code " +
                             $"'{process.ExitCode}' which indicates a fatal error " +
                             $"(executable path: '{process.StartInfo.FileName}'; "+
                             $"arguments: '{string.Join(separator: ' ', values: process.StartInfo.ArgumentList)}').")
    {
        public int StatusCode { get; } = process.ExitCode;
        public string Executable { get; } = process.StartInfo.FileName;
        public Collection<string> ArgumentList { get; } = process.StartInfo.ArgumentList;
    }
    
    public enum CompressionMethod
    {
        // ReSharper disable once InconsistentNaming
        XZ = 2, 
        GZip = 1,
        None = 0
    }
    
    public static async Task CreateTarArchiveAsync(
        FileInfo archiveFile,
        DirectoryInfo archiveRoot,
        IEnumerable<string> includedPaths,
        CompressionMethod compressionMethod = CompressionMethod.None,
        CancellationToken cancellationToken = default)
    {
        if (archiveFile.Exists) throw new IOException(
            message: $"A file with the specified path '{archiveFile.FullName}' for the resulting archive already exists.");
        
        if (!archiveRoot.Exists) throw new DirectoryNotFoundException(
            message: $"No directory could be found at the specified path '{archiveRoot.FullName}' for the archive root.");
        
        ProcessStartInfo tarProcessInfo = new ProcessStartInfo
        {
            FileName = "/usr/bin/env",
            ArgumentList = { "tar", "--create", "--file", archiveFile.FullName, "--directory", archiveRoot.FullName },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        switch (compressionMethod)
        {
            case CompressionMethod.XZ:
                tarProcessInfo.ArgumentList.Add("--xz");
                break;
            case CompressionMethod.GZip:
                tarProcessInfo.ArgumentList.Add("--gzip");
                break;
        }
        
        foreach (var file in includedPaths)   
        {
            tarProcessInfo.ArgumentList.Add(file);
        }

        using var tarProcess = new Process();

        try
        {
            tarProcess.StartInfo = tarProcessInfo;
            tarProcess.OutputDataReceived += HandleStandardOutput;
            tarProcess.ErrorDataReceived += HandleErrorOutput;

            tarProcess.Start();
            tarProcess.BeginOutputReadLine();
            tarProcess.BeginErrorReadLine();
            await tarProcess.WaitForExitAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            throw new StartingProcessFailedException(tarProcess, humanFriendlyName: "create tar archive", exception);
        }

        if (tarProcess.ExitCode != 0)
        {
            throw new ProcessExistsUnsuccessfulException(tarProcess, humanFriendlyName: "create tar archive");
        }
    }

    private static void HandleStandardOutput(object sendingProcess, DataReceivedEventArgs output)
    {
        if (output.Data == null) return;
        Log.Debug(message: "tar: " + output.Data);
    }
    
    private static void HandleErrorOutput(object sendingProcess, DataReceivedEventArgs output)
    {
        if (output.Data == null) return;
        Log.Error(message: "tar: " + output.Data);
    }

    public static string CompressionMethodExtension(CompressionMethod tarballCompressionMethod) =>
        tarballCompressionMethod switch
        {
            CompressionMethod.XZ => ".xz",
            CompressionMethod.GZip => ".gz",
            CompressionMethod.None => string.Empty,
            _ => throw new NotImplementedException()
        };
}