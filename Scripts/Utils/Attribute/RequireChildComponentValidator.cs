using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Reflection;

[InitializeOnLoad]
public static class RequireChildComponentValidator
{
    static RequireChildComponentValidator()
    {
        EditorApplication.hierarchyChanged += ValidateScene;
    }

    private static void ValidateScene()
    {
        var allMonoBehaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

        foreach (var mb in allMonoBehaviours)
        {
            if (mb == null) continue;

            var attrs = mb.GetType().GetCustomAttributes(typeof(RequireChildComponentAttribute), true) as RequireChildComponentAttribute[];

            if (attrs == null || attrs.Length == 0) continue;

            foreach (var attr in attrs)
            {
                foreach (var type in attr.RequiredTypes)
                {
                    var child = mb.GetComponentsInChildren(type, true)
                                  .FirstOrDefault(c => c.gameObject != mb.gameObject);

                    if (child == null)
                    {
                        //Debug.LogWarningFormat(mb,
                        //    $"[RequireChildComponent] {mb.GetType().Name} requires a child GameObject with component: {type.Name}, but none was found under {mb.gameObject.name}.");
                    }
                }
            }
        }
    }
}
