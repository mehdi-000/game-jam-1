using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// Drives tutorial affordances on the Trick HUD: highlights match live gameplay input
/// (tongue shoot / leg tugs) using the same <see cref="InputActionAsset"/> as the frog.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class HudTutorialController : MonoBehaviour
{
    const string ActiveClass = "tutorial-active";

    [SerializeField] InputActionAsset inputActions;

    UIDocument _doc;
    VisualElement _mouseLeft;
    VisualElement _keyQ;
    VisualElement _keyE;

    InputAction _tongueShoot;
    InputAction _leftLegTug;
    InputAction _rightLegTug;

    void Awake()
    {
        _doc = GetComponent<UIDocument>();
    }

    void OnEnable()
    {
        if (inputActions == null)
            return;

        var playerMap = inputActions.FindActionMap("Player");
        if (playerMap == null)
            return;

        _tongueShoot = playerMap.FindAction("TongueShoot");
        _leftLegTug = playerMap.FindAction("LeftLegTug");
        _rightLegTug = playerMap.FindAction("RightLegTug");
    }

    void Start()
    {
        if (_doc == null)
            return;

        var root = _doc.rootVisualElement;
        _mouseLeft = root.Q<VisualElement>("TutorialMouseLeft");
        _keyQ = root.Q<VisualElement>("TutorialKeyQ");
        _keyE = root.Q<VisualElement>("TutorialKeyE");
    }

    void OnDisable()
    {
        _tongueShoot = null;
        _leftLegTug = null;
        _rightLegTug = null;
    }

    void Update()
    {
        if (_tongueShoot == null || _leftLegTug == null || _rightLegTug == null)
            return;

        SetActive(_mouseLeft, _tongueShoot.IsPressed());
        SetActive(_keyQ, _leftLegTug.IsPressed());
        SetActive(_keyE, _rightLegTug.IsPressed());
    }

    static void SetActive(VisualElement el, bool active)
    {
        if (el == null)
            return;

        if (active)
            el.AddToClassList(ActiveClass);
        else
            el.RemoveFromClassList(ActiveClass);
    }
}
