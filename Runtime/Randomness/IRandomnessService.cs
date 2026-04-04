namespace Basic.Randomness
{
    public interface IRandomnessService
    {
        int NextInt(int minInclusive, int maxExclusive);

        long NextLong(long minInclusive, long maxExclusive);

        float NextFloat(float minInclusive, float maxExclusive);

        double NextDouble(double minInclusive, double maxExclusive);
    }
}
