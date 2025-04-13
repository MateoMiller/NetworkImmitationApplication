using Bogus;

namespace NetworkImitator.Extensions;

public static class RandomExtensions
{
    public static string RandomWord()
    {
        var faker = new Faker();
        return faker.Lorem.Word();
    }

    public static string RandomIp()
    {
        var faker = new Faker();
        return faker.Internet.Ip();
    }
}