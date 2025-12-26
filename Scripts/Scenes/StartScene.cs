using Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static Define;

public class StartScene : BaseScene
{


    StartScene()
    {
        SceneType = Define.Scene.Start;
    }

    [SerializeField] private MenuUI m_MenuUI;
    [SerializeField] bool m_IsSkip = false;

    [Header("Company")]
    [SerializeField] private AudioClip m_CompanyTitleSound;
    [SerializeField] private Image m_CompanyTitleBG;
    [SerializeField] private Image m_CompanyTitle;

    [Header("Show And Hide")]
    [SerializeField] private float m_ShowAndHideTime = 1f;
    [SerializeField] private float m_UIShowAndHideInterval = 3f;

    protected override void Init()
    {
        base.Init();

        SceneType = Define.Scene.Start;
    }

    protected override void Start()
    {
        base.Start();

        StartCoroutine(IProcessUI());

    }

    public void SkipIntro()
    {
        // 마우스 클릭 체크
        if(m_IsSkip == false)
        {
            m_IsSkip = true;

            StopAllCoroutines();

            CompleteUI();
        }
    }

    private IEnumerator IProcessUI()
    {
        m_MenuUI.gameObject.SetActive(false);
        m_CompanyTitle.enabled = false;

        // 회사 타이틀
        yield return new WaitForSeconds(m_UIShowAndHideInterval);
        m_CompanyTitle.enabled = true;
        Managers.UI.FadeIn(m_CompanyTitle, m_ShowAndHideTime, EColorMode.HSV);

        // 페이드 효과가 전부 끝나면 보글 사운드
        Managers.Sound.Play(m_CompanyTitleSound);

        // 대기
        yield return new WaitForSeconds(m_UIShowAndHideInterval);

        // 동시 진행
        Managers.UI.FadeOut(m_CompanyTitle, m_ShowAndHideTime, EColorMode.HSV);

        // 대기
        yield return new WaitForSeconds(m_UIShowAndHideInterval);

        Managers.UI.FadeOut(m_CompanyTitleBG, m_ShowAndHideTime, EColorMode.HSV);


        yield return new WaitForSeconds(1f);

        // 페이드 효과가 전부 끝나면  메인 UI
        m_MenuUI.gameObject.SetActive(true);
        Managers.UI.FadeInWithChildren(m_MenuUI.gameObject, m_ShowAndHideTime, EColorMode.HSV);
        m_MenuUI.m_Animator.Play("Show");

        yield return new WaitForSeconds(1f);

        // Sound
        CompleteUI();
    }

    private void CompleteUI()
    {
        Managers.UI.SetColorAlphaWithChildren(m_MenuUI.gameObject, 100, EColorMode.HSV);
        Managers.UI.SetColorAlpha(m_CompanyTitle, 0, EColorMode.HSV);
        Managers.UI.SetColorAlpha(m_CompanyTitleBG, 0, EColorMode.HSV);

        Managers.Sound.Play(m_SceneMainTemaAudioclip, 1, Sound.Bgm);
        m_MenuUI.gameObject.SetActive(true);
        m_MenuUI.m_Animator.Play("Empty");
        m_IsSkip = true;
    }

    public override void Clear()
    {

    }

    protected override void LoadSavedGame(SaveSlotData data)
    {
        base.LoadSavedGame(data);
    }

    protected override void LoadNewGame()
    {
        base.LoadNewGame();
    }
}
