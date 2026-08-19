using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Basic.Singleton.Tests
{
    [TestFixture]
    public class ScriptableSingletonDatabaseTests
    {
        [Test]
        public void BuildSingletonMap_NullList_ReturnsEmptyMap()
        {
            var map = InvokeBuildSingletonMap(null);

            Assert.That(map, Is.Not.Null);
            Assert.That(map.Count, Is.Zero);
        }

        [Test]
        public void BuildSingletonMap_SkipsNullEntries_AndMapsNonNullSingletons()
        {
            var singleton = ScriptableObject.CreateInstance<TestScriptableSingleton>();
            var map = InvokeBuildSingletonMap(new List<Singleton> { null, singleton });

            Assert.That(map.Count, Is.EqualTo(1));
            Assert.That(map[singleton.GetType().GetHashCode()], Is.SameAs(singleton));

            Object.DestroyImmediate(singleton);
        }

        private static Dictionary<int, Singleton> InvokeBuildSingletonMap(List<Singleton> singletons)
        {
            var method = typeof(ScriptableSingletonDatabase).GetMethod(
                "BuildSingletonMap",
                BindingFlags.NonPublic | BindingFlags.Static
            );
            Assert.That(method, Is.Not.Null);
            return (Dictionary<int, Singleton>)method.Invoke(null, new object[] { singletons });
        }

        private sealed class TestScriptableSingleton : Singleton<TestScriptableSingleton> { }
    }
}
