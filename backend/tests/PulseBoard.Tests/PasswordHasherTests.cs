using PulseBoard.Infrastructure.Auth;

namespace PulseBoard.Tests;

public class PasswordHasherTests
{
    private readonly BcryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_ThenVerify_Succeeds()
    {
        var hash = _hasher.Hash("Sup3rSecret!");
        Assert.True(_hasher.Verify(hash, "Sup3rSecret!"));
    }

    [Fact]
    public void Verify_WrongPassword_Fails()
    {
        var hash = _hasher.Hash("Sup3rSecret!");
        Assert.False(_hasher.Verify(hash, "wrong"));
    }

    [Fact]
    public void Hash_IsSalted_ProducesDifferentHashesForSameInput()
    {
        Assert.NotEqual(_hasher.Hash("same"), _hasher.Hash("same"));
    }

    [Fact]
    public void Verify_MalformedHash_ReturnsFalse_DoesNotThrow()
    {
        Assert.False(_hasher.Verify("not-a-bcrypt-hash", "whatever"));
    }

    [Fact]
    public void Verify_HandlesSeederHashes_DefaultWorkFactor()
    {
        // The seeder hashes with BCrypt's default work factor; verification must still succeed.
        var seedHash = BCrypt.Net.BCrypt.HashPassword("Admin123!");
        Assert.True(_hasher.Verify(seedHash, "Admin123!"));
    }
}
