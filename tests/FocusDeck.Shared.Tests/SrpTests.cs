using NUnit.Framework;
using FocusDeck.Shared.Security;
using System.Text;
using System.Numerics;

namespace FocusDeck.Shared.Tests;

public class SrpTests
{
    [Test]
    public void ComputePrivateKey_WithArgon2id_ProducesDeterministicOutput()
    {
        // Arrange
        var kdfParams = new SrpKdfParameters("argon2id", Convert.ToBase64String(Encoding.UTF8.GetBytes("somesalt")), degreeOfParallelism: 2, iterations: 3, memorySizeKiB: 65536, aad: true);
        const string userId = "testuser";
        const string password = "password123";

        // Act
        var privateKey1 = Srp.ComputePrivateKey(kdfParams, userId, password);
        var privateKey2 = Srp.ComputePrivateKey(kdfParams, userId, password);

        // Assert
        Assert.That(privateKey1, Is.EqualTo(privateKey2));
    }

    [Test]
    public void ComputePrivateKey_Legacy_ProducesDeterministicOutput()
    {
        // Arrange
        var salt = Encoding.UTF8.GetBytes("somesalt");
        const string userId = "testuser";
        const string password = "password123";

        // Act
        var privateKey1 = Srp.ComputePrivateKey(salt, userId, password);
        var privateKey2 = Srp.ComputePrivateKey(salt, userId, password);

        // Assert
        Assert.That(privateKey1, Is.EqualTo(privateKey2));
    }

    [Test]
    public void ComputePrivateKey_WithArgon2id_ProducesCorrectKnownValue()
    {
        // Arrange
        var kdfParams = new SrpKdfParameters("argon2id", "c29tZXNhbHQ=", degreeOfParallelism: 2, iterations: 3, memorySizeKiB: 65536, aad: true); // salt = "somesalt"
        const string userId = "testuser";
        const string password = "password123";
        
        // This expected value was pre-computed using the specified Argon2id parameters.
        var expected = BigInteger.Parse("6958963079798314705936191365482286623847185054298203272896443791224534012541");

        // Act
        var privateKey = Srp.ComputePrivateKey(kdfParams, userId, password);

        // Assert
        Assert.That(privateKey, Is.EqualTo(expected));
    }
}
