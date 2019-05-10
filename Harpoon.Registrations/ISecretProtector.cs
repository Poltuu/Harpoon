namespace Harpoon.Registrations.EFStorage
{
    public interface ISecretProtector
    {
        string Protect(string plaintext);
        string Unprotect(string protectedData);
    }
}