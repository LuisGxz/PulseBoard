using PulseBoard.Application.Common.Interfaces;

namespace PulseBoard.Infrastructure.Auth;

/// <summary>BCrypt-based password hashing (work factor 11). Matches the hashes produced by the seeder.</summary>
public class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 11;

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string hash, string password)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
