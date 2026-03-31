using UnityEngine;

/// <summary>
/// Placed as a child of each pipe pair prefab (the invisible collider between the pipes).
/// When the bird passes through it, awards a point.
///
/// Setup:
///   - Add this script to the ScoreTrigger child object of PipePair
///   - The collider on this object must have "Is Trigger" checked
///   - Make sure the Bird has the "Player" tag
/// </summary>
public class ScoreTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            ScoreManager.Instance?.AddPoint();
        }
    }
}
