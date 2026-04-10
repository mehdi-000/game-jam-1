using UnityEngine;

public class LilyPadBehaviour : MonoBehaviour
{
    [SerializeField] private GameObject[] graphicObjects;
    [SerializeField] private float spawnHeight = 0.1f, floatTime = 2f, destroyAfterTime = 15f, sinkingSpeed = 0.3f;
    private bool isFloating, isTriggered;

    private void OnEnable()
    {    
        transform.position = new Vector3(transform.position.x, spawnHeight, transform.position.z);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player") && !isTriggered)
        {
            isTriggered = true;
            StartCoroutine(FloatAndSink());
        }
    }

    private System.Collections.IEnumerator FloatAndSink()
    {
        isFloating = true;
        yield return new WaitForSeconds(floatTime);
        isFloating = false;
        float elapsedTime = 0f;
        Vector3 initialPosition = transform.position;
        while (elapsedTime < destroyAfterTime)
        {
            transform.position = Vector3.Lerp(initialPosition, initialPosition - new Vector3(0, 1, 0), elapsedTime / destroyAfterTime);
            elapsedTime += Time.deltaTime * sinkingSpeed;
            yield return null;
        }

        foreach (var graphicObject in graphicObjects)
        {
            Destroy(graphicObject);
        }

        transform.DetachChildren();
        Destroy(gameObject);
    }
}
