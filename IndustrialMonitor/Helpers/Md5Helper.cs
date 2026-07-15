using System.Security.Cryptography;
using System.Text;

namespace IndustrialMonitor.Helpers;

public static class Md5Helper
{
    public static string ComputeMD5Hash(string input)
    {
        byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}