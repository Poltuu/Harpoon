namespace Harpoon.Sender
{
    /// <summary>
    /// Represents a class able to assign a signature to content using a secret
    /// </summary>
    public interface ISignatureService
    {
        /// <summary>
        /// Returns a unique signature for content using the provided secret
        /// </summary>
        /// <param name="secret"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        string GetSignature(string secret, string content);
    }
}