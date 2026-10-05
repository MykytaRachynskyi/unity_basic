using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Basic.Singleton.Tests
{
    [TestFixture]
    public class ScriptableSingletonDatabaseTests
    {
        private ScriptableSingletonDatabase _previousInstance;
        private Dictionary<int, Singleton> _previousMap;

        [SetUp]
        public void SetUp()
        {
            _previousInstance = GetStaticInstance();
            _previousMap = GetSingletonMap();
        }

        [TearDown]
        public void TearDown()
        {
            SetStaticInstance(_previousInstance);
            SetSingletonMap(_previousMap);
            SetRecreatingMap(false);
        }

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

            SetStaticInstance(database);
            try
            {
                var groups = ScriptableSingletonDatabase.GetGroups();

                Assert.That(groups, Is.EqualTo(new[] { "Configs", "Gameplay" }));
            }
            finally
            {
                Object.DestroyImmediate(database);
            }
        }

        [Test]
        public void GetGroups_NullGroupsList_ReturnsEmpty()
        {
            var database = ScriptableObject.CreateInstance<ScriptableSingletonDatabase>();
            SetField(database, "groups", (List<string>)null);

            SetStaticInstance(database);
            try
            {
                Assert.That(ScriptableSingletonDatabase.GetGroups(), Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(database);
            }
        }

        [Test]
        public void BuildSingletonMap_SkipsNullEntries_AndMapsNonNullSingletons()
        {
            var singleton = ScriptableObject.CreateInstance<TestScriptableSingleton>();

            LogAssert.Expect(
                LogType.Warning,
                new Regex(@"ScriptableSingletonDatabase\.allSingletons\[0\] is null and was skipped")
            );

            var map = InvokeBuildSingletonMap(new List<Singleton> { null, singleton });

            Assert.That(map.Count, Is.EqualTo(1));
            Assert.That(map[singleton.GetType().GetHashCode()], Is.SameAs(singleton));

            Object.DestroyImmediate(singleton);
        }

        [Test]
        public void GetSingleton_NullDatabaseInstance_ReturnsNullWithoutThrowing()
        {
            SetStaticInstance(null);
            SetSingletonMap(null);

            LogAssert.Expect(
                LogType.Error,
                "Failed to load Scriptable Singleton Database from asset database."
            );
            // May also emit formatted Log.Error for the missing type and/or logger settings
            // during first-time Log bootstrap — ignore those so the soft-fail is the focus.
            LogAssert.ignoreFailingMessages = true;
            try
            {
                TestScriptableSingleton result = null;
                Assert.DoesNotThrow(
                    () =>
                        result = ScriptableSingletonDatabase.GetSingleton<TestScriptableSingleton>()
                );

                Assert.That(result, Is.Null);
                Assert.That(GetSingletonMap(), Is.Not.Null);
                Assert.That(GetSingletonMap().Count, Is.Zero);
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
            }
        }

        [Test]
        public void Refresh_NullDatabaseInstance_DoesNotThrow()
        {
            SetStaticInstance(null);

            LogAssert.Expect(
                LogType.Error,
                "Failed to load Scriptable Singleton Database from asset database."
            );

            Assert.DoesNotThrow(ScriptableSingletonDatabase.Refresh);
        }

        [Test]
        public void RecreateSingletonMap_ReentrantCall_DoesNotThrow()
        {
            var database = ScriptableObject.CreateInstance<ScriptableSingletonDatabase>();
            var singleton = ScriptableObject.CreateInstance<TestScriptableSingleton>();
            SetField(database, "allSingletons", new List<Singleton> { singleton });
            SetStaticInstance(database);
            SetSingletonMap(null);

            SetRecreatingMap(true);
            try
            {
                Assert.DoesNotThrow(InvokeRecreateSingletonMap);
                Assert.That(GetSingletonMap(), Is.Not.Null);
                Assert.That(GetSingletonMap().Count, Is.Zero);
            }
            finally
            {
                SetRecreatingMap(false);
                Object.DestroyImmediate(singleton);
                Object.DestroyImmediate(database);
            }
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

        private static void InvokeRecreateSingletonMap()
        {
            var method = typeof(ScriptableSingletonDatabase).GetMethod(
                "RecreateSingletonMap",
                BindingFlags.NonPublic | BindingFlags.Static
            );
            Assert.That(method, Is.Not.Null);
            method.Invoke(null, null);
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

        private static Dictionary<int, Singleton> GetSingletonMap()
        {
            var field = typeof(ScriptableSingletonDatabase).GetField(
                "_singletonMap",
                BindingFlags.NonPublic | BindingFlags.Static
            );
            Assert.That(field, Is.Not.Null);
            return (Dictionary<int, Singleton>)field.GetValue(null);
        }

        private static void SetSingletonMap(Dictionary<int, Singleton> map)
        {
            var field = typeof(ScriptableSingletonDatabase).GetField(
                "_singletonMap",
                BindingFlags.NonPublic | BindingFlags.Static
            );
            Assert.That(field, Is.Not.Null);
            field.SetValue(null, map);
        }

        private static void SetRecreatingMap(bool value)
        {
            var field = typeof(ScriptableSingletonDatabase).GetField(
                "_recreatingMap",
                BindingFlags.NonPublic | BindingFlags.Static
            );
            Assert.That(field, Is.Not.Null);
            field.SetValue(null, value);
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
