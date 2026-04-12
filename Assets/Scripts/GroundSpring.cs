using UnityEngine;
using UnityEngine.U2D;

public class GroundSpring : MonoBehaviour
{
    public float velocity;
    public float force;
    public float height;

    private float targetHeight;
    private int waveIndex;
    private SpriteShapeController spriteShapeController;

    public void Init(SpriteShapeController ssc)
    {
        waveIndex = transform.GetSiblingIndex() + 1;
        spriteShapeController = ssc;
        velocity = 0;
        height = transform.localPosition.y;
        targetHeight = transform.localPosition.y;
    }

    public void WaveSpringUpdate(float springStiffness, float dampening)
    {
        height = transform.localPosition.y;
        float x = height - targetHeight;
        float loss = -dampening * velocity;
        force = -springStiffness * x + loss;
        velocity += force;
        float y = transform.localPosition.y + velocity;
        transform.localPosition = new Vector3(transform.localPosition.x, y, transform.localPosition.z);
    }

    public void WavePointUpdate()
    {
        if (spriteShapeController == null) return;

        Spline spline = spriteShapeController.spline;
        Vector3 currentPos = spline.GetPosition(waveIndex);
        spline.SetPosition(waveIndex, new Vector3(currentPos.x, transform.localPosition.y, currentPos.z));
    }
}
