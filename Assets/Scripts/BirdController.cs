using UnityEngine;

/// <summary>
/// Controls the player bird.
/// - Applies an upward impulse on input (flap)
/// - Rotates visually based on velocity (nose-down when falling, nose-up when flapping)
/// - Notifies GameManager on collision with pipes or ground
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BirdController : MonoBehaviour
{
    [Header("Flap Settings")]
    [Tooltip("Upward force applied each flap")]
    [SerializeField] private float flapForce = 7f;

    [Header("Rotation Settings")]
    [Tooltip("Velocity at which the bird looks straight (horizontal)")]
    [SerializeField] private float referenceVelocity = 3f;
    [Tooltip("Max downward tilt angle in degrees")]
    [SerializeField] private float maxDownAngle = -80f;
    [Tooltip("Max upward tilt angle in degrees")]
    [SerializeField] private float maxUpAngle = 35f;
    [Tooltip("How quickly rotation interpolates to target")]
    [SerializeField] private float rotationSpeed = 10f;

    private Rigidbody2D rb;
    private bool isDead = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        // Keep the bird frozen until the game starts
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
    }

    private void Update()
    {
        if (isDead) return;

        // Accept flap input: Space key, left mouse click, or any touch
        bool flapInput = Input.GetKeyDown(KeyCode.Space)
                      || Input.GetMouseButtonDown(0)
                      || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);

        if (flapInput)
        {
            Flap();
        }

        UpdateRotation();
    }

    /// <summary>Call this to activate the bird (called by GameManager on game start)</summary>
    public void Activate()
    {
        isDead = false;
        rb.gravityScale = 3f;
        Flap(); // Give an initial boost when the game starts
    }

    private void Flap()
    {
        // Override any downward velocity, then apply upward force
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * flapForce, ForceMode2D.Impulse);
    }

    private void UpdateRotation()
    {
        // Map vertical velocity to a rotation angle
        // Positive velocity (rising)  → positive angle (nose up)
        // Negative velocity (falling) → negative angle (nose down)
        float targetAngle = Mathf.Lerp(0f, maxDownAngle, -rb.linearVelocity.y / referenceVelocity);
        targetAngle = Mathf.Clamp(targetAngle, maxDownAngle, maxUpAngle);

        // When flapping hard upward, snap to max up angle quickly
        if (rb.linearVelocity.y > referenceVelocity)
            targetAngle = maxUpAngle;

        float currentAngle = transform.eulerAngles.z;
        // Normalize angle to [-180, 180] for correct interpolation
        if (currentAngle > 180f) currentAngle -= 360f;

        float newAngle = Mathf.Lerp(currentAngle, targetAngle, rotationSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Pipe"))
        {
            Die();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;
        if (other.CompareTag("Pipe"))
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        rb.gravityScale = 1f; // Let the bird fall naturally on death
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        GameManager.Instance.OnBirdDied();
    }

    public void ResetBird(Vector3 startPosition)
    {
        isDead = false;
        transform.position = startPosition;
        transform.rotation = Quaternion.identity;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
    }
}
