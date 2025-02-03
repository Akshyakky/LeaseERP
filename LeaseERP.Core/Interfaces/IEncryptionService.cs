namespace LeaseERP.Core.Interfaces
{
    public interface IEncryptionService
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);
        string EncryptSensitiveData(string plainText);
        string DecryptSensitiveData(string cipherText);
    }
}
