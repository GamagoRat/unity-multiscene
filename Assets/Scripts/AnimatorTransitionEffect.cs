using System.Collections;
using UnityEngine;

public class AnimatorTransitionEffect : TransitionEffect
{
    [SerializeField] private Animator animationController;

    public override IEnumerator FadeIn()
    {
        animationController.SetTrigger("FadeIn");
        yield return new WaitForSeconds(1f);
    }

    public override IEnumerator FadeOut()
    {
        animationController.SetTrigger("FadeOut");
        yield return new WaitForSeconds(1f);
    }
}
