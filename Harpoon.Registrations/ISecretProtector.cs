namespace Harpoon.Registrations.EFStorage
{
    /// <summary>
    /// A class able to protect and unprotect a piece of plaintext data
    /// </summary>
    public interface ISecretProtector
    {
        /// <summary>
        /// Protects a piece of plaintext data
        /// </summary>
        /// <param name="plaintext"></param>
        /// <returns></returns>
        string Protect(string plaintext);

        /// <summary>
        /// Unprotects a piece of protectedData
        /// </summary>
        /// <param name="protectedData"></param>
        /// <returns></returns>
        string Unprotect(string protectedData);
    }
}