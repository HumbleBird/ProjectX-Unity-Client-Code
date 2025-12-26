using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EscapeUI : MonoBehaviour
{
    [SerializeField] private Button m_gototheCampBtn;

    // Start is called before the first frame update
    void Start()
    {
        m_gototheCampBtn.onClick.AddListener(async () => 
        {
            if(Managers.Scene.CurrentScene.isSaveFile)
                await Managers.Save.SaveAllData();
            Managers.Scene.LoadScene(Define.Scene.Camp);
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
