using FluentAssertions;

namespace Maintenance.UnitTests;

public class SanityTests
{
    [Fact]
    public void Xunit_Runs()
    {
        true.Should().BeTrue();
    }
}
