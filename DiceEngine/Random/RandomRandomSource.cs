namespace DiceEngine.Random;

public sealed class RandomRandomSource : IRandomSource
{
    private readonly System.Random _random = System.Random.Shared;

    public int Next(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);
}
