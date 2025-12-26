using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Define;

public class PlayerSoundManager : MonoBehaviour
{
    // Select
    // 액션을 선택했을 때
    [Header("Select")]
    public AudioClip m_SelectUnitAudioClip;
    public AudioClip m_SelectAction_CommandMoveAudioClip;
    public AudioClip m_SelectAction_CommandAttackAudioClip;

    // Command
    // 선택한 액션을 유닛에게 명령 했을 때
    [Header("Command")]
    public AudioClip m_CommandAction_CommandMoveAudioClip;
    public AudioClip m_CommandAction_CommandAttackAudioClip;

    public void Awake()
    {
        Managers.Selection.OnSelectionChanged += PlaySound_SelectUnit;
        Managers.Command.OnCommandAction += PlaySound_CommandAction;
        Managers.Command.OnSelectedActionChanged += PlaySound_SelectAction;
    }

    public void PlaySound_SelectUnit(object sender, EventArgs e)
    {
        Managers.Sound.Play(m_SelectUnitAudioClip);
    }

    public void PlaySound_SelectAction(object sender, CommandManager.OnCommandActionEventArgs e)
    {
        // CommandMove, CommandAttack은 별도 선택이 없다.
        if(e.action == typeof(CommandMoveAction))
        {
            Managers.Sound.Play(m_SelectAction_CommandMoveAudioClip);
        }

        if(e.action == typeof(CommandAttackAction))
        {
            Managers.Sound.Play(m_SelectAction_CommandAttackAudioClip);
        }
    }

    public void PlaySound_CommandAction(object sender, CommandManager.OnCommandActionEventArgs e)
    {
        if(e.action == typeof(CommandMoveAction))
        {
            Managers.Sound.Play(m_CommandAction_CommandMoveAudioClip);
        }

        if(e.action == typeof(CommandAttackAction))
        {
            Managers.Sound.Play(m_CommandAction_CommandAttackAudioClip);
        }
    }
}

