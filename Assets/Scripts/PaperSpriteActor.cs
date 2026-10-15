using UnityEngine;

/// <summary>Authored sprite presentation only; combat timing stays with the gameplay components.</summary>
public class PaperSpriteActor : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private Sprite[] poses;
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField, Min(0.01f)] private float idleFrameDuration = 0.15f;
    [SerializeField] private bool alwaysFaceRight;
    private float idleElapsed;
    private float strikeUntil;
    private bool anticipating;
    private HealthComponent health;
    // Idle-sheet head/sole landmarks: fixed across animation frames.
    public Vector3 DamagePopupPosition => transform.TransformPoint(
        poses != null && poses.Length > 0 && poses[0].name.StartsWith("Ghoul")
            ? new Vector3(-0.76f, 3.55f, 0f)
            : new Vector3(0.17f, 3.59f, 0f));
    public void Configure(SpriteRenderer renderer, Sprite[] frames)
    {
        body = renderer;
        poses = frames;
        body.sprite = poses[0];
    }
    public void ConfigureIdle(SpriteRenderer renderer, Sprite[] frames)
    {
        body = renderer;
        idleFrames = frames;
        // No attack art has been supplied for this character yet. Never flash the old character.
        poses = new Sprite[0];
        alwaysFaceRight = true;
        idleElapsed = 0f;
        body.flipX = false;
        body.sprite = idleFrames[0];
    }
    private void Awake() { health = GetComponent<HealthComponent>(); }
    public void SetAnticipation(bool value) { anticipating = value; }
    public void Strike()
    {
        strikeUntil = Time.time + 0.12f;
        if (body != null && poses != null && poses.Length == 4) body.sprite = poses[3];
    }
    private void LateUpdate()
    {
        if (body == null) return;
        if (alwaysFaceRight) body.flipX = false;
        if (Time.timeScale == 0f) return;
        if (health != null && health.CurrentLife <= 0f) return;
        if (idleFrames != null && idleFrames.Length > 0)
        {
            // Scaled delta time freezes the loop when paused; retain phase through combat cues.
            float duration = Mathf.Max(0.01f, idleFrameDuration);
            idleElapsed = (idleElapsed + Time.deltaTime) % (duration * idleFrames.Length);
            body.sprite = idleFrames[Mathf.FloorToInt(idleElapsed / duration)];
            return;
        }
        if (poses == null || poses.Length != 4) return;
        int pose = Time.time < strikeUntil ? 3 : anticipating ? 2 : ((int)(Time.time / 0.42f) % 2);
        body.sprite = poses[pose];
    }
}
