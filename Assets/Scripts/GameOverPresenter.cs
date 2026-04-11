using UnityEngine;

public class GameOverPresenter : MonoBehaviour
{
    [SerializeField] private Transform frogTransform;
    [SerializeField] private float fallY = -25f;
    [SerializeField] private GameObject endScreenRoot;
    [SerializeField] private EndScreenController endScreen;

    bool _gameOver;

    void Awake()
    {
        if (frogTransform == null)
            frogTransform = transform;
    }

    void Update()
    {
        if (_gameOver || frogTransform == null)
            return;

        if (frogTransform.position.y >= fallY)
            return;

        _gameOver = true;
        Time.timeScale = 0f;

        if (endScreenRoot != null)
            endScreenRoot.SetActive(true);

        var doc = endScreen != null ? endScreen.GetComponent<UnityEngine.UIElements.UIDocument>() : null;
        if (doc != null)
            doc.enabled = true;

        if (endScreen != null)
            endScreen.PresentRunEnd();
    }
}
