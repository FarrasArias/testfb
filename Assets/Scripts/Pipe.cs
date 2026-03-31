using UnityEngine;

/// <summary>
/// Controls a single pipe pair.
/// - Scrolls the pipe pair left at a constant speed
/// - Positions the gap at a random height on spawn
/// - Self-destructs when it passes off the left side of the screen
/// </summary>
public class Pipe : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("How fast the pipe moves left (units per second)")]
    [SerializeField] private float scrollSpeed = 4f;

    [Header("Gap Settings")]
    [Tooltip("Half the height of the gap between top and bottom pipes")]
    [SerializeField] private float gapHalfHeight = 1.8f;

    [Header("References")]
    [SerializeField] private Transform topPipe;
    [SerializeField] private Transform bottomPipe;

    private bool isScrolling = false;
    private const float destroyX = -12f;

    private void Start()
    {
        // Apply random vertical offset so each pipe pair is at a different height
        float randomOffset = Random.Range(-2.5f, 2.5f);
        SetGapPosition(randomOffset);
    }

    private void SetGapPosition(float centerY)
    {
        if (topPipe != null)
        {
            // Top pipe is above the gap center
            topPipe.localPosition = new Vector3(0f, centerY + gapHalfHeight + topPipe.localScale.y * 0.5f, 0f);
        }
        if (bottomPipe != null)
        {
            // Bottom pipe is below the gap center
            bottomPipe.localPosition = new Vector3(0f, centerY - gapHalfHeight - bottomPipe.localScale.y * 0.5f, 0f);
        }
    }

    /// <summary>Called by GameManager to start/stop pipe movement</summary>
    public void SetScrolling(bool scroll)
    {
        isScrolling = scroll;
    }

    private void Update()
    {
        if (!isScrolling) return;

        // Move left
        transform.Translate(Vector3.left * scrollSpeed * Time.deltaTime);

        // Destroy when fully off screen
        if (transform.position.x < destroyX)
        {
            Destroy(gameObject);
        }
    }
}
