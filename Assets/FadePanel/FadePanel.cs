using System;
using System.Collections;
using UnityEngine;

public class FadePanel : MonoBehaviour
{

    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void FadeOut(Action onDoneFading = null, float timeToFade = 1f)
    {
        StartCoroutine(FadeOutCo(timeToFade, onDoneFading));
    }

    IEnumerator FadeOutCo(float timeToFade, Action onDoneFading)
    {
        _animator.Play("FadeOut");
        _animator.speed = 1f / timeToFade;

        yield return new WaitForSeconds(timeToFade);

        onDoneFading?.Invoke();
    }

    public void FadeIn(Action onDoneFading = null, float timeToFade = 1f)
    {
        StartCoroutine(FadeInCo(timeToFade, onDoneFading));
    }

    IEnumerator FadeInCo(float timeToFade, Action onDoneFading)
    {
        _animator.Play("FadeIn");
        _animator.speed = 1f / timeToFade;

        yield return new WaitForSeconds(timeToFade);

        onDoneFading?.Invoke();
    }
}
