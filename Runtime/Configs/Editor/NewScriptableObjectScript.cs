using System.Collections.Generic;
using Basic;
using NaughtyAttributes;
using UnityEngine;

namespace Test
{
    [System.Serializable]
    public class TestID : GUIDBasedConfigID
    {
        public static implicit operator TestID(GUID id) => new() { _guid = id };

        public override void GetNames(List<string> list) =>
            NewScriptableObjectScript.Instance.GetNames(list);

        public override int GUIDToIndex(GUID guid) =>
            NewScriptableObjectScript.Instance.GUIDToIndex(guid);

        public override GUID IndexToGUID(int index) =>
            NewScriptableObjectScript.Instance.IndexToGUID(index);

        public override IConfig IndexToConfig(int newIndex) =>
            NewScriptableObjectScript.Instance.IndexToConfig(newIndex);
    }

    [System.Serializable]
    public class TestConfig : IConfig
    {
        [field: AllowNesting, SerializeField, ReadOnly]
        public GUID GUID { get; set; }
        public string Name;
        public string DEBUG_Name => Name;
        public GUIDBasedConfigID ConfigID => (TestID)GUID;
    }

    [CreateAssetMenu(
        fileName = "NewScriptableObjectScript",
        menuName = "Scriptable Objects/NewScriptableObjectScript"
    )]
    public class NewScriptableObjectScript : Database<NewScriptableObjectScript, TestConfig>
    {
        [field: SerializeField]
        public List<TestConfig> testConfigs;

        [field: SerializeField]
        public TestID TestID { get; private set; }

        public override IList<TestConfig> Configs => testConfigs;
    }
}
