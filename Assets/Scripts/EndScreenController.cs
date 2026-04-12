using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
    [SerializeField] float scoreKDistance = 0.015f;
    [SerializeField] float scoreMinMultiplier = 1f;
    [SerializeField] float scoreMaxMultiplier = 8f;

    [Header("Scene")]
    [Tooltip("If true (dedicated EndScreen scene), runs score presentation on Start. Leave false when this object lives in the level and you call PresentRunEnd from code.")]
    [SerializeField] bool presentScoresOnStart = true;

    [Header("Play again")]
    [Tooltip("When no return scene was saved (e.g. embedded end UI), Play Again loads Boosted if true, otherwise SampleScene. After a normal game over, GameOverPresenter saves the scene you played and that takes precedence.")]
    [SerializeField] bool playAgainBoostedMode;

    [Tooltip("Main menu scene (File → Build Settings).")]
    [SerializeField] string mainMenuSceneName = "Menu";

    UIDocument _doc;
    bool _migrationDone;
    bool _actionButtonsWired;

    Button _playAgainButton;
    Button _menuButton;

    void Awake()
    {
        _doc = GetComponent<UIDocument>();
        EnsureUiEventSystem.EnsureExists();
    }

    IEnumerator Start()
    {
        while (_doc != null && _doc.rootVisualElement == null)
            yield return null;

        yield return null;

        if (_doc != null && _doc.rootVisualElement != null)
            WireActionButtons(_doc.rootVisualElement);

        if (presentScoresOnStart)
        {
            MigrateLegacyBestOnce();
            yield return PresentRunEndRoutine();
        }
    }

    /// <summary>
    /// Prefer <see cref="Button.clicked"/> (Toolkit’s channel for activation) after the UIDocument
    /// tree exists; <see cref="MainMenuController"/> uses the same scene-wide EventSystem + clicks.
    /// </summary>
    void WireActionButtons(VisualElement root)
    {
        if (_actionButtonsWired || root == null)
            return;

        _playAgainButton = root.Q<Button>("PlayAgainButton");
        _menuButton = root.Q<Button>("MenuButton");
        if (_playAgainButton != null)
            _playAgainButton.clicked += PlayAgain;
        if (_menuButton != null)
            _menuButton.clicked += GoToMainMenu;

        foreach (Button btn in root.Query<Button>().ToList())
        {
            var swoosh = btn.Q<VisualElement>(className: "btn-swoosh");
            if (swoosh != null)
            {
                btn.RegisterCallback<MouseEnterEvent>(_ => StartCoroutine(SwooshIn(swoosh, btn)));
                btn.RegisterCallback<MouseLeaveEvent>(_ => StartCoroutine(SwooshOut(swoosh)));
            }
        }

        _actionButtonsWired = true;
    }

    void OnDestroy()
    {
        if (_playAgainButton != null)
            _playAgainButton.clicked -= PlayAgain;
        if (_menuButton != null)
            _menuButton.clicked -= GoToMainMenu;
    }

    public void PresentRunEnd()
    {
        MigrateLegacyBestOnce();
        if (_doc != null && _doc.rootVisualElement != null)
            WireActionButtons(_doc.rootVisualElement);
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

    public void PlayAgain()
    {
        string fallback = playAgainBoostedMode ? "Boosted" : "SampleScene";
        string scene = PlayerPrefs.GetString(GameOverPresenter.ReturnGameScenePlayerPrefsKey, fallback);
        if (string.IsNullOrEmpty(scene))
            scene = fallback;
        SceneManager.LoadScene(scene);
    }

    public void GoToMainMenu()
    {
        if (string.IsNullOrEmpty(mainMenuSceneName))
            return;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    IEnumerator PresentRunEndRoutine()
    {
        yield return null;

        var root = _doc.rootVisualElement;
        if (root == null)
            yield break;

        int trickPts = GameScore.CurrentScore;
        int insects = GameScore.CurrentInsectsEaten;
        int tricks = GameScore.TricksLandedCount;
        float dist = GameScore.DistanceX;

        var tuning = new RunScoreTuning
        {
            KInsects = scoreKInsects,
            KDistance = scoreKDistance,
            MinMultiplier = scoreMinMultiplier,
            MaxMultiplier = scoreMaxMultiplier,
        };

        RunScoreBreakdown breakdown = RunScoreCalculator.GetBreakdown(insects, dist, tuning);
        float mult = breakdown.ClampedMultiplier;
        int finalScore = RunScoreCalculator.ComputeFinalScore(trickPts, insects, dist, tuning);
        int bestBefore = GetBestFinalScore();

        var lblFlip = root.Q<Label>("BreakdownFlipPoints");
        var lblIns = root.Q<Label>("BreakdownInsects");
        var lblTrk = root.Q<Label>("BreakdownTricks");
        var lblDist = root.Q<Label>("BreakdownDistance");
        var lblMultFromIns = root.Q<Label>("MultFromInsects");
        var lblMultFromDist = root.Q<Label>("MultFromDistance");
        var lblMultRaw = root.Q<Label>("MultUncappedValue");
        var multClampRow = root.Q<VisualElement>("MultClampRow");
        var lblMult = root.Q<Label>("BonusMultiplierValue");
        var lblFinal = root.Q<Label>("FinalRunValue");
        var lblEquation = root.Q<Label>("FinalEquationLabel");
        var lblBest = root.Q<Label>("FinalBestValue");

        if (lblFlip != null) lblFlip.text = "0";
        if (lblIns != null) lblIns.text = "0";
        if (lblTrk != null) lblTrk.text = "0";
        if (lblDist != null) lblDist.text = "0";
        if (lblMultFromIns != null) lblMultFromIns.text = "+0.00";
        if (lblMultFromDist != null) lblMultFromDist.text = "+0.00";
        if (lblMultRaw != null) lblMultRaw.text = "";
        if (multClampRow != null) multClampRow.AddToClassList("hide");
        if (lblMult != null) lblMult.text = "×1.00";
        if (lblFinal != null)
        {
            lblFinal.text = "0";
            lblFinal.RemoveFromClassList("number-current--compact");
        }

        if (lblBest != null)
            lblBest.RemoveFromClassList("number-best--compact");

        if (lblEquation != null) lblEquation.text = "";
        if (lblBest != null)
        {
            lblBest.text = bestBefore.ToString();
            ApplyScoreDigitCompactClass(lblBest, bestBefore, "number-best--compact");
        }

        var titleBlock = root.Q<VisualElement>("TitleBlock");
        var breakdownBlock = root.Q<VisualElement>("BreakdownBlock");
        var multiplierDetailBlock = root.Q<VisualElement>("MultiplierDetailBlock");
        var multiplierRow = root.Q<VisualElement>("MultiplierRow");
        var finalBlock = root.Q<VisualElement>("FinalBlock");
        var buttons = root.Query<Button>().ToList();

        if (titleBlock != null)
        {
            titleBlock.style.opacity = 0f;
            titleBlock.transform.position = new Vector3(0f, -180f, 0f);
            titleBlock.transform.scale = new Vector3(0.75f, 0.75f, 1f);
        }

        foreach (var el in new[] { breakdownBlock, multiplierDetailBlock, multiplierRow, finalBlock })
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

        if (multiplierDetailBlock != null)
        {
            yield return StartCoroutine(ScalePopInUnscaled(multiplierDetailBlock, 0.18f));
            yield return StartCoroutine(CountUpFloatContributionUnscaled(lblMultFromIns, 0f, breakdown.ContribInsects, 0.32f));
            yield return StartCoroutine(CountUpFloatContributionUnscaled(lblMultFromDist, 0f, breakdown.ContribDistance, 0.3f));
        }

        if (breakdown.WasClamped && multClampRow != null && lblMultRaw != null)
        {
            multClampRow.RemoveFromClassList("hide");
            if (breakdown.RawMultiplier < scoreMinMultiplier)
                lblMultRaw.text = $"Raw ×{breakdown.RawMultiplier:F2} (raised to min ×{scoreMinMultiplier:F2})";
            else
                lblMultRaw.text = $"Raw ×{breakdown.RawMultiplier:F2} (capped at ×{scoreMaxMultiplier:F0})";
        }

        if (multiplierRow != null)
            yield return StartCoroutine(ScalePopInUnscaled(multiplierRow, 0.2f));

        yield return StartCoroutine(CountUpMultiplierUnscaled(lblMult, 1f, mult, 0.55f));

        if (lblEquation != null)
            lblEquation.text = $"{FormatScore(trickPts)} × {mult:F2} =";

        if (finalBlock != null)
            yield return StartCoroutine(ScalePopInUnscaled(finalBlock, 0.26f));

        yield return StartCoroutine(CountUpIntUnscaled(lblFinal, 0, finalScore, 0.65f));
        ApplyScoreDigitCompactClass(lblFinal, finalScore, "number-current--compact");

        SaveBestIfImproved(finalScore);
        int bestAfter = GetBestFinalScore();
        if (lblBest != null)
        {
            lblBest.text = bestAfter.ToString();
            ApplyScoreDigitCompactClass(lblBest, bestAfter, "number-best--compact");
        }

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

    IEnumerator CountUpFloatContributionUnscaled(Label label, float from, float to, float duration)
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
            label.text = "+" + v.ToString("F2");
            yield return null;
        }

        label.text = "+" + to.ToString("F2");
    }

    static string FormatScore(int value)
    {
        if (value < 0)
            return "0";
        return value.ToString("N0", CultureInfo.InvariantCulture);
    }

    static void ApplyScoreDigitCompactClass(Label label, int value, string compactClass)
    {
        if (label == null || string.IsNullOrEmpty(compactClass))
            return;

        int digits = value == 0 ? 1 : 0;
        int v = Mathf.Abs(value);
        while (v > 0)
        {
            digits++;
            v /= 10;
        }

        if (digits > 7)
            label.AddToClassList(compactClass);
        else
            label.RemoveFromClassList(compactClass);
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
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            swoosh.style.width = target * t;
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
            float t = elapsed / duration;
            swoosh.style.width = Mathf.Lerp(start, 0f, t);
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
}

[System.Serializable]
public struct RunScoreTuning
{
    public float KInsects;
    public float KDistance;
    public float MinMultiplier;
    public float MaxMultiplier;
}

public struct RunScoreBreakdown
{
    public float ContribInsects;
    public float ContribDistance;
    public float RawMultiplier;
    public float ClampedMultiplier;
    public bool WasClamped;
}

public static class RunScoreCalculator
{
    public static RunScoreBreakdown GetBreakdown(int insects, float distanceX, RunScoreTuning tuning)
    {
        float cI = tuning.KInsects * insects;
        float cD = tuning.KDistance * distanceX;
        float raw = 1f + cI + cD;
        float clamped = Mathf.Clamp(raw, tuning.MinMultiplier, tuning.MaxMultiplier);
        return new RunScoreBreakdown
        {
            ContribInsects = cI,
            ContribDistance = cD,
            RawMultiplier = raw,
            ClampedMultiplier = clamped,
            WasClamped = Mathf.Abs(raw - clamped) > 0.0001f,
        };
    }

    public static float GetBonusMultiplier(int insects, float distanceX, RunScoreTuning tuning)
    {
        return GetBreakdown(insects, distanceX, tuning).ClampedMultiplier;
    }

    public static int ComputeFinalScore(int trickPoints, int insects, float distanceX, RunScoreTuning tuning)
    {
        if (trickPoints <= 0)
            return 0;

        float mult = GetBreakdown(insects, distanceX, tuning).ClampedMultiplier;
        return Mathf.Max(0, Mathf.RoundToInt(trickPoints * mult));
    }
}
