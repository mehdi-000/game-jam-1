using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverPresenter : MonoBehaviour
{
    public const string ReturnGameScenePlayerPrefsKey = "FrogstylerReturnGameScene";

    [Header("Finish detection")]
    [Tooltip("Ignore Finish hits whose contacts are mostly sideways (wide thin finish colliders). Requires at least one contact whose normal is mostly vertical (up or down).")]
    [SerializeField] float minContactUpNormal = 0.45f;

    [Tooltip("Only count Finish when the frog is at or below this world Y. Stops early triggers while on lily pads or in the air above the line. Tune to your scene (lily height vs finish ground).")]
    [SerializeField] float frogMaxWorldYForFinish = -4.25f;

    [Header("End screen")]
    [Tooltip("Seconds to wait after a valid finish before loading the end screen scene (uses unscaled time).")]
    [SerializeField] float endScreenDelaySeconds = 0.75f;

    [Tooltip("Must be added to File → Build Settings. Uses dedicated scene so UI Toolkit buttons receive input reliably.")]
    [SerializeField] string endScreenSceneName = "EndScreen";

    bool _gameOver;

    void OnCollisionEnter(Collision collision)
    {
        if (_gameOver)
            return;

        var col = collision.collider;
        if (col == null)
            return;

        if (!col.CompareTag("Finish") && !col.transform.root.CompareTag("Finish"))
            return;

        if (transform.position.y > frogMaxWorldYForFinish)
            return;

        if (!HasTopSurfaceContact(collision, minContactUpNormal))
            return;

        _gameOver = true;
        StartCoroutine(ShowEndScreenAfterDelay());
    }

    IEnumerator ShowEndScreenAfterDelay()
    {
        if (endScreenDelaySeconds > 0f)
            yield return new WaitForSecondsRealtime(endScreenDelaySeconds);

        Time.timeScale = 1f;

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
