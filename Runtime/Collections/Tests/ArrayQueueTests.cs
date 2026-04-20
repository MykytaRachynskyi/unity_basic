// ReSharper disable ForCanBeConvertedToForeach
// ReSharper disable LoopCanBeConvertedToQuery

using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Basic.Collections.Tests
{
    [TestFixture]
    public class ArrayQueueTests
    {
        private const int DEFAULT_CAPACITY = 8;

        [Test]
        public void DefaultConstructor_StartsEmptyWithDefaultCapacity()
        {
            var q = new ArrayQueue<int>();

            Assert.That(q.Count, Is.Zero);
            Assert.That(q.Capacity, Is.EqualTo(DEFAULT_CAPACITY));
            Assert.That(q.Head, Is.Zero);
            Assert.That(q.Tail, Is.Zero);
        }

        [Test]
        public void CapacityConstructor_UsesRequestedSizeWhenPositive()
        {
            var q = new ArrayQueue<int>(3);

            Assert.That(q.Count, Is.Zero);
            Assert.That(q.Capacity, Is.EqualTo(3));
        }

        [Test]
        public void CapacityConstructor_NonPositiveFallsBackToDefaultCapacity()
        {
            Assert.That(new ArrayQueue<int>(0).Capacity, Is.EqualTo(DEFAULT_CAPACITY));
            Assert.That(new ArrayQueue<int>(-1).Capacity, Is.EqualTo(DEFAULT_CAPACITY));
        }

        [Test]
        public void TryDequeue_WhenEmpty_ReturnsFalseAndDefaultOut()
        {
            var q = new ArrayQueue<int>();

            var ok = q.TryDequeue(out var item);

            Assert.That(ok, Is.False);
            Assert.That(item, Is.EqualTo(0));
            Assert.That(q.Count, Is.Zero);
            Assert.That(q.Head, Is.Zero);
            Assert.That(q.Tail, Is.Zero);
        }

        [Test]
        public void TryEnqueue_ReturnsTrue_AndIncrementsCount()
        {
            var q = new ArrayQueue<int>();

            Assert.That(q.TryEnqueue(1), Is.True);
            Assert.That(q.Count, Is.EqualTo(1));
            Assert.That(q.Head, Is.Zero);
            Assert.That(q.Tail, Is.EqualTo(1));
            Assert.That(q.TryEnqueue(2), Is.True);
            Assert.That(q.Count, Is.EqualTo(2));
            Assert.That(q.Head, Is.Zero);
            Assert.That(q.Tail, Is.EqualTo(2));
        }

        [Test]
        public void TryDequeue_FifoOrder_SingleEnqueueDequeueCycle()
        {
            var q = new ArrayQueue<int>();
            q.TryEnqueue(10);
            q.TryEnqueue(20);
            q.TryEnqueue(30);

            Assert.That(q.TryDequeue(out var a), Is.True);
            Assert.That(a, Is.EqualTo(10));
            Assert.That(q.Count, Is.EqualTo(2));

            Assert.That(q.TryDequeue(out var b), Is.True);
            Assert.That(b, Is.EqualTo(20));

            Assert.That(q.TryDequeue(out var c), Is.True);
            Assert.That(c, Is.EqualTo(30));

            Assert.That(q.TryDequeue(out _), Is.False);
            Assert.That(q.Count, Is.Zero);
            Assert.That(q.Head, Is.EqualTo(3));
            Assert.That(q.Tail, Is.EqualTo(3));
        }

        [Test]
        public void TailWraps_WhenEnqueueReachesEndOfBuffer()
        {
            var q = new ArrayQueue<int>(4);
            for (var i = 0; i < 4; i++)
            {
                q.TryEnqueue(i);
            }

            Assert.That(q.Head, Is.Zero);
            Assert.That(q.Tail, Is.Zero);

            Assert.That(q.TryDequeue(out var d0), Is.True);
            Assert.That(d0, Is.EqualTo(0));
            Assert.That(q.Head, Is.EqualTo(1));
            Assert.That(q.Tail, Is.Zero);
            Assert.That(q.TryDequeue(out var d1), Is.True);
            Assert.That(d1, Is.EqualTo(1));
            Assert.That(q.Head, Is.EqualTo(2));
            Assert.That(q.Tail, Is.Zero);

            q.TryEnqueue(100);
            Assert.That(q.Head, Is.EqualTo(2));
            Assert.That(q.Tail, Is.EqualTo(1));
            q.TryEnqueue(200);
            Assert.That(q.Head, Is.EqualTo(2));
            Assert.That(q.Tail, Is.EqualTo(2));

            Assert.That(q.Count, Is.EqualTo(4));
            Assert.That(q.TryDequeue(out var x0), Is.True);
            Assert.That(x0, Is.EqualTo(2));
            Assert.That(q.TryDequeue(out var x1), Is.True);
            Assert.That(x1, Is.EqualTo(3));
            Assert.That(q.TryDequeue(out var x2), Is.True);
            Assert.That(x2, Is.EqualTo(100));
            Assert.That(q.TryDequeue(out var x3), Is.True);
            Assert.That(x3, Is.EqualTo(200));
        }

        [Test]
        public void HeadWraps_WhenDequeueAdvancesPastEndOfBuffer_WithoutGrowing()
        {
            var q = new ArrayQueue<int>(4);
            q.TryEnqueue(0);
            q.TryEnqueue(1);
            q.TryEnqueue(2);
            q.TryEnqueue(3);

            Assert.That(q.Head, Is.Zero);
            Assert.That(q.Tail, Is.Zero);

            Assert.That(q.TryDequeue(out _), Is.True);
            Assert.That(q.Head, Is.EqualTo(1));
            Assert.That(q.TryDequeue(out _), Is.True);
            Assert.That(q.Head, Is.EqualTo(2));
            Assert.That(q.TryDequeue(out _), Is.True);
            Assert.That(q.Head, Is.EqualTo(3));
            Assert.That(q.Tail, Is.Zero);

            q.TryEnqueue(10);
            Assert.That(q.Head, Is.EqualTo(3));
            Assert.That(q.Tail, Is.EqualTo(1));
            q.TryEnqueue(11);
            Assert.That(q.Tail, Is.EqualTo(2));
            q.TryEnqueue(12);
            Assert.That(q.Head, Is.EqualTo(3));
            Assert.That(q.Tail, Is.EqualTo(3));

            Assert.That(q.Capacity, Is.EqualTo(4));
            Assert.That(q.ToDequeuedList(), Is.EqualTo(new[] { 3, 10, 11, 12 }));
        }

        [Test]
        public void TailWraps_MultipleRoundsOfPartialDrainAndFill()
        {
            const int capacity = 5;
            const int rounds = 30;
            var q = new ArrayQueue<int>(capacity);

            for (var i = 0; i < capacity; i++)
            {
                q.TryEnqueue(i);
            }

            Assert.That(q.Head, Is.Zero);
            Assert.That(q.Tail, Is.Zero);

            var nextValue = capacity;
            for (var round = 0; round < rounds; round++)
            {
                Assert.That(q.Count, Is.EqualTo(capacity));
                Assert.That(q.Capacity, Is.EqualTo(capacity));

                Assert.That(q.TryDequeue(out _), Is.True);
                Assert.That(q.TryDequeue(out _), Is.True);

                q.TryEnqueue(nextValue++);
                q.TryEnqueue(nextValue++);

                Assert.That(q.Count, Is.EqualTo(capacity));
                var expectedIndex = (2 * (round + 1)) % capacity;
                Assert.That(q.Head, Is.EqualTo(expectedIndex));
                Assert.That(q.Tail, Is.EqualTo(expectedIndex));
            }

            var firstInFinalWindow = 2 * rounds;
            var expected = new List<int>();
            for (var i = 0; i < capacity; i++)
            {
                expected.Add(firstInFinalWindow + i);
            }

            Assert.That(q.ToDequeuedList(), Is.EqualTo(expected));
        }

        [Test]
        public void Grow_WhenFullOnNextEnqueue_IncreasesCapacity()
        {
            var q = new ArrayQueue<int>(4);
            for (var i = 0; i < 4; i++)
            {
                q.TryEnqueue(i);
            }

            Assert.That(q.Head, Is.Zero);
            Assert.That(q.Tail, Is.Zero);

            var capBefore = q.Capacity;
            q.TryEnqueue(99);

            Assert.That(capBefore, Is.EqualTo(4));
            Assert.That(q.Capacity, Is.GreaterThan(4));
            Assert.That(q.Count, Is.EqualTo(5));
            Assert.That(q.Head, Is.Zero);
            Assert.That(q.Tail, Is.EqualTo(5));
            Assert.That(q.ToDequeuedList(), Is.EqualTo(new[] { 0, 1, 2, 3, 99 }));
        }

        [Test]
        public void Grow_WithWrappedHead_PreservesLogicalOrder()
        {
            var q = new ArrayQueue<int>(4);
            for (var i = 0; i < 4; i++)
            {
                q.TryEnqueue(i);
            }

            Assert.That(q.TryDequeue(out _), Is.True);
            Assert.That(q.TryDequeue(out _), Is.True);
            q.TryEnqueue(10);
            q.TryEnqueue(11);

            Assert.That(q.Head, Is.EqualTo(2));
            Assert.That(q.Tail, Is.EqualTo(2));

            q.TryEnqueue(12);

            Assert.That(q.Capacity, Is.GreaterThan(4));
            Assert.That(q.Head, Is.Zero);
            Assert.That(q.Tail, Is.EqualTo(5));
            Assert.That(q.ToDequeuedList(), Is.EqualTo(new[] { 2, 3, 10, 11, 12 }));
        }

        [Test]
        public void Grow_MinimumCapacityOne_UsesCapacityPlusOneBranch()
        {
            var q = new ArrayQueue<int>(1);
            q.TryEnqueue(7);
            Assert.That(q.Head, Is.Zero);
            Assert.That(q.Tail, Is.Zero);
            q.TryEnqueue(8);

            Assert.That(q.Capacity, Is.GreaterThanOrEqualTo(2));
            Assert.That(q.Head, Is.Zero);
            Assert.That(q.Tail, Is.Zero);
            Assert.That(q.ToDequeuedList(), Is.EqualTo(new[] { 7, 8 }));
        }

        [Test]
        public void Grow_DefaultConstructor_FillsEightThenGrowsOnNinth()
        {
            var q = new ArrayQueue<int>();
            for (var i = 0; i < DEFAULT_CAPACITY; i++)
            {
                q.TryEnqueue(i);
            }

            Assert.That(q.Capacity, Is.EqualTo(DEFAULT_CAPACITY));
            q.TryEnqueue(DEFAULT_CAPACITY);

            Assert.That(q.Capacity, Is.GreaterThan(DEFAULT_CAPACITY));
            var list = q.ToDequeuedList();
            Assert.That(list.Count, Is.EqualTo(DEFAULT_CAPACITY + 1));
            for (var i = 0; i <= DEFAULT_CAPACITY; i++)
            {
                Assert.That(list[i], Is.EqualTo(i));
            }
        }

        [Test]
        public void Grow_RepeatedOverflow_CapacityEventuallyHoldsAllElementsAndFifoPreserved()
        {
            const int n = 64;
            var q = new ArrayQueue<int>(2);
            var growthSteps = 0;
            var lastCap = q.Capacity;

            for (var i = 0; i < n; i++)
            {
                q.TryEnqueue(i);
                if (q.Capacity > lastCap)
                {
                    growthSteps++;
                    lastCap = q.Capacity;
                }
            }

            Assert.That(growthSteps, Is.GreaterThanOrEqualTo(2));
            Assert.That(q.Capacity, Is.GreaterThanOrEqualTo(n));
            Assert.That(q.Count, Is.EqualTo(n));

            for (var i = 0; i < n; i++)
            {
                Assert.That(q.TryDequeue(out var v), Is.True);
                Assert.That(v, Is.EqualTo(i));
            }
        }

        [Test]
        public void ReferenceType_DefaultIsNull_OnFailedDequeue()
        {
            var q = new ArrayQueue<string>();

            q.TryDequeue(out var s);

            Assert.That(s, Is.Null);
        }

        [Test]
        public void ReferenceType_EnqueueDequeue_RoundTrip()
        {
            var q = new ArrayQueue<string>();
            q.TryEnqueue("a");
            q.TryEnqueue("b");

            Assert.That(q.TryDequeue(out var x), Is.True);
            Assert.That(x, Is.EqualTo("a"));
            Assert.That(q.TryDequeue(out var y), Is.True);
            Assert.That(y, Is.EqualTo("b"));
        }

        [Test]
        public void InterleavedEnqueueDequeue_MaintainsFifoAfterGrowthAndWrap()
        {
            var q = new ArrayQueue<int>(3);
            q.TryEnqueue(1);
            q.TryEnqueue(2);
            q.TryDequeue(out _);
            q.TryEnqueue(3);
            q.TryEnqueue(4);
            q.TryEnqueue(5);

            Assert.That(q.Capacity, Is.GreaterThan(3));
            Assert.That(q.Head, Is.Zero);
            Assert.That(q.Tail, Is.Zero);
            Assert.That(q.ToDequeuedList(), Is.EqualTo(new[] { 2, 3, 4, 5 }));
        }

        [Test]
        public void Indices_TailAdvancesWithModulo_OnEachEnqueue()
        {
            var q = new ArrayQueue<int>(3);
            q.TryEnqueue(0);
            Assert.That(q.Tail, Is.EqualTo(1));
            q.TryEnqueue(1);
            Assert.That(q.Tail, Is.EqualTo(2));
            q.TryEnqueue(2);
            Assert.That(q.Tail, Is.Zero);
        }

        [Test]
        public void Indices_HeadAdvancesWithModulo_OnEachDequeue()
        {
            var q = new ArrayQueue<int>(3);
            q.TryEnqueue(0);
            q.TryEnqueue(1);
            q.TryEnqueue(2);

            q.TryDequeue(out _);
            Assert.That(q.Head, Is.EqualTo(1));
            q.TryDequeue(out _);
            Assert.That(q.Head, Is.EqualTo(2));
            q.TryDequeue(out _);
            Assert.That(q.Head, Is.Zero);
        }

        [Test]
        public void Indices_WhenFull_HeadMayEqualTail()
        {
            var q = new ArrayQueue<int>(4);
            for (var i = 0; i < 4; i++)
            {
                q.TryEnqueue(i);
            }

            Assert.That(q.Head, Is.EqualTo(q.Tail));
            Assert.That(q.Count, Is.EqualTo(4));
        }

        [Test]
        public void Indices_FailedDequeue_DoesNotChangeHeadOrTail()
        {
            var q = new ArrayQueue<int>();
            q.TryEnqueue(1);
            q.TryDequeue(out _);
            var head = q.Head;
            var tail = q.Tail;

            q.TryDequeue(out _);

            Assert.That(q.Head, Is.EqualTo(head));
            Assert.That(q.Tail, Is.EqualTo(tail));
        }

        [Test]
        public void Indices_AfterDrainToEmpty_HeadAndTailAreNotReset()
        {
            var q = new ArrayQueue<int>(8);
            q.TryEnqueue(1);
            q.TryEnqueue(2);
            q.TryDequeue(out _);
            q.TryDequeue(out _);

            Assert.That(q.Count, Is.Zero);
            Assert.That(q.Head, Is.EqualTo(2));
            Assert.That(q.Tail, Is.EqualTo(2));
        }

        [Test]
        public void Indexer_ForLoopFromZeroToCount_MatchesFifo_WhenBufferWrapped()
        {
            var q = new ArrayQueue<int>(4);
            for (var i = 0; i < 4; i++)
            {
                q.TryEnqueue(i);
            }

            q.TryDequeue(out _);
            q.TryDequeue(out _);
            q.TryEnqueue(10);
            q.TryEnqueue(11);

            var fromIndexer = new List<int>();
            
            for (var i = 0; i < q.Count; i++)
            {
                fromIndexer.Add(q[i]);
            }

            Assert.That(fromIndexer, Is.EqualTo(new[] { 2, 3, 10, 11 }));
        }

        [Test]
        public void Indexer_NegativeOrAtLeastCount_ThrowsArgumentOutOfRange()
        {
            var q = new ArrayQueue<int>();
            q.TryEnqueue(7);

            Assert.Throws<ArgumentOutOfRangeException>(() => _ = q[-1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = q[1]);
        }

        [Test]
        public void Indexer_EmptyQueue_IndexZero_ThrowsArgumentOutOfRange()
        {
            var q = new ArrayQueue<int>();

            Assert.Throws<ArgumentOutOfRangeException>(() => _ = q[0]);
        }

        [Test]
        public void Foreach_YieldsSameOrder_AsIndexerForLoop_AfterGrowth()
        {
            var q = new ArrayQueue<int>(3);
            q.TryEnqueue(1);
            q.TryEnqueue(2);
            q.TryDequeue(out _);
            q.TryEnqueue(3);
            q.TryEnqueue(4);
            q.TryEnqueue(5);

            var fromIndexer = new List<int>();
            for (var i = 0; i < q.Count; i++)
            {
                fromIndexer.Add(q[i]);
            }

            var fromForeach = new List<int>();
            foreach (var x in q)
            {
                fromForeach.Add(x);
            }

            Assert.That(fromForeach, Is.EqualTo(fromIndexer));
            Assert.That(fromForeach, Is.EqualTo(new[] { 2, 3, 4, 5 }));
        }

        [Test]
        public void Foreach_Empty_YieldsNoElements()
        {
            var q = new ArrayQueue<int>();
            var n = 0;
            foreach (var _ in q)
            {
                n++;
            }

            Assert.That(n, Is.Zero);
        }

        [Test]
        public void IEnumerable_AsInterface_Foreach_MatchesFifo()
        {
            var q = new ArrayQueue<int>();
            q.TryEnqueue(10);
            q.TryEnqueue(20);
            IEnumerable<int> seq = q;
            var list = new List<int>();
            foreach (var x in seq)
            {
                list.Add(x);
            }

            Assert.That(list, Is.EqualTo(new[] { 10, 20 }));
        }

        [Test]
        public void GetEnumerator_CurrentBeforeMoveNext_ThrowsInvalidOperation()
        {
            var q = new ArrayQueue<int>();
            q.TryEnqueue(1);
            var e = q.GetEnumerator();

            Assert.Throws<InvalidOperationException>(() => _ = e.Current);
        }

        [Test]
        public void Clear_WhenQueueHasItems_ResetsCountAndIndicesWithoutChangingCapacity()
        {
            var q = new ArrayQueue<int>(4);
            q.TryEnqueue(0);
            q.TryEnqueue(1);
            q.TryDequeue(out _);
            q.TryEnqueue(2);

            Assert.That(q.Count, Is.EqualTo(2));
            Assert.That(q.Head, Is.EqualTo(1));
            Assert.That(q.Tail, Is.EqualTo(3));
            Assert.That(q.Capacity, Is.EqualTo(4));

            q.Clear();

            Assert.That(q.Count, Is.Zero);
            Assert.That(q.Head, Is.Zero);
            Assert.That(q.Tail, Is.Zero);
            Assert.That(q.Capacity, Is.EqualTo(4));
            Assert.That(q.TryDequeue(out _), Is.False);
        }

        [Test]
        public void Clear_AfterGrowth_QueueCanBeReusedInFifoOrder()
        {
            var q = new ArrayQueue<int>(2);
            q.TryEnqueue(10);
            q.TryEnqueue(11);
            q.TryEnqueue(12);
            var grownCapacity = q.Capacity;

            Assert.That(grownCapacity, Is.GreaterThan(2));

            q.Clear();

            Assert.That(q.Count, Is.Zero);
            Assert.That(q.Head, Is.Zero);
            Assert.That(q.Tail, Is.Zero);
            Assert.That(q.Capacity, Is.EqualTo(grownCapacity));

            q.TryEnqueue(100);
            q.TryEnqueue(200);

            Assert.That(q.ToDequeuedList(), Is.EqualTo(new[] { 100, 200 }));
        }
    }

    internal static class ArrayQueueTestExtensions
    {
        public static List<T> ToDequeuedList<T>(this ArrayQueue<T> queue)
        {
            var list = new List<T>();
            while (queue.TryDequeue(out var item))
            {
                list.Add(item);
            }

            return list;
        }
    }
}
