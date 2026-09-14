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
        public void GetGroups_ReturnsSerializedGroups()
        {
            var database = ScriptableObject.CreateInstance<ScriptableSingletonDatabase>();
            SetField(database, "groups", new List<string> { "Configs", "Gameplay" });

            var previousInstance = GetStaticInstance();
            SetStaticInstance(database);
            try
            {
                var groups = ScriptableSingletonDatabase.GetGroups();

                Assert.That(groups, Is.EqualTo(new[] { "Configs", "Gameplay" }));
            }
            finally
            {
                SetStaticInstance(previousInstance);
                Object.DestroyImmediate(database);
            }
        }

        [Test]
        public void GetGroups_NullGroupsList_ReturnsEmpty()
        {
            var database = ScriptableObject.CreateInstance<ScriptableSingletonDatabase>();
            SetField(database, "groups", (List<string>)null);

            var previousInstance = GetStaticInstance();
            SetStaticInstance(database);
            try
            {
                Assert.That(ScriptableSingletonDatabase.GetGroups(), Is.Empty);
            }
            finally
            {
                SetStaticInstance(previousInstance);
                Object.DestroyImmediate(database);
            }
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

        private static ScriptableSingletonDatabase GetStaticInstance()
        {
            var field = typeof(ScriptableSingletonDatabase).GetField(
                "_instance",
                BindingFlags.NonPublic | BindingFlags.Static
            );
            Assert.That(field, Is.Not.Null);
            return (ScriptableSingletonDatabase)field.GetValue(null);
        }

        private static void SetStaticInstance(ScriptableSingletonDatabase instance)
        {
            var field = typeof(ScriptableSingletonDatabase).GetField(
                "_instance",
                BindingFlags.NonPublic | BindingFlags.Static
            );
            Assert.That(field, Is.Not.Null);
            field.SetValue(null, instance);
        }

        private static void SetField<T>(ScriptableSingletonDatabase database, string fieldName, T value)
        {
            var field = typeof(ScriptableSingletonDatabase).GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            Assert.That(field, Is.Not.Null);
            field.SetValue(database, value);
        }

        private sealed class TestScriptableSingleton : Singleton<TestScriptableSingleton> { }
    }
}
