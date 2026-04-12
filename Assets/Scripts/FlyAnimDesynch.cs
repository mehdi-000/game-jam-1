using UnityEngine;

[RequireComponent(typeof(Animator))]

public class FlyAnimDesynch : MonoBehaviour
{
    void Start()
    {
        Animator anim= GetComponent<Animator>();
        float randomTime = Random.value;
        anim.speed = Random.Range(0.5f, 1f);
        anim.Update(0f);
        anim.Play(0, 0, randomTime);
        anim.Update(0f);
    }
}
