using UnityEngine;

namespace Basic.Logger
{
    [CreateAssetMenu(fileName = "LoggerSettingsContainer", menuName = "Modules/Logger/Settings")]
    public class LoggerSettingsContainer
        : SettingsContainer<LoggerSettingsContainer, LoggerSettings> { }
}
