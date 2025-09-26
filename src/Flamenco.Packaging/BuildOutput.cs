namespace Flamenco.Packaging;

public enum BuildOutput
{
    /// <summary>
    /// Only build the debian directory of the source tree.
    /// Does not search for a .orig tarball. 
    /// </summary>
    DebianDirectoryOnly,
    
    /// <summary>
    /// Only build the debian source tree, but does not invoke
    /// dpkg-buildpackage to build the source package.
    /// </summary>
    DebianSourceTreeOnly,
    
    /// <summary>
    /// Builds the complete debian source tree and invokes dpkg-buildpackage
    /// to build the source package. The generated .changes file will include
    /// the .orig traball.
    /// </summary>
    SourcePackageIncludingOrigTarball,
    
    /// <summary>
    /// Builds the complete debian source tree and invokes dpkg-buildpackage
    /// to build the source package. The generated .changes file will exclude
    /// the .orig traball.
    /// </summary>
    SourcePackageExcludingOrigTarball,
}