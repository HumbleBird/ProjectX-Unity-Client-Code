using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DelayTimer : StateMachineBehaviour
{
    public float delayTime = 0.3f;
    float timer;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        timer = 0f;
        animator.speed = 0f; // 현재 포즈에서 멈추기
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        timer += Time.deltaTime;
        if (timer >= delayTime)
        {
            animator.speed = 1f;
            animator.SetTrigger("DelayEnd");
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.speed = 1f; // 혹시 모르니 항상 복구
    }
}

