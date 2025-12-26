using UnityEngine;

public static class ColorUtil
{
    public static Color GetNormalDamage() =>    GetColor("#FFFFFF");

    public static Color GetCriticalHit() => GetColor("#FF5555");

    public static Color GetMissOrEvasion() => GetColor("#C0C0C0");

    public static Color GetHeal() => GetColor("#66FF66");

    private static Color GetColor(string htmlColor)
    {
        // # 없으면 추가
        if (htmlColor.StartsWith("#") == false)
            htmlColor = "#" + htmlColor;

        if (ColorUtility.TryParseHtmlString(htmlColor, out Color color))
            return color;

        Debug.LogWarning($"색상 값을 파싱하지 못했습니다: {htmlColor}.");
        return Color.magenta;
    }
}