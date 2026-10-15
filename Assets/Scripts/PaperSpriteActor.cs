// Developer map: Presentation layer for authored idle and attack cels plus legacy four-pose actors. Attachment positions use body-local Unity units, angles use degrees; empty player equipment hides the entire weapon.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using UnityEngine;

/// <summary>Authored idle and gauge-driven attack poses, or legacy four-pose actors. Never applies damage.</summary>
public class PaperSpriteActor : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private PaperPlayerAnimationSet animationSet;
    [SerializeField] private PaperEnemyAnimationSet enemyAnimationSet;
    [SerializeField] private Sprite[] poses;
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField, Min(0.1f)] private float idleFramesPerSecond = 8f;
    [SerializeField] private Sprite[] attackFrames;
    [SerializeField] private Sprite[] attackWeaponFrames;
    [SerializeField] private bool useWeaponAttachments;
    [SerializeField] private PaperWeaponVisual defaultWeaponVisual;
    [SerializeField] private PaperWeaponGrip[] idleWeaponGrips;
    [SerializeField] private PaperWeaponGrip[] attackWeaponGrips;
    private PlayerController weaponOwner;
    private PaperWeaponGrip currentGrip;
    private bool hasGrip;
    private SpriteRenderer attackWeapon;
    private int attackPose = -1;
    private int impactTick = -1;
    public const int AttackFrameCount = 8;
    public const int ImpactFrame = 3; // Fourth authored drawing: forward impact.
    public bool HasAttackFrames => attackFrames != null && attackFrames.Length == AttackFrameCount;
    private float idleElapsed;
    private float strikeUntil;
    private bool anticipating;
    private HealthComponent health;
    // Idle-sheet head/sole landmarks: fixed across animation frames.
    public string DisplayName => enemyAnimationSet != null ? enemyAnimationSet.displayName : "Ghoul";
    public Vector3 DamagePopupPosition => transform.TransformPoint(
        enemyAnimationSet != null ? enemyAnimationSet.popupOffset :
        poses != null && poses.Length > 0 && poses[0].name.StartsWith("Ghoul")
            ? new Vector3(-0.76f, 3.55f, 0f)
            : new Vector3(0.17f, 3.59f, 0f));
    public void Configure(SpriteRenderer renderer, Sprite[] frames)
    {
        body = renderer;
        poses = frames;
        idleFrames = null;
        body.sprite = poses[0];
    }
    public void ConfigureIdle(SpriteRenderer renderer, Sprite[] frames, float framesPerSecond = 8f)
    {
        body = renderer;
        poses = null;
        idleFrames = frames;
        idleFramesPerSecond = Mathf.Max(0.1f, framesPerSecond);
        idleElapsed = 0f;
        if (body != null && idleFrames != null && idleFrames.Length > 0)
        {
            body.flipX = false;
            body.sprite = idleFrames[0];
        }
    }
    public void ConfigureAttack(Sprite[] frames, Sprite[] weaponFrames)
    {
        attackFrames = frames;
        SetAttackWeaponFrames(weaponFrames);
    }
    public void ConfigureAnimationSet(PaperPlayerAnimationSet animations)
    {
        animationSet = animations;
        if (animations == null) return;
        ConfigureIdle(body, animations.idleFrames, animations.idleFramesPerSecond);
        ConfigureAttack(animations.attackFrames, null);
        ConfigureWeaponAttachments(animations.defaultWeaponVisual,
            animations.idleWeaponGrips, animations.attackWeaponGrips);
    }
    public void ConfigureEnemyAnimationSet(PaperEnemyAnimationSet animations)
    {
        enemyAnimationSet = animations;
        if (animations == null) return;
        // Enemy weapons belong to their authored cels; player equipment attachments
        // stay an independent path and must not hide an enemy's baked weapon.
        useWeaponAttachments = false;
        ConfigureIdle(body, animations.idleFrames, animations.idleFramesPerSecond);
        ConfigureAttack(animations.attackFrames, null);
    }
    // Equipment can supply an eight-pose weapon layer without replacing character art.
    public void SetAttackWeaponFrames(Sprite[] frames)
    {
        attackWeaponFrames = frames;
        if (attackWeapon != null) attackWeapon.enabled = false;
    }
    public void ConfigureWeaponAttachments(PaperWeaponVisual fallback,
        PaperWeaponGrip[] idleGrips, PaperWeaponGrip[] attackGrips)
    {
        useWeaponAttachments = true;
        defaultWeaponVisual = fallback;
        idleWeaponGrips = idleGrips;
        attackWeaponGrips = attackGrips;
        attackWeaponFrames = null;
        BindWeaponOwner();
        ShowGrip(idleWeaponGrips, 0);
    }
    private void BindWeaponOwner()
    {
        var owner = GetComponent<PlayerController>();
        if (owner == weaponOwner) return;
        if (weaponOwner != null) weaponOwner.AttackChanged -= RefreshAttachedWeapon;
        weaponOwner = owner;
        if (weaponOwner != null) weaponOwner.AttackChanged += RefreshAttachedWeapon;
    }
    private void OnEnable() { BindWeaponOwner(); }
    private void OnDisable()
    {
        if (weaponOwner != null) weaponOwner.AttackChanged -= RefreshAttachedWeapon;
        weaponOwner = null;
        if (attackWeapon != null) attackWeapon.enabled = false;
    }
    private void ShowGrip(PaperWeaponGrip[] grips, int index)
    {
        hasGrip = grips != null && index >= 0 && index < grips.Length;
        if (hasGrip) currentGrip = grips[index];
        RefreshAttachedWeapon();
    }
    private void RefreshAttachedWeapon()
    {
        if (!useWeaponAttachments) return;
        EnsureWeaponRenderer();
        if (attackWeapon == null) return;
        var equipped = weaponOwner != null ? weaponOwner.EquippedWeapon : null;
        // Empty equipment slot is always empty, even when a fallback art profile exists.
        var visual = equipped != null ? (equipped.WeaponVisual != null ? equipped.WeaponVisual : defaultWeaponVisual) : null;
        if (!hasGrip || visual == null || visual.sprite == null)
        {
            attackWeapon.enabled = false;
            attackWeapon.sprite = null;
            return;
        }
        float scale = Mathf.Max(0.01f, visual.scale);
        var rotation = Quaternion.Euler(0f, 0f, currentGrip.angle + visual.rotationOffset);
        var offset = rotation * new Vector3(visual.gripOffset.x * scale, visual.gripOffset.y * scale, 0f);
        attackWeapon.transform.localPosition = new Vector3(currentGrip.position.x, currentGrip.position.y, 0f) - offset;
        attackWeapon.transform.localRotation = rotation;
        attackWeapon.transform.localScale = new Vector3(scale, scale, 1f);
        attackWeapon.sortingOrder = body.sortingOrder + (currentGrip.inFront ? 1 : -1);
        attackWeapon.sprite = visual.sprite;
        attackWeapon.enabled = true;
    }
    private void EnsureWeaponRenderer()
    {
        if (attackWeapon != null || body == null) return;
        var child = new GameObject("Paper Attack Weapon", typeof(SpriteRenderer));
        child.transform.SetParent(body.transform, false);
        attackWeapon = child.GetComponent<SpriteRenderer>();
        attackWeapon.sharedMaterial = body.sharedMaterial;
        attackWeapon.sortingLayerID = body.sortingLayerID;
        attackWeapon.sortingOrder = body.sortingOrder + 1;
        attackWeapon.enabled = false;
    }
    private void ShowAttackPose(int index)
    {
        if (body == null || !HasAttackFrames) return;
        attackPose = Mathf.Min(index, AttackFrameCount - 1);
        body.flipX = false;
        body.sprite = attackFrames[attackPose];
        if (useWeaponAttachments)
        {
            ShowGrip(attackWeaponGrips, attackPose);
            return;
        }
        EnsureWeaponRenderer();
        bool hasWeapon = attackWeaponFrames != null && attackWeaponFrames.Length == AttackFrameCount;
        attackWeapon.sprite = hasWeapon ? attackWeaponFrames[attackPose] : null;
        attackWeapon.enabled = hasWeapon && attackWeapon.sprite != null;
    }
    public void CancelAttack()
    {
        attackPose = -1;
        // Enemy death can synchronously spawn the next target in the damage call.
        // Keep the impact visible for this rendered frame even when that resets the gauge.
        if (impactTick == Time.frameCount) return;
        if (attackWeapon != null) attackWeapon.enabled = false;
        if (body != null && idleFrames != null && idleFrames.Length > 0) body.sprite = idleFrames[0];
        if (useWeaponAttachments) ShowGrip(idleWeaponGrips, 0);
    }
    public void ShowAttackGauge(float gaugeFraction, bool hasLandedTurn)
    {
        if (!HasAttackFrames || impactTick == Time.frameCount) return;
        const float impactPhase = ImpactFrame / (float)AttackFrameCount;
        if (!hasLandedTurn && gaugeFraction < 1f - impactPhase)
        {
            CancelAttack();
            return;
        }
        // Gauge threshold is damage. Windup precedes it; recovery follows it.
        float phase = Mathf.Repeat(gaugeFraction + impactPhase, 1f);
        ShowAttackPose((int)(phase * AttackFrameCount));
    }
    private void Awake()
    {
        health = GetComponent<HealthComponent>();
        BindWeaponOwner();
        if (animationSet != null) ConfigureAnimationSet(animationSet);
        else if (enemyAnimationSet != null) ConfigureEnemyAnimationSet(enemyAnimationSet);
    }
    public void SetAnticipation(bool value) { anticipating = value; }
    public void Strike()
    {
        if (HasAttackFrames)
        {
            impactTick = Time.frameCount;
            ShowAttackPose(ImpactFrame);
            return;
        }
        strikeUntil = Time.time + 0.12f;
        if (body != null && (idleFrames == null || idleFrames.Length == 0)
            && poses != null && poses.Length == 4) body.sprite = poses[3];
    }
    private void LateUpdate()
    {
        if (useWeaponAttachments) RefreshAttachedWeapon();
        if (body == null || Time.timeScale == 0f || SkillTreeUI.IsOpen) return;
        if (health != null && health.CurrentLife <= 0f) return;
        if (impactTick == Time.frameCount || attackPose >= 0) return;
        if (attackWeapon != null && !useWeaponAttachments) attackWeapon.enabled = false;
        if (idleFrames != null && idleFrames.Length > 0)
        {
            // Combat poses are driven by the BattleManager gauge, never this idle clock.
            float rate = Mathf.Max(0.1f, idleFramesPerSecond);
            idleElapsed = Mathf.Repeat(idleElapsed + Time.deltaTime, idleFrames.Length / rate);
            body.flipX = false;
            int index = Mathf.Min((int)(idleElapsed * rate), idleFrames.Length - 1);
            body.sprite = idleFrames[index];
            if (useWeaponAttachments) ShowGrip(idleWeaponGrips, index);
            return;
        }
        if (poses == null || poses.Length != 4) return;
        int pose = Time.time < strikeUntil ? 3 : anticipating ? 2 : ((int)(Time.time / 0.42f) % 2);
        body.sprite = poses[pose];
    }
}
