using UnityEngine;
using UnityEngine.InputSystem;

// Debug helper: put this on the Player object.
// - Shows position, velocity, state and facing on screen (toggle with F1).
// - Draws the player's colliders (cyan) and the interaction BoxCast (green/yellow).
//   Colliders show in the Game view only when the "Gizmos" button in the Game view toolbar is on.
public class DebugOverlay : MonoBehaviour
{
    [Header("Toggle")]
    public bool showOverlay  = true;
    public bool showHitboxes = true;
    public Key  toggleKey    = Key.F1;

    public enum Corner { TopLeft, TopRight, BottomLeft, BottomRight }

    [Header("Overlay Position (press F2 in Play mode to move it to the next corner)")]
    public Corner  corner        = Corner.TopLeft;
    public Key     moveKey       = Key.F2;
    public Vector2 margin        = new Vector2(10, 10);
    public Vector2 overlaySize   = new Vector2(430, 150);

    [Header("Colors")]
    public Color colliderColor = Color.cyan;
    public Color interactColor = Color.green;
    public Color hitColor      = Color.yellow;

    private Rigidbody      _rb;
    private PlayerMovement _movement;
    private PlayerAction   _action;
    private GUIStyle       _style;

    void Awake()
    {
        _rb       = GetComponent<Rigidbody>();
        _movement = GetComponent<PlayerMovement>();
        _action   = GetComponent<PlayerAction>();
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current[toggleKey].wasPressedThisFrame)
            showOverlay = !showOverlay;

        if (Keyboard.current[moveKey].wasPressedThisFrame)
            corner = (Corner)(((int)corner + 1) % 4);
    }

    void OnGUI()
    {
        if (!showOverlay) return;

        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize  = 16,
                padding   = new RectOffset(10, 10, 8, 8)
            };
            _style.normal.textColor = Color.white;
        }

        bool right  = corner == Corner.TopRight    || corner == Corner.BottomRight;
        bool bottom = corner == Corner.BottomLeft  || corner == Corner.BottomRight;
        float x = right  ? Screen.width  - overlaySize.x - margin.x : margin.x;
        float y = bottom ? Screen.height - overlaySize.y - margin.y : margin.y;

        Vector3 p   = transform.position;
        Vector3 v   = _rb != null ? _rb.linearVelocity : Vector3.zero;
        Vector3 dir = _movement != null ? _movement.GetLookDirection() : transform.forward;

        string text =
            $"<b>DEBUG</b>  ({toggleKey} hide, {moveKey} move)\n" +
            $"Position:  X {p.x:0.00}   Y {p.y:0.00}   Z {p.z:0.00}\n" +
            $"Velocity:  X {v.x:0.00}   Y {v.y:0.00}   Z {v.z:0.00}   (speed {v.magnitude:0.00})\n" +
            $"Facing:    X {dir.x:0.00}   Z {dir.z:0.00}\n" +
            $"State:     {PlayerStateManager.CurrentState}\n" +
            $"FPS:       {(1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f)):0}";

        _style.richText = true;
        GUI.Box(new Rect(x, y, overlaySize.x, overlaySize.y), text, _style);
    }

    void OnDrawGizmos()
    {
        if (!showHitboxes) return;

        // Player's own colliders
        Gizmos.color = colliderColor;
        foreach (Collider c in GetComponentsInChildren<Collider>())
            DrawCollider(c);

        // Interaction BoxCast, matching PlayerAction.TryInteract
        PlayerAction action = _action != null ? _action : GetComponent<PlayerAction>();
        if (action == null) return;

        PlayerMovement movement = _movement != null ? _movement : GetComponent<PlayerMovement>();
        Vector3 look = movement != null ? movement.GetLookDirection() : transform.forward;
        look.y = 0f;
        if (look.sqrMagnitude < 0.0001f) look = Vector3.forward;
        look.Normalize();

        Vector3 size  = new Vector3(action.boxWidth, action.boxHeight, action.boxDepth);
        Vector3 start = transform.position;
        Vector3 end   = start + look * action.interactionDistance;

        bool hitSomething = Physics.BoxCast(start, size * 0.5f, look, out RaycastHit hit,
                                            Quaternion.identity, action.interactionDistance,
                                            action.interactableLayer);

        Gizmos.color = hitSomething ? hitColor : interactColor;
        Gizmos.DrawWireCube(start, size);
        Gizmos.DrawWireCube(end, size);
        Gizmos.DrawLine(start, end);

        if (hitSomething)
        {
            Gizmos.DrawSphere(hit.point, 0.2f);
            DrawCollider(hit.collider);
        }
    }

    private static void DrawCollider(Collider c)
    {
        if (c == null || !c.enabled) return;

        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = c.transform.localToWorldMatrix;

        switch (c)
        {
            case BoxCollider box:
                Gizmos.DrawWireCube(box.center, box.size);
                break;
            case SphereCollider sphere:
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
                break;
            case CapsuleCollider cap:
                Gizmos.matrix = old;
                Gizmos.DrawWireCube(c.bounds.center, c.bounds.size);
                break;
            default:
                Gizmos.matrix = old;
                Gizmos.DrawWireCube(c.bounds.center, c.bounds.size);
                break;
        }

        Gizmos.matrix = old;
    }
}