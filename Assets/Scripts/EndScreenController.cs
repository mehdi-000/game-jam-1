using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class EndScreenController : MonoBehaviour
{
    const float SwooshCoverage = 0.6f;
    const string LegacyHighScoreKey = "FrogstylerHighScore";
    const string BestFinalScoreKey = "FrogstylerBestFinalScore";

    [Header("Final score multiplier tuning")]
    [SerializeField] float scoreKInsects = 0.04f;
    [SerializeField] float scoreKTricks = 0.03f;
    [SerializeField] float scoreKDistance = 0.015f;
    [SerializeField] float scoreMinMultiplier = 1f;
    [SerializeField] float scoreMaxMultiplier = 8f;

    UIDocument _doc;
    bool _buttonsHooked;
    bool _migrationDone;

    void Awake()
    {
        _doc = GetComponent<UIDocument>();
    }

    public void PresentRunEnd()
    {
        MigrateLegacyBestOnce();
        StartCoroutine(PresentRunEndRoutine());
    }

    void MigrateLegacyBestOnce()
    {
        if (_migrationDone)
            return;
        _migrationDone = true;

        if (!PlayerPrefs.HasKey(BestFinalScoreKey) && PlayerPrefs.HasKey(LegacyHighScoreKey))
        {
            int legacy = PlayerPrefs.GetInt(LegacyHighScoreKey, 0);
            PlayerPrefs.SetInt(BestFinalScoreKey, legacy);
            PlayerPrefs.Save();
        }
    }

    int GetBestFinalScore()
    {
        return PlayerPrefs.GetInt(BestFinalScoreKey, 0);
    }

    void SaveBestIfImproved(int finalScore)
    {
        int best = GetBestFinalScore();
        if (finalScore <= best)
            return;

        PlayerPrefs.SetInt(BestFinalScoreKey, finalScore);
        PlayerPrefs.Save();
    }

    void EnsureButtonsRegistered()
    {
        if (_buttonsHooked)
            return;

        var root = _doc.rootVisualElement;
        if (root == null)
            return;

        root.Q<Button>("PlayAgainButton")?.RegisterCallback<ClickEvent>(_ =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        });

        root.Q<Button>("QuitButton")?.RegisterCallback<ClickEvent>(_ => Quit());

        foreach (Button btn in root.Query<Button>().ToList())
        {
            var swoosh = btn.Q<VisualElement>(className: "btn-swoosh");
            if (swoosh != null)
            {
                btn.RegisterCallback<MouseEnterEvent>(_ => StartCoroutine(SwooshIn(swoosh, btn)));
                btn.RegisterCallback<MouseLeaveEvent>(_ => StartCoroutine(SwooshOut(swoosh)));
            }
        }

        _buttonsHooked = true;
    }

    IEnumerator PresentRunEndRoutine()
    {
        yield return null;

        var root = _doc.rootVisualElement;
        if (root == null)
            yield break;

        EnsureButtonsRegistered();

        int trickPts = GameScore.CurrentScore;
        int insects = GameScore.CurrentInsectsEaten;
        int tricks = GameScore.TricksLandedCount;
        float dist = GameScore.DistanceX;

        var tuning = new RunScoreTuning
        {
            KInsects = scoreKInsects,
            KTricks = scoreKTricks,
            KDistance = scoreKDistance,
            MinMultiplier = scoreMinMultiplier,
            MaxMultiplier = scoreMaxMultiplier,
        };

        float mult = RunScoreCalculator.GetBonusMultiplier(insects, tricks, dist, tuning);
        int finalScore = RunScoreCalculator.ComputeFinalScore(trickPts, insects, tricks, dist, tuning);
        int bestBefore = GetBestFinalScore();

        var lblFlip = root.Q<Label>("BreakdownFlipPoints");
        var lblIns = root.Q<Label>("BreakdownInsects");
        var lblTrk = root.Q<Label>("BreakdownTricks");
        var lblDist = root.Q<Label>("BreakdownDistance");
        var lblMult = root.Q<Label>("BonusMultiplierValue");
        var lblFinal = root.Q<Label>("FinalRunValue");
        var lblBest = root.Q<Label>("FinalBestValue");

        if (lblFlip != null) lblFlip.text = "0";
        if (lblIns != null) lblIns.text = "0";
        if (lblTrk != null) lblTrk.text = "0";
        if (lblDist != null) lblDist.text = "0";
        if (lblMult != null) lblMult.text = "×1.00";
        if (lblFinal != null) lblFinal.text = "0";
        if (lblBest != null) lblBest.text = bestBefore.ToString();

        var titleBlock = root.Q<VisualElement>("TitleBlock");
        var breakdownBlock = root.Q<VisualElement>("BreakdownBlock");
        var multiplierRow = root.Q<VisualElement>("MultiplierRow");
        var finalBlock = root.Q<VisualElement>("FinalBlock");
        var buttons = root.Query<Button>().ToList();

        if (titleBlock != null)
        {
            titleBlock.style.opacity = 0f;
            titleBlock.transform.position = new Vector3(0f, -180f, 0f);
            titleBlock.transform.scale = new Vector3(0.75f, 0.75f, 1f);
        }

        foreach (var el in new[] { breakdownBlock, multiplierRow, finalBlock })
        {
            if (el != null)
            {
                el.style.opacity = 0f;
                el.transform.scale = new Vector3(0.85f, 0.85f, 1f);
            }
        }

        foreach (var btn in buttons)
        {
            btn.style.opacity = 0f;
            btn.transform.position = new Vector3(-70f, 0f, 0f);
        }

        if (titleBlock != null)
            yield return StartCoroutine(TitleBounceInUnscaled(titleBlock));

        yield return new WaitForSecondsRealtime(0.06f);

        if (breakdownBlock != null)
        {
            yield return StartCoroutine(ScalePopInUnscaled(breakdownBlock, 0.22f));
            yield return StartCoroutine(CountUpIntUnscaled(lblFlip, 0, trickPts, 0.38f));
            yield return StartCoroutine(CountUpIntUnscaled(lblIns, 0, insects, 0.28f));
            yield return StartCoroutine(CountUpIntUnscaled(lblTrk, 0, tricks, 0.28f));
            yield return StartCoroutine(CountUpFloatUnscaled(lblDist, 0f, dist, 0.32f, true));
        }

        if (multiplierRow != null)
            yield return StartCoroutine(ScalePopInUnscaled(multiplierRow, 0.2f));

        yield return StartCoroutine(CountUpMultiplierUnscaled(lblMult, 1f, mult, 0.55f));

        if (finalBlock != null)
            yield return StartCoroutine(ScalePopInUnscaled(finalBlock, 0.26f));

        yield return StartCoroutine(CountUpIntUnscaled(lblFinal, 0, finalScore, 0.65f));

        SaveBestIfImproved(finalScore);
        int bestAfter = GetBestFinalScore();
        if (lblBest != null)
            lblBest.text = bestAfter.ToString();

        yield return new WaitForSecondsRealtime(0.08f);

        foreach (var btn in buttons)
        {
            StartCoroutine(SlideInUnscaled(btn));
            yield return new WaitForSecondsRealtime(0.09f);
        }
    }

    IEnumerator TitleBounceInUnscaled(VisualElement el)
    {
        const float duration = 0.5f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float ease = BounceEaseOut(t);

            el.style.opacity = Mathf.Clamp01(t * 2.5f);
            el.transform.position = new Vector3(0f, Mathf.Lerp(-180f, 0f, ease), 0f);
            float s = Mathf.Lerp(0.75f, 1f, ease);
            el.transform.scale = new Vector3(s, s, 1f);
            yield return null;
        }

        el.style.opacity = 1f;
        el.transform.position = Vector3.zero;
        el.transform.scale = Vector3.one;
    }

    IEnumerator ScalePopInUnscaled(VisualElement el, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            el.style.opacity = t;
            float s = Mathf.Lerp(0.85f, 1f, t);
            el.transform.scale = new Vector3(s, s, 1f);
            yield return null;
        }

        el.style.opacity = 1f;
        el.transform.scale = Vector3.one;
    }

    IEnumerator CountUpIntUnscaled(Label label, int from, int to, float duration)
    {
        if (label == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);
            label.text = Mathf.RoundToInt(Mathf.Lerp(from, to, t)).ToString();
            yield return null;
        }

        label.text = to.ToString();
    }

    IEnumerator CountUpFloatUnscaled(Label label, float from, float to, float duration, bool asMeters)
    {
        if (label == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);
            float v = Mathf.Lerp(from, to, t);
            label.text = asMeters ? $"{Mathf.FloorToInt(v)} m" : v.ToString("F0");
            yield return null;
        }

        label.text = asMeters ? $"{Mathf.FloorToInt(to)} m" : to.ToString("F0");
    }

    IEnumerator CountUpMultiplierUnscaled(Label label, float from, float to, float duration)
    {
        if (label == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);
            float v = Mathf.Lerp(from, to, t);
            label.text = "×" + v.ToString("F2");
            yield return null;
        }

        label.text = "×" + to.ToString("F2");
    }

    IEnumerator SlideInUnscaled(VisualElement el)
    {
        const float duration = 0.28f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            el.style.opacity = t;
            el.transform.position = new Vector3(Mathf.Lerp(-70f, 0f, t), 0f, 0f);
            yield return null;
        }

        el.style.opacity = 1f;
        el.transform.position = Vector3.zero;
    }

    IEnumerator SwooshIn(VisualElement swoosh, VisualElement btn)
    {
        const float duration = 0.14f;
        float elapsed = 0f;
        float target = btn.resolvedStyle.width * SwooshCoverage;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            swoosh.style.width = target * Mathf.SmoothStep(0f, 1f, elapsed / duration);
            yield return null;
        }

        swoosh.style.width = target;
    }

    static IEnumerator SwooshOut(VisualElement swoosh)
    {
        const float duration = 0.08f;
        float start = swoosh.resolvedStyle.width;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            swoosh.style.width = Mathf.Lerp(start, 0f, elapsed / duration);
            yield return null;
        }

        swoosh.style.width = 0f;
    }

    static float BounceEaseOut(float t)
    {
        if (t < 1f / 2.75f)
            return 7.5625f * t * t;
        if (t < 2f / 2.75f)
        {
            t -= 1.5f / 2.75f;
            return 7.5625f * t * t + 0.75f;
        }

        if (t < 2.5f / 2.75f)
        {
            t -= 2.25f / 2.75f;
            return 7.5625f * t * t + 0.9375f;
        }

        t -= 2.625f / 2.75f;
        return 7.5625f * t * t + 0.984375f;
    }

    static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

[System.Serializable]
public struct RunScoreTuning
{
    public float KInsects;
    public float KTricks;
    public float KDistance;
    public float MinMultiplier;
    public float MaxMultiplier;
}

public static class RunScoreCalculator
{
    public static float GetBonusMultiplier(int insects, int tricks, float distanceX, RunScoreTuning tuning)
    {
        float m = 1f + tuning.KInsects * insects + tuning.KTricks * tricks + tuning.KDistance * distanceX;
        return Mathf.Clamp(m, tuning.MinMultiplier, tuning.MaxMultiplier);
    }

    public static int ComputeFinalScore(int trickPoints, int insects, int tricks, float distanceX, RunScoreTuning tuning)
    {
        if (trickPoints <= 0)
            return 0;

        float mult = GetBonusMultiplier(insects, tricks, distanceX, tuning);
        return Mathf.Max(0, Mathf.RoundToInt(trickPoints * mult));
    }
}
