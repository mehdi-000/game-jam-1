using UnityEngine;

public class FrogEatOnCollision : MonoBehaviour
{
    [SerializeField] private FrogController frog;
    [SerializeField] private TrickHudController trickHud;

    void Awake()
    {
        if (frog == null)
            frog = GetComponent<FrogController>();
        if (trickHud == null)
            trickHud = FindFirstObjectByType<TrickHudController>();
    }

    void OnCollisionEnter(Collision collision)
    {
        GameObject root = collision.collider.transform.root.gameObject;
        if (!root.CompareTag("Target"))
            return;

        Vector3 popPoint = collision.contactCount > 0
            ? collision.GetContact(0).point
            : root.transform.position;

        if (frog != null)
            frog.DetachTongueIfStuckTo(root.transform);

        GameScore.AddInsect();
        if (trickHud != null)
            trickHud.OnInsectEaten(popPoint);

        Destroy(root);
    }
}
