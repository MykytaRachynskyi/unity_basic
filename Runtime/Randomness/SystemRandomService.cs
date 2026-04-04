using System;

namespace Basic.Randomness
{
	public sealed class SystemRandomService : IRandomnessService
	{
		private readonly Random _random;
		private readonly byte[] _ulongBuffer = new byte[8];

		public SystemRandomService()
		{
			_random = new Random();
		}

		public SystemRandomService(int seed)
		{
			_random = new Random(seed);
		}

		public int NextInt(int minInclusive, int maxExclusive)
		{
			ValidateRange(minInclusive, maxExclusive);
			return _random.Next(minInclusive, maxExclusive);
		}

		public long NextLong(long minInclusive, long maxExclusive)
		{
			ValidateRange(minInclusive, maxExclusive);
			if (minInclusive == long.MinValue && maxExclusive == long.MaxValue)
			{
				long candidate;
				do
				{
					_random.NextBytes(_ulongBuffer);
					candidate = BitConverter.ToInt64(_ulongBuffer, 0);
				} while (candidate == long.MaxValue);

				return candidate;
			}

			var span = ExclusiveSpan(minInclusive, maxExclusive);
			if (span == 1UL)
			{
				return minInclusive;
			}

			var maxValid = ulong.MaxValue - ulong.MaxValue % span;
			ulong r;
			do
			{
				_random.NextBytes(_ulongBuffer);
				r = BitConverter.ToUInt64(_ulongBuffer, 0);
			} while (r > maxValid);

			return minInclusive + (long)(r % span);
		}

		public float NextFloat(float minInclusive, float maxExclusive)
		{
			ValidateRange(minInclusive, maxExclusive);
			double t = _random.NextDouble();
			return (float)(minInclusive + t * (maxExclusive - minInclusive));
		}

		public double NextDouble(double minInclusive, double maxExclusive)
		{
			ValidateRange(minInclusive, maxExclusive);
			double t = _random.NextDouble();
			return minInclusive + t * (maxExclusive - minInclusive);
		}

		private static ulong ExclusiveSpan(long minInclusive, long maxExclusive)
		{
			if (minInclusive >= 0 || maxExclusive <= 0)
			{
				return (ulong)(maxExclusive - minInclusive);
			}

			const ulong minValueMagnitude = 1UL << 63;
			ulong negPart = minInclusive == long.MinValue
				? minValueMagnitude
				: (ulong)(-minInclusive);
			return negPart + (ulong)maxExclusive;
		}

		private static void ValidateRange<T>(T minInclusive, T maxExclusive)
			where T : IComparable<T>
		{
			if (minInclusive.CompareTo(maxExclusive) >= 0)
			{
				throw new ArgumentOutOfRangeException(
					nameof(maxExclusive),
					"maxExclusive must be greater than minInclusive."
				);
			}
		}
	}
}