using System.IO.Compression;
using System.Text;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace Ffvi.SaveTool;

public static class SaveCrypto
{
    private static readonly byte[] Key =
    [
        97, 9, 7, 0, 185, 195, 184, 185, 66, 21, 153, 231, 156, 165, 123, 135,
        190, 169, 50, 211, 121, 123, 173, 118, 99, 237, 77, 222, 10, 148, 60, 197,
    ];

    private static readonly byte[] Iv =
    [
        2, 36, 249, 119, 31, 219, 110, 14, 59, 213, 8, 215, 183, 149, 191, 46,
        12, 189, 23, 105, 66, 104, 10, 99, 123, 18, 188, 98, 115, 219, 46, 187,
    ];

    private const int BlockSize = 32;

    public static string Decrypt(byte[] fileBytes)
    {
        var stripped = StripBom(fileBytes);
        var b64Padded = AddBase64Padding(Encoding.ASCII.GetString(stripped).Trim());
        var cipher = Convert.FromBase64String(b64Padded);
        var padded = RijndaelTransform(cipher, encrypt: false);
        var compressed = StripZeroPadding(padded);
        var json = DeflateDecompress(compressed);
        return Encoding.UTF8.GetString(json);
    }

    public static byte[] Encrypt(string json)
    {
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var compressed = DeflateCompress(jsonBytes);
        var padded = AddZeroPadding(compressed);
        var cipher = RijndaelTransform(padded, encrypt: true);
        var b64 = Convert.ToBase64String(cipher).TrimEnd('=');
        return Encoding.ASCII.GetBytes(b64);
    }

    private static byte[] StripBom(byte[] data) =>
        data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF
            ? data[3..]
            : data;

    private static string AddBase64Padding(string s)
    {
        var rem = s.Length % 4;
        return rem == 0 ? s : s + new string('=', 4 - rem);
    }

    // .NET's RijndaelManaged is AES-only (128-bit block) on .NET Core/5+, so use BouncyCastle
    // for the 256-bit block size the game uses.
    private static byte[] RijndaelTransform(byte[] input, bool encrypt)
    {
        var cipher = new CbcBlockCipher(new RijndaelEngine(256));
        cipher.Init(encrypt, new ParametersWithIV(new KeyParameter(Key), Iv));

        var output = new byte[input.Length];
        for (var i = 0; i < input.Length; i += BlockSize)
            cipher.ProcessBlock(input, i, output, i);
        return output;
    }

    // Matches the game's custom padder (not PKCS7): pad with zero bytes to a 32-byte boundary,
    // adding 0 bytes if already aligned. Decode strips trailing zeros up to BlockSize-1.
    private static byte[] AddZeroPadding(byte[] data)
    {
        var count = BlockSize - ((data.Length + BlockSize - 1) % BlockSize + 1);
        if (count == 0) return data;
        var result = new byte[data.Length + count];
        Buffer.BlockCopy(data, 0, result, 0, data.Length);
        return result;
    }

    private static byte[] StripZeroPadding(byte[] data)
    {
        if (data.Length == 0) return data;
        var minEnd = data.Length - BlockSize + 1;
        var offset = data.Length;
        while (offset > minEnd)
        {
            offset--;
            if (data[offset] != 0)
                return data[..(offset + 1)];
        }
        return data[..minEnd];
    }

    private static byte[] DeflateDecompress(byte[] compressed)
    {
        using var input = new MemoryStream(compressed);
        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        deflate.CopyTo(output);
        return output.ToArray();
    }

    private static byte[] DeflateCompress(byte[] data)
    {
        using var output = new MemoryStream();
        using (var deflate = new DeflateStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            deflate.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }
}
