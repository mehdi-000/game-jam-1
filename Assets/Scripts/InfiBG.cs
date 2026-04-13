using UnityEngine;

public class InfiBG : MonoBehaviour
{
    private Transform cam;
    private SpriteRenderer sr;
    private float tileWidth;
    void Start()
    {
        cam = Camera.main.transform;
        sr = GetComponent<SpriteRenderer>();
        tileWidth = sr.bounds.size.x;
    }
    void Update()
    {
        float distance = cam.position.x - transform.position.x;
        if (distance > tileWidth)
        {
            transform.position += new Vector3(tileWidth * 2f, 0f, 0f);
        }
        else if (distance < tileWidth)
        {
            transform.position -= new Vector3(tileWidth * 2f, 0f, 0f);
        }
    }
}
