#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;
using static Define;

[CustomEditor(typeof(GridSystemVisual))]
public class GridSystemVisualEditor : Editor
{
    private const float COLOR_BOX_SIZE = 20f;

    private readonly E_GridVisualType_Intensity[] _intensityOrder =
    {
        E_GridVisualType_Intensity.Light,
        E_GridVisualType_Intensity.Medium,
        E_GridVisualType_Intensity.Strong
    };

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GridSystemVisual visual = (GridSystemVisual)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("🎨 Material Cache (Editable Color Preview)", EditorStyles.boldLabel);

        if (visual == null || visual._materialCache == null)
        {
            EditorGUILayout.HelpBox("_materialCache is empty or not initialized.", MessageType.Info);
            return;
        }

        foreach (var kvp in visual._materialCache)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(kvp.Key.ToString(), EditorStyles.boldLabel);

            foreach (var intensity in _intensityOrder)
            {
                if (!kvp.Value.ContainsKey(intensity)) continue;
                Material mat = kvp.Value[intensity];
                if (mat == null) continue;

                // 현재 머티리얼 색상 가져오기
                Color currentColor = GetMaterialColor(mat);
                Color.RGBToHSV(currentColor, out float h, out float s, out float v);
                float a = currentColor.a;

                EditorGUILayout.BeginVertical("box");

                // 머티리얼 표시
                EditorGUILayout.BeginHorizontal();
                Rect colorRect = GUILayoutUtility.GetRect(COLOR_BOX_SIZE, COLOR_BOX_SIZE);
                EditorGUI.DrawRect(colorRect, currentColor);
                EditorGUILayout.LabelField(intensity.ToString(), GUILayout.Width(70));
                EditorGUILayout.ObjectField(mat, typeof(Material), false);
                EditorGUILayout.EndHorizontal();

                // 🎨 색상 선택
                Color newColor = EditorGUILayout.ColorField("Color", currentColor);

                // 🌈 HSV & Alpha 조절
                EditorGUI.BeginChangeCheck();
                h = EditorGUILayout.Slider("Hue", h, 0f, 1f);
                s = EditorGUILayout.Slider("Saturation", s, 0f, 1f);
                v = EditorGUILayout.Slider("Value", v, 0f, 1f);
                a = EditorGUILayout.Slider("Alpha", a, 0f, 1f);

                // 🎛 Reset & Apply 버튼
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Reset"))
                {
                    Undo.RecordObject(mat, "Reset Material Color");
                    Color resetColor = visual.gridVisualTypeMaterialList
                        .FirstOrDefault(x => x.gridVisualType == kvp.Key).material.color;
                    SetMaterialColor(mat, resetColor);
                    EditorUtility.SetDirty(mat);
                }

                if (GUILayout.Button("Apply"))
                {
                    Undo.RecordObject(visual, "Apply Material Cache");

                    // _materialCache에 현재 색 반영
                    visual._materialCache[kvp.Key][intensity] = mat;

                    EditorUtility.SetDirty(visual);
                    Debug.Log($"✅ Applied {kvp.Key} ({intensity}) to _materialCache");
                }
                EditorGUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck() || newColor != currentColor)
                {
                    Undo.RecordObject(mat, "Adjust Material HSV");

                    if (newColor != currentColor)
                        Color.RGBToHSV(newColor, out h, out s, out v);

                    Color updated = Color.HSVToRGB(h, s, v);
                    updated.a = a;

                    SetMaterialColor(mat, updated);
                    EditorUtility.SetDirty(mat);
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }
    }

    private static Color GetMaterialColor(Material mat)
    {
        if (mat == null) return Color.magenta;
        if (mat.HasProperty("_BaseColor")) return mat.GetColor("_BaseColor");
        if (mat.HasProperty("_Color")) return mat.color;
        return Color.magenta;
    }

    private static void SetMaterialColor(Material mat, Color c)
    {
        if (mat == null) return;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", c);
        else if (mat.HasProperty("_Color"))
            mat.color = c;
    }
}
#endif
