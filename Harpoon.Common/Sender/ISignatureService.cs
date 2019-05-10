namespace Harpoon.Sender
{
    public interface ISignatureService
    {
        string GetSignature(string secret, string content);
    }
}