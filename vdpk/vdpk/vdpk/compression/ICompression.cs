 
namespace vdpk
{
  public  interface ICompression
    {
        byte[] Compress(byte[] bytes);
        byte[] Decompress(byte[] bytes);
    }
}
