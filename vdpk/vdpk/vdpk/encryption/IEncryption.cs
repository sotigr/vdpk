namespace vdpk
{
    public interface IEncryption
    {
        byte[] Encrypt(byte[] bytes, string pass);
        byte[] Decrypt(byte[] bytes, string pass);
        string Encrypt(string str, string pass);
        string Decrypt(string str, string pass);
    }
}
