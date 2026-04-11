using UnityEngine;

public class TongueAutoStick : MonoBehaviour
{
    [SerializeField] private FrogController frogController;
    [SerializeField] private bool willAutoStick = true;

    private void OnCollisionEnter(Collision collision)
    {
        if (frogController == null)
        {
            Debug.LogError("FrogController reference is missing in TongueAutoStick.");
            return;
        }
        
        if (!willAutoStick) return;

        if (collision.gameObject.layer == LayerMask.NameToLayer("Stickable"))
        {
            frogController.StickTo(collision.transform, collision);
        }
    }
}