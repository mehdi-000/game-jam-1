using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FrogController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputActionAsset inputActions;

    [SerializeField] private Vector3 spawnPosition;

    [Header("Legs")]
    [SerializeField] private SpringJoint leftLeg, rightLeg;
    [SerializeField] private Rigidbody leftLegRb, rightLegRb;

    [Header("Tongue")]
    [SerializeField] private SpringJoint tongue;
    [SerializeField] private Rigidbody tongueRb;
    [SerializeField] private Collider tongueCollider;

    [Header("Tongue Parameters")]
    [SerializeField] private Transform mouseTransform3D;
    [SerializeField] private List<Transform> Targets = new List<Transform>();
    [SerializeField] private Vector2 mousePosition;
    [SerializeField] private float tongueShootForce = 10f, maxTongueDistance = 10f;
    [SerializeField] private float shootCooldown = 0.5f,
        stickCooldown = 0.25f; // Time in seconds before the tongue can stick again after being released

    [Header("Leg Parameters")]
    [SerializeField] private bool recomputeLegLengthOnStart = false;
    [SerializeField] private float 
        tugMax, tugMin, // 0-1, 1 is leg toughes Frog, 0 is max extension
        tugSpeed, // how fast the leg tugs in when tugging
        springMax, // the spring force of fully extended leg
        legLength, // the maximum length of the leg when fully extended
        legKickForceMultiplier, // the base force applied to the feet when kicking, multiplied by the current tug level
        currentTugLeft, currentTugRight;

    [Header("Runtime Variables")]
    [SerializeField] private bool isJumping, leftIsTugging, rightIsTugging, isTongueSticking; 
    private float timeSinceTongueRelease;


    [SerializeField] private float jumpforce = 10f;


    #region Subscribe to input actions (OnEnable/OnDisable)
    private void OnEnable()
    {
        inputActions.Enable();

        // Subscribe to the input actions

        // Tongue actions
        var tongueShootAction = inputActions.FindAction("TongueShoot");
        if (tongueShootAction != null) { tongueShootAction.performed += ctx => ShootTongue(); }
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

        // QoL
        var resetFrogAction = inputActions.FindAction("Reset");
        if (resetFrogAction != null) { resetFrogAction.performed += ctx => ResetFrog(); }
        var reloadAction = inputActions.FindAction("Reload");
        if (reloadAction != null) { reloadAction.performed += ctx => Reload(); }

    }

    private void OnDisable()
    {
        inputActions.Disable();


        // Unsubscribe from the input actions to prevent memory leaks

        var tongueShootAction = inputActions.FindAction("TongueShoot");
        if (tongueShootAction != null) { tongueShootAction.performed -= ctx => ShootTongue(); }
        var aimAction = inputActions.FindAction("Look");
        if (aimAction != null) { aimAction.performed -= ctx => mousePosition = ctx.ReadValue<Vector2>(); }

        var leftLegTugAction = inputActions.FindAction("LeftLegTug");
        if (leftLegTugAction != null) { leftLegTugAction.performed -= ctx => leftIsTugging = true; }
        var rightLegTugAction = inputActions.FindAction("RightLegTug");
        if (rightLegTugAction != null) { rightLegTugAction.performed -= ctx => rightIsTugging = true; }

        var leftLegKickAction = inputActions.FindAction("LeftLegKick");
        if (leftLegKickAction != null) { leftLegKickAction.performed -= ctx => JumpAndResetTug(jumpforce * currentTugLeft, true); }
        var rightLegKickAction = inputActions.FindAction("RightLegKick");
        if (rightLegKickAction != null) { rightLegKickAction.performed -= ctx => JumpAndResetTug(jumpforce * currentTugRight, false); }

        var resetFrogAction = inputActions.FindAction("Reset");
        if (resetFrogAction != null) { resetFrogAction.performed -= ctx => ResetFrog(); }
        var reloadAction = inputActions.FindAction("Reload");
        if (reloadAction != null) { reloadAction.performed -= ctx => Reload(); }
    }
    #endregion

    #region Monobehaviour Methods
    private void Start()
    {
        if (recomputeLegLengthOnStart)
        {
            legLength = Vector3.Distance(leftLeg.transform.position, leftLeg.connectedBody.transform.position);
        }

        if (leftLegRb == null) { leftLegRb = leftLeg.GetComponent<Rigidbody>(); }
        if (rightLegRb == null) { rightLegRb = rightLeg.GetComponent<Rigidbody>(); }
        if (tongueRb != null) { tongueRb = tongue.GetComponent<Rigidbody>(); }
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

        if (!isTongueSticking && (timeSinceTongueRelease < stickCooldown || timeSinceTongueRelease < shootCooldown))
        {
            timeSinceTongueRelease += Time.deltaTime;
        }

        AimTongue();
    }
    #endregion

    // if the collider of the tongue hits something, try sticking to it


    #region Aiming Methods
    private void AimTongue()
    {
        Ray ray = Camera.main.ScreenPointToRay(mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            mouseTransform3D.position = new Vector3(hit.point.x, hit.point.y, transform.position.z);
        }
    }
    #endregion

    #region Jumping Methods
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
    #endregion

    #region Tongue Methods
    public void StickTo(Transform target = null, Collision collision = null)
    {
        if (tongueRb == null || target == null && collision == null || timeSinceTongueRelease < stickCooldown) { return; }

        Vector3 contactPoint;
        Vector3 contactNormal;

        if (collision != null)
        {
            ContactPoint contact = collision.contacts[0];
            contactPoint = contact.point;
            contactNormal = contact.normal;

            if (target == null)
            {
                target = collision.transform;
            }
        }
        else
        {
            contactPoint = target.position;
            contactNormal = (transform.position - target.position).normalized;
        }

        tongueRb.position = contactPoint;
        tongueRb.rotation = Quaternion.LookRotation(contactNormal);

        tongueRb.transform.SetParent(target);
        tongueRb.transform.localPosition = Vector3.zero;
        tongueRb.transform.localRotation = Quaternion.identity;
        tongueRb.isKinematic = true;

        isTongueSticking = true;
    }

    public void ReleaseTongue()
    {
        tongueRb.transform.SetParent(null);
        tongueRb.isKinematic = false;

        isTongueSticking = false;
        timeSinceTongueRelease = 0f;
    }

    private void ShootTongue()
    {        
        if (isTongueSticking)
        {
            ReleaseTongue();
            tongueRb.position = transform.position;
            tongueRb.rotation = Quaternion.identity;
            tongueCollider.enabled = false;
        }
        else // Try finding a target (by Tag) to stick to
        {
            if (timeSinceTongueRelease < shootCooldown) { return; }

            if (Vector3.Distance(mouseTransform3D.position, transform.position) > maxTongueDistance)
            {
                tongueRb.AddForce((mouseTransform3D.position - tongueRb.position).normalized * tongueShootForce, ForceMode.Impulse);
                timeSinceTongueRelease = 0f; // Reset cooldown to prevent immediate re-shooting
                return;
            }

            tongueCollider.enabled = true;
            Physics.Raycast(mouseTransform3D.position, -mouseTransform3D.forward, out RaycastHit hit, 20f);

            // First, try a direct raycast to find a target
            if (hit.collider != null)
            {
                Debug.Log("Hit: " + hit.collider.gameObject.name);
                if (hit.collider.gameObject.CompareTag("Target"))
                {
                    StickTo(hit.transform);
                }
            }
            else
            {
                bool foundTarget = false;
                // When no direct hit, check for nearby colliders within a certain radius to find a target
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
                        StickTo(closestCollider.transform);
                        foundTarget = true;
                    }
                }

                if (!foundTarget)
                {
                    tongueRb.AddForce((mouseTransform3D.position - tongueRb.position).normalized * tongueShootForce, ForceMode.Impulse);
                    timeSinceTongueRelease = 0f; // Reset cooldown to prevent immediate re-shooting
                }
            }
        }
    }
    #endregion

    #region QoL Methods
    private void ResetFrog()
    {
        transform.position = spawnPosition;
        transform.rotation = Quaternion.identity;

        ReleaseTongue();


        leftLegRb.transform.localPosition = leftLeg.connectedBody.transform.localPosition + new Vector3(0, legLength, 0);
        leftLegRb.linearVelocity = Vector3.zero;

        rightLegRb.transform.localPosition = rightLeg.connectedBody.transform.localPosition + new Vector3(0, legLength, 0);
        rightLegRb.linearVelocity = Vector3.zero;
    }

    public void Reload()
    {

    }

    #endregion
}
