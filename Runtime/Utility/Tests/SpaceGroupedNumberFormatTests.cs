using NUnit.Framework;

namespace Basic.Utility.Tests
{
    [TestFixture]
    public class SpaceGroupedNumberFormatTests
    {
        [Test]
        public void Format_Integer_GroupsWithSpaces()
        {
            Assert.That(SpaceGroupedNumberFormat.Format(1234567), Is.EqualTo("1 234 567"));
        }

        [Test]
        public void ToString_Integer_GroupsWithSpaces()
        {
            Assert.That(1234567.ToString("#,##0", SpaceGroupedNumberFormat.Invariant), Is.EqualTo("1 234 567"));
        }

        [Test]
        public void Format_ValueUnder1000_HasNoSpaces()
        {
            Assert.That(SpaceGroupedNumberFormat.Format(999), Is.EqualTo("999"));
        }

        [Test]
        public void Format_NegativeInteger_GroupsAfterSign()
        {
            Assert.That(SpaceGroupedNumberFormat.Format(-1234), Is.EqualTo("-1 234"));
        }

        [Test]
        public void Format_Double_GroupsIntegerPartAndFraction()
        {
            Assert.That(SpaceGroupedNumberFormat.Format(1234567.5), Is.EqualTo("1 234 567.5"));
        }

        [Test]
        public void ToString_Double_FixedDecimals()
        {
            Assert.That(
                1234567.5.ToString("#,##0.00", SpaceGroupedNumberFormat.Invariant),
                Is.EqualTo("1 234 567.50"));
        }

        [Test]
        public void StringFormat_WithProvider_GroupsCorrectly()
        {
            Assert.That(
                string.Format(SpaceGroupedNumberFormat.Invariant, "{0:#,##0}", 1234567),
                Is.EqualTo("1 234 567"));
        }

        [Test]
        public void Format_Long_UsesSameGrouping()
        {
            Assert.That(SpaceGroupedNumberFormat.Format(1_000_000L), Is.EqualTo("1 000 000"));
        }
    }
}
