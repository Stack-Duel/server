using Bogus;

namespace StackDuel.Application.Services.Users;

public interface IUsernameGeneratorService
{
    string Generate();
}

internal sealed class UsernameGeneratorService : IUsernameGeneratorService
{
    private readonly Faker _faker = new();

    public string Generate()
    {
        string adjective = Clean(_faker.Hacker.Adjective());
        string noun = Clean(_faker.Hacker.Noun());
        int number = _faker.Random.Int(10, 99);

        return $"{Truncate(adjective, 8)}_{Truncate(noun, 7)}{number}";
    }

    private static string Clean(string value)
    {
        char[] chars =
        [
            .. value.Select(c => c == ' ' ? '_' : c).Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'),
        ];
        return new string(chars);
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}