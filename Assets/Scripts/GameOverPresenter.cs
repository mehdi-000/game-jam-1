using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverPresenter : MonoBehaviour
{
    public const string ReturnGameScenePlayerPrefsKey = "FrogstylerReturnGameScene";

    [Header("Finish detection")]
    [Tooltip("On collision enter, require at least one contact whose normal is mostly vertical. Reduces false starts when grazing the side of a thin Finish volume; once you are overlapping, OnCollisionStay uses tag + Y only so deep water still counts.")]
    [SerializeField] float minContactUpNormal = 0.45f;

    [Tooltip("Only count Finish when the frog is at or below this world Y. Stops early triggers while on lily pads or in the air above the line. Tune to your scene (lily height vs finish ground).")]
    [SerializeField] float frogMaxWorldYForFinish = -4.25f;

    [Tooltip("How long the frog must stay in valid contact with Finish (water) before the run ends. Skims and quick bounces clear before this and cancel the countdown.")]
    [SerializeField] float minTimeInFinishBeforeGameOver = 0.45f;

    [Header("End screen")]
    [Tooltip("Seconds to wait after a valid finish before loading the end screen scene (uses unscaled time).")]
    [SerializeField] float endScreenDelaySeconds = 0.75f;

    [Tooltip("Must be added to File → Build Settings. Uses dedicated scene so UI Toolkit buttons receive input reliably.")]
    [SerializeField] string endScreenSceneName = "EndScreen";

    bool _gameOver;
    readonly HashSet<Collider> _activeFinishColliders = new HashSet<Collider>();
    Coroutine _pendingGameOver;

    void OnCollisionEnter(Collision collision)
    {
        if (_gameOver)
            return;

        if (!TryRegisterFinishContact(collision, requireTopSurfaceNormal: true))
            return;

        EnsurePendingGameOverStarted();
    }

    void OnCollisionStay(Collision collision)
    {
        if (_gameOver)
            return;

        // Submerged / resting in water often has no "top surface" contacts (side & bottom normals only).
        if (!TryRegisterFinishContact(collision, requireTopSurfaceNormal: false))
            return;

        EnsurePendingGameOverStarted();
    }

    void OnCollisionExit(Collision collision)
    {
        var col = collision.collider;
        if (col == null)
            return;

        if (!IsFinishCollider(col))
            return;

        _activeFinishColliders.Remove(col);

        if (_activeFinishColliders.Count == 0 && _pendingGameOver != null)
        {
            StopCoroutine(_pendingGameOver);
            _pendingGameOver = null;
        }
    }

    void EnsurePendingGameOverStarted()
    {
        if (_activeFinishColliders.Count >= 1 && _pendingGameOver == null)
            _pendingGameOver = StartCoroutine(WaitForSustainedFinishContact());
    }

    bool TryRegisterFinishContact(Collision collision, bool requireTopSurfaceNormal)
    {
        var col = collision.collider;
        if (col == null)
            return false;

        if (!IsFinishCollider(col))
            return false;

        if (transform.position.y > frogMaxWorldYForFinish)
            return false;

        if (requireTopSurfaceNormal && !HasTopSurfaceContact(collision, minContactUpNormal))
            return false;

        int before = _activeFinishColliders.Count;
        _activeFinishColliders.Add(col);
        return _activeFinishColliders.Count > before;
    }

    IEnumerator WaitForSustainedFinishContact()
    {
        float wait = Mathf.Max(0f, minTimeInFinishBeforeGameOver);
        if (wait > 0f)
            yield return new WaitForSecondsRealtime(wait);

        _pendingGameOver = null;

        if (_gameOver || _activeFinishColliders.Count == 0)
            yield break;

        if (transform.position.y > frogMaxWorldYForFinish)
            yield break;

        _gameOver = true;
        StartCoroutine(ShowEndScreenAfterDelay());
    }

    static bool IsFinishCollider(Collider col)
    {
        return col.CompareTag("Finish") || col.transform.root.CompareTag("Finish");
    }

    IEnumerator ShowEndScreenAfterDelay()
    {
        if (endScreenDelaySeconds > 0f)
            yield return new WaitForSecondsRealtime(endScreenDelaySeconds);

        PlayerPrefs.SetString(ReturnGameScenePlayerPrefsKey, SceneManager.GetActiveScene().name);
        PlayerPrefs.Save();
        SceneManager.LoadScene(endScreenSceneName);
    }

    static bool HasTopSurfaceContact(Collision collision, float minUpDot)
    {
        int n = collision.contactCount;
        if (n <= 0)
            return false;

        for (int i = 0; i < n; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            if (Mathf.Abs(Vector3.Dot(normal, Vector3.up)) >= minUpDot)
                return true;
        }

        return false;
    }
}
