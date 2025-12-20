using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace Basic.EventLoop
{
    public class EventLoopManager
    {
        private class CustomUpdate { }

        private class CustomLateUpdate { }

        private static List<PlayerLoopSystem> _insertedSystems = new();

        // Public API
        public static void SetupGameLoop(
            System.Action updateCallback,
            System.Action lateUpdateCallback
        )
        {
            var defaultLoop = PlayerLoop.GetCurrentPlayerLoop();

            var customUpdate = new PlayerLoopSystem()
            {
                subSystemList = null,
                updateDelegate = new(updateCallback),
                type = typeof(CustomUpdate),
            };

            var loopWithCustomUpdate = InsertSystemBefore<Update>(in defaultLoop, customUpdate);

            var customLateUpdate = new PlayerLoopSystem()
            {
                subSystemList = null,
                updateDelegate = new(lateUpdateCallback),
                type = typeof(CustomLateUpdate),
            };

            var loopWithCustomLateUpdate = InsertSystemBefore<PreLateUpdate>(
                in loopWithCustomUpdate,
                customLateUpdate
            );

            PlayerLoop.SetPlayerLoop(loopWithCustomLateUpdate);
        }

        public static void DumpCurrentPlayerLoop()
        {
            System.Text.StringBuilder sb = new();
            RecursivePlayerLoopPrint(PlayerLoop.GetCurrentPlayerLoop(), sb, 0);
            Debug.Log(sb.ToString());
        }

        public static void CleanupGameLoop()
        {
            foreach (var playerLoopSystem in _insertedSystems)
                TryRemoveSystem(playerLoopSystem.type);

            _insertedSystems.Clear();
        }

        // Private functions
        private static bool TryRemoveSystem(System.Type type)
        {
            if (type == null)
                throw new System.ArgumentNullException(
                    nameof(type),
                    "Trying to remove a null type!"
                );

            var currentSystem = PlayerLoop.GetCurrentPlayerLoop();
            var couldRemove = TryRemoveTypeFrom(ref currentSystem, type);
            PlayerLoop.SetPlayerLoop(currentSystem);
            return couldRemove;
        }

        private static bool TryRemoveTypeFrom(ref PlayerLoopSystem currentSystem, System.Type type)
        {
            var subSystems = currentSystem.subSystemList;
            if (subSystems == null)
                return false;

            for (int i = 0; i < subSystems.Length; i++)
            {
                if (subSystems[i].type == type)
                {
                    var newSubSystems = new PlayerLoopSystem[subSystems.Length - 1];

                    System.Array.Copy(subSystems, newSubSystems, i);
                    System.Array.Copy(
                        subSystems,
                        i + 1,
                        newSubSystems,
                        i,
                        subSystems.Length - i - 1
                    );

                    currentSystem.subSystemList = newSubSystems;

                    return true;
                }

                if (TryRemoveTypeFrom(ref subSystems[i], type))
                    return true;
            }

            return false;
        }

        private static PlayerLoopSystem InsertSystemAfter<T>(
            in PlayerLoopSystem loopSystem,
            PlayerLoopSystem newSystem
        )
            where T : struct
        {
            PlayerLoopSystem newPlayerLoop = new()
            {
                loopConditionFunction = loopSystem.loopConditionFunction,
                type = loopSystem.type,
                updateDelegate = loopSystem.updateDelegate,
                updateFunction = loopSystem.updateFunction,
            };
            List<PlayerLoopSystem> newSubSystemList = new();

            if (loopSystem.subSystemList != null)
            {
                for (var i = 0; i < loopSystem.subSystemList.Length; i++)
                {
                    newSubSystemList.Add(loopSystem.subSystemList[i]);
                    if (loopSystem.subSystemList[i].type == typeof(T))
                    {
                        newSubSystemList.Add(newSystem);
                    }
                }
            }

            newPlayerLoop.subSystemList = newSubSystemList.ToArray();
            _insertedSystems.Add(newSystem);
            return newPlayerLoop;
        }

        private static PlayerLoopSystem InsertSystemBefore<T>(
            in PlayerLoopSystem loopSystem,
            PlayerLoopSystem newSystem
        )
            where T : struct
        {
            PlayerLoopSystem newPlayerLoop = new()
            {
                loopConditionFunction = loopSystem.loopConditionFunction,
                type = loopSystem.type,
                updateDelegate = loopSystem.updateDelegate,
                updateFunction = loopSystem.updateFunction,
            };
            List<PlayerLoopSystem> newSubSystemList = new();

            if (loopSystem.subSystemList != null)
            {
                for (var i = 0; i < loopSystem.subSystemList.Length; i++)
                {
                    if (loopSystem.subSystemList[i].type == typeof(T))
                    {
                        newSubSystemList.Add(newSystem);
                    }
                    newSubSystemList.Add(loopSystem.subSystemList[i]);
                }
            }

            newPlayerLoop.subSystemList = newSubSystemList.ToArray();
            _insertedSystems.Add(newSystem);
            return newPlayerLoop;
        }

        private static void RecursivePlayerLoopPrint(
            PlayerLoopSystem playerLoopSystem,
            System.Text.StringBuilder sb,
            int depth
        )
        {
            if (depth == 0)
            {
                sb.AppendLine("ROOT NODE");
            }
            else if (playerLoopSystem.type != null)
            {
                for (int i = 0; i < depth; i++)
                {
                    sb.Append("\t");
                }
                sb.AppendLine(playerLoopSystem.type.Name);
            }
            if (playerLoopSystem.subSystemList != null)
            {
                depth++;
                foreach (var s in playerLoopSystem.subSystemList)
                {
                    RecursivePlayerLoopPrint(s, sb, depth);
                }
                depth--;
            }
        }
    }
}
