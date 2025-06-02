using Bogus;

namespace NetworkImitator.Extensions;

public static class RandomExtensions
{
    public static string RandomSentence(int totalSize)
    {
        var faker = new Faker();
        var words = new List<string>();
        var size = 0;
        while (size < totalSize)
        {
            var newWord = faker.Random.Word();
            words.Add(newWord);
            size += newWord.Length;
        }
        return string.Join(" ", words)[..totalSize];
    }
    
    public static string RandomWord()
    {
        var faker = new Faker();
        return faker.Random.Word();
    }

    public static string RandomIp()
    {
        var faker = new Faker();
        return faker.Internet.Ip();
    }
}