using UnityEngine;

/// <summary>Four authored poses: idle A/B, anticipation and strike. No combat logic.</summary>
public class PaperSpriteActor : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private Sprite[] poses;
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
    private void Awake() { health = GetComponent<HealthComponent>(); }
    public void SetAnticipation(bool value) { anticipating = value; }
    public void Strike()
    {
        strikeUntil = Time.time + 0.12f;
        if (body != null && poses.Length == 4) body.sprite = poses[3];
    }
    private void LateUpdate()
    {
        if (body == null || poses == null || poses.Length != 4 || Time.timeScale == 0f) return;
        if (health != null && health.CurrentLife <= 0f) return;
        int pose = Time.time < strikeUntil ? 3 : anticipating ? 2 : ((int)(Time.time / 0.42f) % 2);
        body.sprite = poses[pose];
    }
}
