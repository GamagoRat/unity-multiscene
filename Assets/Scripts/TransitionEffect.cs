using System.Collections;
using UnityEngine;

public abstract class TransitionEffect : MonoBehaviour
{
    public abstract IEnumerator FadeIn();
    public abstract IEnumerator FadeOut();
}
