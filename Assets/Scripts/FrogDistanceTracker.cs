using UnityEngine;
public class FrogDistanceTracker : MonoBehaviour
{
    [SerializeField] private Transform trackTransform;

    float _lastX;

    void Awake()
    {
        if (trackTransform == null)
            trackTransform = transform;
        _lastX = trackTransform.position.x;
    }

    void FixedUpdate()
    {
        if (trackTransform == null)
            return;

        float x = trackTransform.position.x;
        float dx = x - _lastX;
        _lastX = x;
        if (dx > 0f)
            GameScore.AddDistanceX(dx);
    }
}
