namespace DiceEngine.Random;

public interface IRandomSource
{
    int Next(int minInclusive, int maxExclusive);
}
