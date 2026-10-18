using Project.Application.Abstractions;

namespace Project.Infrastructure.Tests.Support;

public sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"hashed:{password}";

    public bool Verify(string password, string passwordHash) => passwordHash == Hash(password);
}

public sealed class ThrowingPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => throw new InvalidOperationException("Hash should not be called.");

    public bool Verify(string password, string passwordHash) => throw new InvalidOperationException("Verify should not be called.");
}
