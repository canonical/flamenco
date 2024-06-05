using System.Diagnostics;
using Flamenco.Packaging;
using Flamenco.Packaging.Dpkg;

namespace Flamenco.Packaging.Dpkg;

public class DebVersionTests
{
    [Fact]
    public void ToStringOf_EmptyDebVersion_Returns_EmptyString()
    {
        Assert.Equal(expected: string.Empty, actual: DebVersion.Empty.ToString());
        Assert.Null(DebVersion.Empty.Epoch);
        Assert.Equal(expected: 0u, actual: DebVersion.Empty.EpochValue);
        Assert.Equal(expected: string.Empty, actual: DebVersion.Empty.UpstreamVersion);
        Assert.Equal(expected: string.Empty, actual: DebVersion.Empty.EffectiveUpstreamVersion);
        Assert.Null(DebVersion.Empty.RevertedUpstreamVersion);
        Assert.Null(DebVersion.Empty.RealUpstreamVersion);
        Assert.Null(DebVersion.Empty.Revision);
        Assert.Null(DebVersion.Empty.DebianRevision);
        Assert.Null(DebVersion.Empty.UbuntuRevision);
    }
    
    [Fact]
    public void Parsing_EmptyString_Works()
    {
        DebVersion.Parse(string.Empty);
    }
    
    [Theory]
    [InlineData("a:1")]
    public void Parsing_InvalidVersionString_Fails(string invalidVersionString)
    {
        Assert.Throws<FormatException>(testCode: () => DebVersion.Parse(invalidVersionString));
    }
    
     [Theory]
     [InlineData("1", null, 0u, "1", "1", null, null, null, null, null)]
     [InlineData("1-1", null, 0u, "1", "1", null, null, "1", "1", null)]
     [InlineData("1-1ubuntu1", null, 0u, "1", "1", null, null, "1ubuntu1", "1", "1")]
     [InlineData("1-1-1ubuntu1", null, 0u, "1-1", "1-1", null, null, "1ubuntu1", "1", "1")]
     [InlineData("2+really1-1ubuntu1", null, 0u, "2+really1", "1", "2", "1", "1ubuntu1", "1", "1")]
     [InlineData("1:1", "1", 1u, "1", "1", null, null, null, null, null)]
     [InlineData("1:1-1", "1", 1u, "1", "1", null, null, "1", "1", null)]
     [InlineData("1:1-1ubuntu1", "1", 1u, "1", "1", null, null, "1ubuntu1", "1", "1")]
     [InlineData("1:1-1-1ubuntu1", "1", 1u, "1-1", "1-1", null, null, "1ubuntu1", "1", "1")]
     [InlineData("1:1:1-1:1:-1-1ubuntu1", "1", 1u, "1:1-1:1:-1", "1:1-1:1:-1", null, null, "1ubuntu1", "1", "1")]
     [InlineData("1:2+really1-1ubuntu1", "1", 1u, "2+really1", "1", "2", "1", "1ubuntu1", "1", "1")]
     [InlineData("001:1", "001", 1u, "1", "1", null, null, null, null, null)]
     [InlineData("9999:1", "9999", 9999u, "1", "1", null, null, null, null, null)]
     [InlineData("0009999:1", "0009999", 9999u, "1", "1", null, null, null, null, null)]
     [InlineData("999.999.999~preview999.999-999~prefix999ubuntu999~postfix999", null, 0u, "999.999.999~preview999.999", "999.999.999~preview999.999", null, null, "999~prefix999ubuntu999~postfix999", "999~prefix999", "999~postfix999")]
     [InlineData("1:8.0.104-8.0.4-1", "1", 1u, "8.0.104-8.0.4", "8.0.104-8.0.4", null, null, "1", "1", null)]
     [InlineData("1:6.0.129", "1", 1u, "6.0.129", "6.0.129", null, null, null, null, null)]
     [InlineData("1:8.0.104-8.0.4-0ubuntu1~23.10.1", "1", 1u, "8.0.104-8.0.4", "8.0.104-8.0.4", null, null, "0ubuntu1~23.10.1", "0", "1~23.10.1")]
     [InlineData("1:6.0.129-0ubuntu1~22.04.1", "1", 1u, "6.0.129", "6.0.129", null, null, "0ubuntu1~22.04.1", "0", "1~22.04.1")]
     [InlineData("6.0.129-0ubuntu1~22.04.1", null, 0u, "6.0.129", "6.0.129", null, null, "0ubuntu1~22.04.1", "0", "1~22.04.1")]
     [InlineData("8.0.104-8.0.4-0ubuntu1~23.10.1", null, 0u, "8.0.104-8.0.4", "8.0.104-8.0.4", null, null, "0ubuntu1~23.10.1", "0", "1~23.10.1")]
     [InlineData("8.0.104", null, 0u, "8.0.104", "8.0.104", null, null, null, null, null)]
     [InlineData("8.0.104+8.0.4", null, 0u, "8.0.104+8.0.4", "8.0.104+8.0.4", null, null, null, null, null)]
     [InlineData("1:999.999.999~preview999.999", "1", 1u, "999.999.999~preview999.999", "999.999.999~preview999.999", null, null, null, null, null)]
     [InlineData("1:999.999.999~preview999.999-999~prefix999ubuntu999~postfix999", "1", 1u, "999.999.999~preview999.999", "999.999.999~preview999.999", null, null, "999~prefix999ubuntu999~postfix999", "999~prefix999", "999~postfix999")]
     [InlineData("999.999.999~preview999.999", null, 0u, "999.999.999~preview999.999", "999.999.999~preview999.999", null, null, null, null, null)]
     [InlineData("8.0.100-8.0.0~rc1+really7.0.100-7.0.0~beta1~bootstrap+amd64-0ubuntu1", null, 0u, "8.0.100-8.0.0~rc1+really7.0.100-7.0.0~beta1~bootstrap+amd64", "7.0.100-7.0.0~beta1~bootstrap+amd64", "8.0.100-8.0.0~rc1", "7.0.100-7.0.0~beta1~bootstrap+amd64", "0ubuntu1", "0", "1")]
     [InlineData("8.0.100-8.0.0~rc1-0ubuntu1~test1ubuntu2ubuntu3", null, 0u, "8.0.100-8.0.0~rc1", "8.0.100-8.0.0~rc1", null, null, "0ubuntu1~test1ubuntu2ubuntu3", "0", "1~test1ubuntu2ubuntu3")]
     public void Parsing_ValidVersionString_MatchesExpectedValues(
         string validVersionString, 
         string? expectedEpoch,
         uint expectedEpochValue,
         string expectedUpstreamVersion,
         string expectedEffectiveUpstreamVersion,
         string? expectedRevertedUpstreamVersion,
         string? expectedRealUpstreamVersion,
         string? expectedRevision,
         string? expectedDebianRevision,
         string? expectedUbuntuRevision)
     {
         var debVersion = DebVersion.Parse(validVersionString);
         
         Assert.Equal(expected: expectedEpoch, actual: debVersion.Epoch);
         Assert.Equal(expected: expectedEpochValue, actual: debVersion.EpochValue);
         Assert.Equal(expected: expectedUpstreamVersion, actual: debVersion.UpstreamVersion);
         Assert.Equal(expected: expectedEffectiveUpstreamVersion, actual: debVersion.EffectiveUpstreamVersion);
         Assert.Equal(expected: expectedRevertedUpstreamVersion, actual: debVersion.RevertedUpstreamVersion);
         Assert.Equal(expected: expectedRealUpstreamVersion, actual: debVersion.RealUpstreamVersion);
         Assert.Equal(expected: expectedRevision, actual: debVersion.Revision);
         Assert.Equal(expected: expectedDebianRevision, actual: debVersion.DebianRevision);
         Assert.Equal(expected: expectedUbuntuRevision, actual: debVersion.UbuntuRevision);
         Assert.Equal(expected: validVersionString, actual: debVersion.ToString());   
     }

     [Theory]
     [InlineData("0", "0")]
     [InlineData("00", "0")]
     [InlineData("0:0", "0")]
     public void VersionA_Equals_VersionB(string a, string b)
     {
         var alpha = DebVersion.Parse(a);
         var beta = DebVersion.Parse(b);
         
         Assert.Equal(expected: 0, actual: alpha.CompareTo((object?)beta));
         Assert.Equal(expected: 0, actual: alpha.CompareTo(beta));
         Assert.Equal(expected: 0, actual: beta.CompareTo((object?)alpha));
         Assert.Equal(expected: 0, actual: beta.CompareTo(alpha));
         
         Assert.True(alpha.Equals(beta));
         Assert.True(beta.Equals(alpha));
         Assert.True(alpha == beta);
         Assert.True(beta == alpha);
         Assert.False(alpha != beta);
         Assert.False(beta != alpha);
                 
         Assert.False(alpha > beta);
         Assert.False(beta > alpha);
         Assert.True(alpha >= beta);
         Assert.True(beta >= alpha);
                 
         Assert.False(alpha < beta);
         Assert.False(beta < alpha);
         Assert.True(alpha <= beta);
         Assert.True(beta <= alpha);
     }
     
     [Theory]
     [InlineData("2", "1")]
     [InlineData("1-2", "1-1")]
     [InlineData("1:1", "2")]
     public void VersionA_IsGreaterThan_VersionB(string a, string b)
     {
         var alpha = DebVersion.Parse(a);
         var beta = DebVersion.Parse(b);
         
         Assert.Equal(expected: 1, actual: alpha.CompareTo((object?)beta));
         Assert.Equal(expected: 1, actual: alpha.CompareTo(beta));
         Assert.Equal(expected: -1, actual: beta.CompareTo((object?)alpha));
         Assert.Equal(expected: -1, actual: beta.CompareTo(alpha));
         
         Assert.False(alpha.Equals(beta));
         Assert.False(beta.Equals(alpha));
         Assert.False(alpha == beta);
         Assert.False(beta == alpha);
         Assert.True(alpha != beta);
         Assert.True(beta != alpha);
                 
         Assert.False(alpha < beta);
         Assert.True(beta < alpha);
         Assert.False(alpha <= beta);
         Assert.True(beta <= alpha);
                 
         Assert.True(alpha > beta);
         Assert.False(beta > alpha);
         Assert.True(alpha >= beta);
         Assert.False(beta >= alpha);
     }

     [Theory]
     [InlineData("")]
     [InlineData("1")]
     [InlineData("1:1")]
     [InlineData("1-1")]
     [InlineData("1:1-1")]
     [InlineData("1-1ubuntu1")]
     [InlineData("1:1-1ubuntu1")]
     public void Null_IsLessThan_AnyDebVersion(string version)
     {
         DebVersion debVersion = DebVersion.Parse(version);
         Assert.True(null < debVersion);
         
         Assert.Equal(expected: 1, actual: debVersion.CompareTo((object?)null));
         Assert.Equal(expected: 1, actual: debVersion.CompareTo((DebVersion?)null));
         
         Assert.False(debVersion.Equals(null));
         Assert.False(debVersion == null);
         Assert.True(debVersion != null);
                 
         Assert.False(debVersion < null);
         Assert.False(debVersion <= null);
         Assert.True(debVersion > null);
         Assert.True(debVersion >= null);
     }
     
     [Fact]
     public void Sort()
     {
         var versions = new [] { DebVersion.Parse("2"), null, DebVersion.Parse("1"), DebVersion.Parse("3"), null };
         
         Array.Sort(versions);
         
         Assert.Null(versions[0]);
         Assert.Null(versions[1]);
         Assert.Equal("1", versions[2]?.ToString());
         Assert.Equal("2", versions[3]?.ToString());
         Assert.Equal("3", versions[4]?.ToString());
     }
}