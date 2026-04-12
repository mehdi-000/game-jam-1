using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class MainMenuController : MonoBehaviour
{
    const float swooshCoverage = 0.6f;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        var buttons = root.Query<Button>().ToList();

        StartCoroutine(StaggerEntrance(buttons));

        root.Q<Button>("PlayButton").RegisterCallback<ClickEvent>(_ =>
        {
            SceneManager.LoadScene("SampleScene");
        });

        root.Q<Button>("BoostedButton").RegisterCallback<ClickEvent>(_ =>
        {
            SceneManager.LoadScene("Boosted");
        });

        root.Q<Button>("QuitButton").RegisterCallback<ClickEvent>(_ => Quit());

        foreach (Button btn in buttons)
        {
            var swoosh = btn.Q<VisualElement>(className: "btn-swoosh");
            btn.RegisterCallback<MouseEnterEvent>(_ =>
            {
                StartCoroutine(SwooshIn(swoosh, btn));
            });
            btn.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                StartCoroutine(SwooshOut(swoosh));
            });
        }
    }

    private IEnumerator StaggerEntrance(List<Button> buttons)
    {
        foreach (var btn in buttons)
        {
            btn.style.opacity = 0f;
            btn.transform.position = new Vector3(-60f, 0f, 0f);
        }

        yield return new WaitForSeconds(0.25f);

        foreach (var btn in buttons)
        {
            StartCoroutine(SlideIn(btn));
            yield return new WaitForSeconds(0.09f);
        }
    }

    private static IEnumerator SlideIn(VisualElement el)
    {
        const float duration = 0.28f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            el.style.opacity = t;
            el.transform.position = new Vector3(Mathf.Lerp(-60f, 0f, t), 0f, 0f);
            yield return null;
        }
        el.style.opacity = 1f;
        el.transform.position = Vector3.zero;
    }

    private IEnumerator SwooshIn(VisualElement swoosh, VisualElement btn)
    {
        const float duration = 0.14f;
        float elapsed = 0f;
        float target = btn.resolvedStyle.width * swooshCoverage;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            swoosh.style.width = target * t;
            yield return null;
        }
        swoosh.style.width = target;
    }

    private static IEnumerator SwooshOut(VisualElement swoosh)
    {
        const float duration = 0.08f;
        float start = swoosh.resolvedStyle.width;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            swoosh.style.width = Mathf.Lerp(start, 0f, t);
            yield return null;
        }
        swoosh.style.width = 0;
    }

    private static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
