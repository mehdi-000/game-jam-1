using System;
using UnityEngine;

public class TrickComboController : MonoBehaviour
{
    public event Action<string, int, int, int> OnTrickScored;
    public event Action OnComboReset;

    [SerializeField] private RotationTracker rotationTracker;

    [Header("Trick thresholds")]
    [SerializeField] private float trickDegrees = 330f;
    [SerializeField] private float minSecondsBetweenTricks = 0.35f;

    [Header("Combo")]
    [SerializeField] private float comboResetSeconds = 3.2f;
    [SerializeField] private int basePointsPerTrick = 100;
    [Tooltip("Multiplier is 2^min(comboChain, maxComboExponent). Caps at 2^maxComboExponent.")]
    [SerializeField] private int maxComboExponent = 3;

    int _comboChain;
    float _comboTimer;
    float _lastTrickTime = -999f;

    public int ComboChain => _comboChain;
    public int CurrentMultiplierValue => ComputeMultiplier();

    void Awake()
    {
        if (rotationTracker == null)
            rotationTracker = GetComponent<RotationTracker>();
        GameScore.Reset();
    }

    void OnEnable()
    {
        if (rotationTracker != null)
            rotationTracker.OnPitchYawRollUpdated += HandleRotation;
    }

    void OnDisable()
    {
        if (rotationTracker != null)
            rotationTracker.OnPitchYawRollUpdated -= HandleRotation;
    }

    void Update()
    {
        if (_comboTimer > 0f)
        {
            _comboTimer -= Time.deltaTime;
            if (_comboTimer <= 0f)
            {
                _comboChain = 0;
                OnComboReset?.Invoke();
            }
        }
    }

    void HandleRotation(PitchYawRollEventData data)
    {
        Vector3 a = data.AccumulatedPitchYawRoll;
        float ax = Mathf.Abs(a.x);
        float ay = Mathf.Abs(a.y);
        float az = Mathf.Abs(a.z);
        float maxAbs = ax;
        int axis = 0;
        if (ay > maxAbs) { maxAbs = ay; axis = 1; }
        if (az > maxAbs) { maxAbs = az; axis = 2; }

        if (maxAbs < trickDegrees)
            return;

        if (Time.unscaledTime - _lastTrickTime < minSecondsBetweenTricks)
            return;

        if (_comboTimer > 0f)
            _comboChain++;
        else
            _comboChain = 1;

        int mult = ComputeMultiplier();
        int points = basePointsPerTrick * mult;
        GameScore.AddPoints(points);
        GameScore.RegisterTrickLanded();

        string trickName = ResolveTrickName(axis, a);
        rotationTracker.ConsumeAccumulatedAlongAxis(axis, trickDegrees);

        _lastTrickTime = Time.unscaledTime;
        _comboTimer = comboResetSeconds;

        OnTrickScored?.Invoke(trickName, points, mult, GameScore.CurrentScore);
    }

    int ComputeMultiplier()
    {
        int exp = Mathf.Min(_comboChain, maxComboExponent);
        return 1 << exp;
    }

    static string ResolveTrickName(int axis, Vector3 accum)
    {
        switch (axis)
        {
            case 0:
                return accum.x >= 0f ? "FRONT FLIP" : "BACK FLIP";
            case 1:
                return "AIR SPIN";
            default:
                return "BARREL";
        }
    }
}
