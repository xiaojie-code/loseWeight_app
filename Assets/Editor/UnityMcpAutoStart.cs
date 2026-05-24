using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace LoseWeightApp.EditorTools
{
    [InitializeOnLoad]
    internal static class UnityMcpAutoStart
    {
        static UnityMcpAutoStart()
        {
            EditorApplication.delayCall += TryStartServer;
        }

        private static void TryStartServer()
        {
            try
            {
                string[] typeNames =
                {
                    "UnityMCP.Editor.UnityMCPMain, UnityMCP",
                    "UnityMCPMain, UnityMCP"
                };

                string[] methodNames =
                {
                    "CheckAndAutoRestartServer",
                    "DoAutoStartServer",
                    "StartServer"
                };

                foreach (var typeName in typeNames)
                {
                    var mainType = Type.GetType(typeName);
                    if (mainType == null) continue;

                    foreach (var methodName in methodNames)
                    {
                        var startServer = mainType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                        if (startServer == null) continue;

                        startServer.Invoke(null, null);
                        Debug.Log($"[UnityMCP] Auto-start invoked {typeName}.{methodName} from project editor helper.");
                        return;
                    }
                }

                Debug.LogWarning("[UnityMCP] Auto-start helper could not find UnityMCPMain start method.");
            }
            catch (TargetInvocationException ex)
            {
                Debug.LogWarning($"[UnityMCP] Auto-start skipped: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UnityMCP] Auto-start helper failed: {ex.Message}");
            }
        }
    }
}