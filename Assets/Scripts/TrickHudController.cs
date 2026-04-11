using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class TrickHudController : MonoBehaviour
{
    [SerializeField] private TrickComboController trickCombo;

    UIDocument _doc;
    Label _trickName;
    Label _trickShadow;
    Label _score;
    Label _insects;
    Label _tricks;
    Label _distance;
    Label _mult;
    VisualElement _comboBlock;
    VisualElement _trickRow;
    VisualElement _hudRoot;

    void Awake()
    {
        _doc = GetComponent<UIDocument>();
    }

    void Start()
    {
        if (trickCombo == null)
            trickCombo = FindFirstObjectByType<TrickComboController>();

        var root = _doc.rootVisualElement;
        _hudRoot = root.Q<VisualElement>("HudRoot");
        _trickRow = root.Q<VisualElement>("TrickRow");
        _trickName = root.Q<Label>("TrickName");
        _trickShadow = root.Q<Label>("TrickShadow");
        _score = root.Q<Label>("ScoreValue");
        _insects = root.Q<Label>("InsectEatenValue");
        _tricks = root.Q<Label>("TricksCountValue");
        _distance = root.Q<Label>("DistanceValue");
        _mult = root.Q<Label>("MultiplierValue");
        _comboBlock = root.Q<VisualElement>("ComboBlock");

        if (_trickName != null) _trickName.text = "";
        if (_trickShadow != null) _trickShadow.text = "";
        if (_score != null) _score.text = GameScore.CurrentScore.ToString();
        if (_insects != null) _insects.text = GameScore.CurrentInsectsEaten.ToString();
        if (_tricks != null) _tricks.text = GameScore.TricksLandedCount.ToString();
        RefreshDistanceLabel();
        UpdateMultiplierLabel(2);

        if (trickCombo != null)
        {
            trickCombo.OnTrickScored += HandleTrickScored;
            trickCombo.OnComboReset += HandleComboReset;
        }
    }

    void Update()
    {
        RefreshDistanceLabel();
    }

    void RefreshDistanceLabel()
    {
        if (_distance != null)
            _distance.text = $"{Mathf.FloorToInt(GameScore.DistanceX)} m";
    }

    void OnDestroy()
    {
        if (trickCombo != null)
        {
            trickCombo.OnTrickScored -= HandleTrickScored;
            trickCombo.OnComboReset -= HandleComboReset;
        }
    }

    void HandleTrickScored(string trickName, int pointsAdded, int multiplier, int totalScore)
    {
        if (_trickName != null)
        {
            _trickName.text = trickName;
            if (_trickShadow != null)
                _trickShadow.text = trickName;
            StartCoroutine(TrickPopIn(_trickRow));
        }

        if (_score != null)
        {
            _score.text = totalScore.ToString();
            StartCoroutine(ScorePunch(_score));
        }

        if (_tricks != null)
            _tricks.text = GameScore.TricksLandedCount.ToString();

        UpdateMultiplierLabel(multiplier);
        if (_comboBlock != null)
        {
            _comboBlock.RemoveFromClassList("combo-dim");
            StartCoroutine(MultPulse(_mult));
        }
    }

    void HandleComboReset()
    {
        if (_comboBlock != null)
            _comboBlock.AddToClassList("combo-dim");
        UpdateMultiplierLabel(2);
    }

    void UpdateMultiplierLabel(int multiplier)
    {
        if (_mult != null)
            _mult.text = multiplier + "x";
    }

    public void OnInsectEaten(Vector3 worldPosition)
    {
        if (_insects != null)
            _insects.text = GameScore.CurrentInsectsEaten.ToString();

        RefreshDistanceLabel();

        if (_hudRoot != null)
            StartCoroutine(InsectPlusOneFloat(worldPosition));
    }

    IEnumerator InsectPlusOneFloat(Vector3 worldPosition)
    {
        var cam = Camera.main;
        if (cam == null || _hudRoot == null)
            yield break;

        Vector3 screen = cam.WorldToScreenPoint(worldPosition);
        if (screen.z < 0f)
            yield break;

        IPanel panel = _doc.rootVisualElement.panel;
        if (panel == null)
            yield break;

        Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screen.x, screen.y));
        Vector2 local = _hudRoot.WorldToLocal(panelPos);

        var floater = new Label("+1");
        floater.AddToClassList("insect-plus-one");
        floater.style.position = Position.Absolute;
        floater.style.opacity = 1f;
        _hudRoot.Add(floater);

        yield return null;

        float w = floater.layout.width > 1f ? floater.layout.width : 56f;
        float h = floater.layout.height > 1f ? floater.layout.height : 32f;
        float left = local.x - w * 0.5f;
        float top = local.y - h * 0.5f;
        floater.style.left = left;
        floater.style.top = top;

        const float duration = 0.55f;
        float elapsed = 0f;
        const float rise = 48f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float ease = Mathf.SmoothStep(0f, 1f, t);
            floater.style.opacity = 1f - ease;
            floater.style.top = top - rise * ease;
            yield return null;
        }

        floater.RemoveFromHierarchy();
    }

    static IEnumerator TrickPopIn(VisualElement row)
    {
        if (row == null) yield break;

        const float duration = 0.26f;
        float elapsed = 0f;
        row.style.opacity = 0f;
        row.transform.position = new Vector3(48f, 0f, 0f);
        row.transform.scale = new Vector3(0.92f, 0.92f, 1f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            row.style.opacity = t;
            row.transform.position = new Vector3(Mathf.Lerp(48f, 0f, t), 0f, 0f);
            float s = Mathf.Lerp(0.92f, 1.04f, t);
            row.transform.scale = new Vector3(s, s, 1f);
            yield return null;
        }

        elapsed = 0f;
        const float settle = 0.12f;
        while (elapsed < settle)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / settle;
            float s = Mathf.Lerp(1.04f, 1f, Mathf.SmoothStep(0f, 1f, t));
            row.transform.scale = new Vector3(s, s, 1f);
            yield return null;
        }

        row.style.opacity = 1f;
        row.transform.position = Vector3.zero;
        row.transform.scale = Vector3.one;
    }

    static IEnumerator ScorePunch(Label scoreLabel)
    {
        if (scoreLabel == null) yield break;

        const float duration = 0.22f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            float s = Mathf.Lerp(1.18f, 1f, t);
            scoreLabel.transform.scale = new Vector3(s, s, 1f);
            yield return null;
        }

        scoreLabel.transform.scale = Vector3.one;
    }

    static IEnumerator MultPulse(Label multLabel)
    {
        if (multLabel == null) yield break;

        const float duration = 0.18f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            float s = Mathf.Lerp(1.28f, 1f, t);
            multLabel.transform.scale = new Vector3(s, s, 1f);
            yield return null;
        }

        multLabel.transform.scale = Vector3.one;
    }
}
