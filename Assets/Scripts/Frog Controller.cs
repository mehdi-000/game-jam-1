using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FrogController : MonoBehaviour
{
    [SerializeField] private SpringJoint leftLeg, rightLeg, tongue;
    [SerializeField] private TongueSticker tongueSticker;
    [SerializeField] private Rigidbody leftLegRb, rightLegRb, tongueRb;
    [SerializeField] private Collider tongueCollider;
    [SerializeField] private bool recomputeLegLengthOnStart = true;


    [Header("Tongue Parameters")]
    [SerializeField] private Transform mouseTransform3D;
    [SerializeField] private List<Transform> Targets = new List<Transform>();
    [SerializeField] private Vector2 mousePosition;
    [SerializeField] private float tongueShootForce = 10f;

    [Header("Leg Parameters")]
    [SerializeField] private float 
        tugMax, tugMin, // 0-1, 1 is leg toughes Frog, 0 is max extension
        tugSpeed, // how fast the leg tugs in when tugging
        springMax, // the spring force of fully extended leg
        legLength, // the maximum length of the leg when fully extended
        legKickForceMultiplier, // the base force applied to the feet when kicking, multiplied by the current tug level
        currentTugLeft, currentTugRight;


    [SerializeField] private bool isJumping, leftIsTugging, rightIsTugging;

    [SerializeField] private InputActionAsset inputActions;

    [SerializeField] private float jumpforce = 10f;

    private void OnEnable()
    {
        inputActions.Enable();

        // Subscribe to the input actions

        // Tongue actions
        var tongueShootAction = inputActions.FindAction("TongueShoot");
        if (tongueShootAction != null) { tongueShootAction.performed += ctx => ShootTongue(transform.up); }
        var aimAction = inputActions.FindAction("Look");
        if (aimAction != null) { aimAction.performed += ctx => mousePosition = ctx.ReadValue<Vector2>(); }

        // Tugging actions
        var leftLegTugAction = inputActions.FindAction("LeftLegTug");
        if (leftLegTugAction != null) { leftLegTugAction.performed += ctx => leftIsTugging = true; }
        var rightLegTugAction = inputActions.FindAction("RightLegTug");
        if (rightLegTugAction != null) { rightLegTugAction.performed += ctx => rightIsTugging = true; }

        // Kicking actions
        var leftLegKickAction = inputActions.FindAction("LeftLegKick");
        if (leftLegKickAction != null) { leftLegKickAction.performed += ctx => JumpAndResetTug(jumpforce * currentTugLeft, true); }
        var rightLegKickAction = inputActions.FindAction("RightLegKick");
        if (rightLegKickAction != null) { rightLegKickAction.performed += ctx => JumpAndResetTug(jumpforce * currentTugRight, false); }

    }


    private void OnDisable()
    {
        inputActions.Disable();


        // Unsubscribe from the input actions to prevent memory leaks

        var tongueShootAction = inputActions.FindAction("TongueShoot");
        if (tongueShootAction != null) { tongueShootAction.performed -= ctx => ShootTongue(transform.up); }

        var leftLegTugAction = inputActions.FindAction("LeftLegTug");
        if (leftLegTugAction != null) { leftLegTugAction.performed -= ctx => leftIsTugging = true; }
        var rightLegTugAction = inputActions.FindAction("RightLegTug");
        if (rightLegTugAction != null) { rightLegTugAction.performed -= ctx => rightIsTugging = true; }

        var leftLegKickAction = inputActions.FindAction("LeftLegKick");
        if (leftLegKickAction != null) { leftLegKickAction.performed -= ctx => JumpAndResetTug(jumpforce * currentTugLeft, true); }
        var rightLegKickAction = inputActions.FindAction("RightLegKick");
        if (rightLegKickAction != null) { rightLegKickAction.performed -= ctx => JumpAndResetTug(jumpforce * currentTugRight, false); }
    }


    private void Start()
    {
        if (recomputeLegLengthOnStart)
        {
            legLength = Vector3.Distance(leftLeg.transform.position, leftLeg.connectedBody.transform.position);
        }

    }

    private void Update()
    {
        if (leftIsTugging) 
        { 
            currentTugLeft = Mathf.Clamp(currentTugLeft + Time.deltaTime * tugSpeed, tugMin, tugMax); 
            Tug(leftLeg, currentTugLeft);
        }

        if (rightIsTugging) 
        { 
            currentTugRight = Mathf.Clamp(currentTugRight + Time.deltaTime * tugSpeed, tugMin, tugMax); 
            Tug(rightLeg, currentTugRight);
        }

        Ray ray = Camera.main.ScreenPointToRay(mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            mouseTransform3D.position = new Vector3(hit.point.x, hit.point.y, transform.position.z);
        }
    }

    private void JumpAndResetTug(float force, bool usingleftLeg)
    {
        Jump(usingleftLeg ? leftLeg : rightLeg, force);
        ResetTug(usingleftLeg);
    }

    private void Jump(SpringJoint leg, float force)
    {
        var connectedBody = leg.connectedBody;
        leg.spring = springMax;

        if (connectedBody != null)
        {
            Vector3 jumpDirection = connectedBody.transform.position - leg.transform.position;
            leg.gameObject.GetComponent<Rigidbody>().AddForce(-jumpDirection.normalized * force * legKickForceMultiplier, ForceMode.Impulse);
            connectedBody.AddForce(jumpDirection.normalized * force, ForceMode.Impulse);
        }
    }

    private void ResetTug(bool isLeft)
    {
        if (isLeft)
        {
            leftIsTugging = false;
            currentTugLeft = 0;
            Tug(leftLeg, currentTugLeft);
        }
        else
        {
            rightIsTugging = false;
            currentTugRight = 0;
            Tug(rightLeg, currentTugRight);
        }
    }

    private void Tug(SpringJoint leg, float tug)
    {
        leg.anchor = new Vector3(0, (1 - tug) * legLength, 0);
        //leg.spring = springMax * (1 - tug);
    }

    private void ShootTongue(Vector3 targetPos = default)
    {        
        if (tongueSticker.isStuck)
        {
            tongueSticker.Release();
        }
        else
        {
            Physics.SphereCast(mouseTransform3D.position, 4f, -mouseTransform3D.forward, out RaycastHit hit, 100f);

            if (hit.collider != null)
            {
                Debug.Log("Hit: " + hit.collider.gameObject.name);
                if (hit.collider.gameObject.CompareTag("Target"))
                {
                    tongueSticker.StickTo(hit.transform);
                }
            }
            else
            {
                // Fallback: Suche nach Objekten in der Nähe der Mausposition mit OverlapSphere
                Collider[] nearbyColliders = Physics.OverlapSphere(mouseTransform3D.position, 4f);
                
                if (nearbyColliders.Length > 0)
                {
                    Collider closestCollider = nearbyColliders[0];
                    float closestDistance = Vector3.Distance(mouseTransform3D.position, closestCollider.transform.position);
                    
                    foreach (Collider collider in nearbyColliders)
                    {
                        if (collider.gameObject.CompareTag("Target"))
                        {
                            float distance = Vector3.Distance(mouseTransform3D.position, collider.transform.position);
                            if (distance < closestDistance)
                            {
                                closestCollider = collider;
                                closestDistance = distance;
                            }
                        }
                    }
                    
                    if (closestCollider.gameObject.CompareTag("Target"))
                    {
                        tongueSticker.StickTo(closestCollider.transform);
                    }
                }
                else
                {
                    Vector3 direction = targetPos == default ? transform.up : targetPos - tongue.transform.position;
                    tongueRb.AddForce(direction.normalized * tongueShootForce, ForceMode.Impulse);
                }
            }
        }
    }

}
