using System.Security.Cryptography;

namespace StackDuel.Domain.Games;

public static class GameJoinCode
{
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private const int Length = 7;

    public static string Generate()
    {
        Span<char> code = stackalloc char[Length];

        for (int i = 0; i < Length; i++)
        {
            code[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(code);
    }
}