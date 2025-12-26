using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static Define;

public class UIManager
{
    int _order = 10;

    public Stack<UI_Popup> _popupStack = new Stack<UI_Popup>();
    public UI_Scene SceneUI { get; set; }

    public GameObject Root
    {
        get
        {
			GameObject root = GameObject.Find("@UI_Root");
			if (root == null)
				root = new GameObject { name = "@UI_Root" };
            return root;
		}
    }

    public void SetCanvas(GameObject go, bool sort = true)
    {
        Canvas canvas = Util.GetOrAddComponent<Canvas>(go);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;

        if (sort)
        {
            canvas.sortingOrder = _order;
            _order++;
        }
        else
        {
            canvas.sortingOrder = 0;
        }
    }

	public T MakeWorldSpaceUI<T>(Transform parent = null, string name = null) where T : UI_Base
	{
		if (string.IsNullOrEmpty(name))
			name = typeof(T).Name;

		GameObject go = Managers.Resource.Instantiate($"UI/WorldSpace/{name}");
		if (parent != null)
			go.transform.SetParent(parent);

        Canvas canvas = go.GetOrAddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;

		return Util.GetOrAddComponent<T>(go);
	}

	public T MakeSubItem<T>(Transform parent = null, string name = null) where T : UI_Base
	{
		if (string.IsNullOrEmpty(name))
			name = typeof(T).Name;

		GameObject go = Managers.Resource.Instantiate($"UI/SubItem/{name}");
		if (parent != null)
			go.transform.SetParent(parent);

		return Util.GetOrAddComponent<T>(go);
	}

    public T ShowSceneUI<T>(string name = null) where T : UI_Scene
	{
		if (string.IsNullOrEmpty(name))
			name = typeof(T).Name;

		GameObject go = Managers.Resource.Instantiate($"UI/Scene/{name}");
		T sceneUI = Util.GetOrAddComponent<T>(go);
        SceneUI = sceneUI;

		go.transform.SetParent(Root.transform);

		return sceneUI;
	}

	public T ShowPopupUI<T>(string name = null) where T : UI_Popup
    {
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;

        GameObject go = Managers.Resource.Instantiate($"UI/Popup/{name}");
        T popup = Util.GetOrAddComponent<T>(go);
        _popupStack.Push(popup);

        go.transform.SetParent(Root.transform);

		return popup;
    }

    public void ClosePopupUI(UI_Popup popup)
    {
		if (_popupStack.Count == 0)
			return;
        
        if (_popupStack.Peek() != popup)
        {
            Debug.Log("Close Popup Failed!");
            return;
        }

        ClosePopupUI();
    }

    public void ClosePopupUI<T>(string name = null) where T : UI_Popup
    {
        if (_popupStack.Count == 0)
            return;

        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;

        foreach (UI_Popup pop in _popupStack)
        {
            if(pop.name == name)
            {
                pop.ClosePopupUI();
                return;
            }
        }

        Debug.Log("Close Popup Failed!");
    }

    public void ClosePopupUI()
    {
        if (_popupStack.Count == 0)
            return;

        UI_Popup popup = _popupStack.Pop();
        Managers.Resource.Destroy(popup.gameObject);
        popup = null;
        _order--;
    }

    public void CloseAllPopupUI()
    {
        while (_popupStack.Count > 0)
            ClosePopupUI();
    }

    public void Clear()
    {
        CloseAllPopupUI();
        SceneUI = null;
    }

    public T GetUIBase<T>(string name = null) where T : UI_Base
    {
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;

        // Stack<T>는 IEnumerable<T>라서 LINQ 사용 가능
        return _popupStack
            .OfType<T>() // 타입이 T인 것만 필터링
            .FirstOrDefault(x => x.name == name); // 이름 일치하는 것 반환
    }




    // ─────────────────────────────
    // 🔹 자식 포함 FadeIn / FadeOut
    // ─────────────────────────────
    public void FadeInWithChildren(GameObject obj, float duration, EColorMode mode = EColorMode.RGB)
    {
        if (obj == null) return;

        Image[] images = obj.GetComponentsInChildren<Image>(includeInactive: true);
        foreach (var img in images)
        {
            FadeIn(img, duration, mode);
        }
    }

    public void FadeOutWithChildren(GameObject obj, float duration, EColorMode mode = EColorMode.RGB)
    {
        if (obj == null) return;

        Image[] images = obj.GetComponentsInChildren<Image>(includeInactive: true);
        foreach (var img in images)
        {
            FadeOut(img, duration, mode);
        }
    }

    /// <summary>
    /// 이미지 페이드 인 (서서히 나타나기)
    /// </summary>
    /// <param name="image">UI Image</param>
    /// <param name="duration">시간(초)</param>
    /// <param name="colorMode">색상 모드 (RGB, RGB01, HSV)</param>
    public void FadeIn(Image image, float duration, EColorMode colorMode = EColorMode.RGB)
    {
        Managers.SceneServices.CoroutineRunner.Run(FadeRoutine(image, duration, true, colorMode));
    }

    /// <summary>
    /// 이미지 페이드 아웃 (서서히 사라지기)
    /// </summary>
    public  void FadeOut(Image image, float duration, EColorMode colorMode = EColorMode.RGB)
    {
        Managers.SceneServices.CoroutineRunner.Run(FadeRoutine(image, duration, false, colorMode));
    }

    public IEnumerator FadeRoutine(Image image, float duration, bool fadeIn, EColorMode colorMode)
    {
        if (image == null) yield break;

        float timer = 0f;
        Color startColor = image.color;
        float startAlpha = fadeIn ? 0f : startColor.a;
        float endAlpha = fadeIn ? 1f : 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float a = Mathf.Lerp(startAlpha, endAlpha, t);

            image.color = GetColorWithAlpha(image.color, a, colorMode);
            yield return null;
        }

        image.color = GetColorWithAlpha(image.color, endAlpha, colorMode);
    }

    public  Color GetColorWithAlpha(Color baseColor, float alpha, EColorMode mode)
    {
        switch (mode)
        {
            case EColorMode.RGB:
                // 일반적인 0~255 RGB를 Color(0~1)로 변환 후 적용
                return new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

            case EColorMode.RGB01:
                // 이미 0~1 범위의 색상일 경우
                return new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

            case EColorMode.HSV:
                Color.RGBToHSV(baseColor, out float h, out float s, out float v);
                Color hsvColor = Color.HSVToRGB(h, s, v);
                hsvColor.a = alpha;
                return hsvColor;

            default:
                return new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        }
    }

    /// <summary>
    /// 이미지의 색상 설정 (알파 포함)
    /// </summary>
    /// <param name="image">UI Image</param>
    /// <param name="color">설정할 색상 (RGBA)</param>
    /// <param name="mode">색상 모드 (RGB, RGB01, HSV)</param>
    public void SetColor(Image image, Color color, EColorMode mode)
    {
        if (image == null) return;

        switch (mode)
        {
            case EColorMode.RGB:
            case EColorMode.RGB01:
                // Color는 이미 0~1 범위의 값으로 설정됩니다.
                image.color = color;
                break;

            case EColorMode.HSV:
                // HSV 모드에서는 일반적으로 색상 자체(RGB)보다는
                // 투명도(Alpha)를 변경할 때 사용되지만,
                // 여기서는 입력된 Color의 HSV 값에 입력된 Color의 Alpha를 적용합니다.
                Color.RGBToHSV(color, out float h, out float s, out float v);
                Color hsvColor = Color.HSVToRGB(h, s, v);
                hsvColor.a = color.a; // 입력된 color의 알파 값을 적용
                image.color = hsvColor;
                break;

            default:
                image.color = color;
                break;
        }
    }

    // ─────────────────────────────
    // 🔹 자식 포함 SetColor
    // ─────────────────────────────
    /// <summary>
    /// GameObject와 자식들의 Image 색상 일괄 설정
    /// </summary>
    /// <param name="obj">대상 GameObject</param>
    /// <param name="color">설정할 색상 (RGBA)</param>
    /// <param name="mode">색상 모드 (RGB, RGB01, HSV)</param>
    public void SetColorWithChildren(GameObject obj, Color color, EColorMode mode = EColorMode.RGB)
    {
        if (obj == null) return;

        // obj 자신과 모든 자식에서 Image 컴포넌트를 가져옵니다 (비활성화된 것도 포함).
        Image[] images = obj.GetComponentsInChildren<Image>(includeInactive: true);

        foreach (var img in images)
        {
            SetColor(img, color, mode);
        }
    }

    /// <summary>
    /// 이미지의 투명도(Alpha)만 설정합니다.
    /// </summary>
    /// <param name="image">UI Image</param>
    /// <param name="alpha">설정할 투명도 (0.0f ~ 1.0f)</param>
    /// <param name="mode">색상 모드 (RGB, RGB01, HSV)</param>
    public void SetColorAlpha(Image image, float alpha, EColorMode mode = EColorMode.RGB)
    {
        if (image == null) return;

        // GetColorWithAlpha 메서드를 사용하여 새 알파 값을 적용한 Color를 얻습니다.
        image.color = GetColorWithAlpha(image.color, alpha, mode);
    }

    /// <summary>
    /// GameObject와 자식들의 Image 투명도(Alpha) 일괄 설정
    /// </summary>
    /// <param name="obj">대상 GameObject</param>
    /// <param name="alpha">설정할 투명도 (0.0f ~ 1.0f)</param>
    /// <param name="mode">색상 모드 (RGB, RGB01, HSV)</param>
    public void SetColorAlphaWithChildren(GameObject obj, float alpha, EColorMode mode = EColorMode.RGB)
    {
        if (obj == null) return;

        Image[] images = obj.GetComponentsInChildren<Image>(includeInactive: true);

        foreach (var img in images)
        {
            SetColorAlpha(img, alpha, mode);
        }
    }
}
