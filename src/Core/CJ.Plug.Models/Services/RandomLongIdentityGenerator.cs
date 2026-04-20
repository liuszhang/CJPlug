using System.Security.Cryptography;



/// <summary>
/// Generates a unique identifier using a random long value.
/// </summary>
public class RandomLongIdentityGenerator
{
    /// <inheritdoc />
    public static string GenerateId()
    {
        //return Generate16Id();
        return Generate4Id();
    }

    public static string Generate4Id()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        //const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 4)
            .Select(s => s[random.Next(s.Length)])
            .ToArray());
    }

    public static string Generate16Id()
    {
        var bytes = new byte[8];
        RandomNumberGenerator.Fill(bytes);
        var randomLong = BitConverter.ToInt64(bytes, 0);
        return randomLong.ToString("x");
    }
}