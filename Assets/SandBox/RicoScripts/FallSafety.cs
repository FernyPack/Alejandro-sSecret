using UnityEngine;

// FALL SAFETY
// If I fall off the map, it puts me back where I was last standing.
// Also stops me from walking through walls when moving fast.
public class FallSafety : MonoBehaviour
{
    [Tooltip("How far below the starting height counts as 'fell off the map'.")]
    public float fallDistance = 15f;

    [Tooltip("How far down to check for ground under the player.")]
    public float groundCheckDistance = 5f;

    private Rigidbody _rb;
    private Vector3   _spawnPosition;
    private Vector3   _lastSafePosition;
    private float     _killHeight;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb != null)
        {
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // don't pass through walls
            _rb.interpolation          = RigidbodyInterpolation.Interpolate;        // smoother movement
        }

        _spawnPosition    = transform.position;
        _lastSafePosition = _spawnPosition;
        _killHeight       = _spawnPosition.y - fallDistance;
    }

    void FixedUpdate()
    {
        if (IsGrounded())
            _lastSafePosition = transform.position;

        if (transform.position.y < _killHeight)
            Respawn();
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance,
                               ~0, QueryTriggerInteraction.Ignore);
    }

    private void Respawn()
    {
        Vector3 target = _lastSafePosition.y > _killHeight ? _lastSafePosition : _spawnPosition;

        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.position       = target;
        }
        transform.position = target;

        Debug.Log("[FallSafety] Player fell off the map and was moved back.");
    }
}