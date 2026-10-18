using Project.Infrastructure.Security;

namespace Project.Infrastructure.Tests.Security;

public class BcryptPasswordHasherTests
{
    private readonly BcryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_ReturnsADifferentValueForTheSamePassword()
    {
        var first = _hasher.Hash("Password123!");
        var second = _hasher.Hash("Password123!");

        first.Should().NotBe("Password123!");
        first.Should().StartWith("$2");
        second.Should().NotBe(first);
    }

    [Fact]
    public void Verify_AcceptsTheOriginalPassword()
    {
        var hash = _hasher.Hash("Password123!");

        _hasher.Verify("Password123!", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_RejectsADifferentPassword()
    {
        var hash = _hasher.Hash("Password123!");

        _hasher.Verify("Password123", hash).Should().BeFalse();
    }
}
