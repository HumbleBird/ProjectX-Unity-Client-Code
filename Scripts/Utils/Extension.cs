using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public static class Extension
{
	public static T GetOrAddComponent<T>(this GameObject go) where T : UnityEngine.Component
	{
		return Util.GetOrAddComponent<T>(go);
	}

	public static void BindEvent(this GameObject go, Action action, Define.UIEvent type = Define.UIEvent.Click)
	{
		UI_Base.BindEvent(go, action, type);
	}

	public static void BindEventRemove(this GameObject go, Define.UIEvent type = Define.UIEvent.Click)
	{
		UI_Base.BindEventRemove(go, type);
	}

	public static bool IsValid(this GameObject go)
	{
		return go != null && go.activeSelf;
	}

	public static bool TryGetComponentInChildren<T>(this GameObject go, out T result) where T : Component
    {
		return Util.TryGetComponentInChildren<T>(go, out result);
    }


}
