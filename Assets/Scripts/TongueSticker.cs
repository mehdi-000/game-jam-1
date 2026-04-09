using UnityEngine;

public class TongueSticker : MonoBehaviour
{
    public bool isStuck = false;
    [SerializeField] private float stickCooldown = 0.25f;
    private float timeSinceRelease = 0f;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Stickable"))
        {
            if(timeSinceRelease < stickCooldown)
            {
                return; // Prevent sticking if still in cooldown
            }

            StickTo(collision.transform);

            ContactPoint contact = collision.contacts[0];
            Vector3 contactPoint = contact.point;
            Vector3 contactNormal = contact.normal;

            transform.position = contactPoint;
            transform.rotation = Quaternion.LookRotation(contactNormal);

            GetComponent<Rigidbody>().isKinematic = true;
        }
    }

    private void Update()
    {
        if (!isStuck && timeSinceRelease < stickCooldown)
        {
            timeSinceRelease += Time.deltaTime;
        }
    }

    public void StickTo(Transform target)
    {
        transform.SetParent(target);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        GetComponent<Rigidbody>().isKinematic = true;
        isStuck = true;
    }

    public void Release()
    {
        transform.SetParent(null);
        GetComponent<Rigidbody>().isKinematic = false;

        isStuck = false;
        timeSinceRelease = 0f;
    }
}
