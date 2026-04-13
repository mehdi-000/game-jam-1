using UnityEngine;

public class InfiBGSky : MonoBehaviour
{
    private Transform cam;
    private SpriteRenderer sr;
    private float tileWidth;
    private float tileHeight;
    void Start()
    {
        cam = Camera.main.transform;
        sr = GetComponent<SpriteRenderer>();
        tileWidth = sr.bounds.size.x;
        tileHeight = sr.bounds.size.y;
    }
    void Update()
    {
        float xdistance = cam.position.x - transform.position.x;
        float ydistance = cam.position.y - transform.position.y;
        if (xdistance > tileWidth)
        {
            transform.position += new Vector3(tileWidth * 2f, 0f, 0f);
        }
        else if (xdistance < tileWidth)
        {
            transform.position -= new Vector3(tileWidth * 2f, 0f, 0f);
        }


        if (ydistance > tileHeight)
        {
            transform.position += new Vector3(0f, tileHeight * 2f, 0f);
        }
        else if (ydistance < tileHeight)
        {
            transform.position -= new Vector3(0f, tileHeight * 2f, 0f);
        }
    }
}