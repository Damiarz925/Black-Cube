// Developer map: Lightweight runtime projectile/impact presentation for active skills.
using System;
using System.Collections;
using UnityEngine;

public sealed class SkillProjectile : MonoBehaviour
{
    private static Sprite orbSprite;
    private static AudioClip heavyImpactClip;
    public static AudioClip HeavyImpactClip => heavyImpactClip != null ? heavyImpactClip : (heavyImpactClip = CreateImpactClip());

    public static void Launch(Transform source, Transform target, Element element, Action onImpact, float lateralOffset = 0f)
    {
        if (source == null || target == null) return;
        var go = new GameObject("Fireball Projectile", typeof(SpriteRenderer), typeof(SkillProjectile));
        go.hideFlags = HideFlags.DontSave;
        var renderer = go.GetComponent<SpriteRenderer>();
        renderer.sprite = GetOrbSprite();
        renderer.color = ElementColor(element);
        renderer.sortingOrder = 80;
        go.transform.position = source.position + Vector3.up * .9f + Vector3.right * lateralOffset;
        go.transform.localScale = Vector3.one * .55f;
        go.GetComponent<SkillProjectile>().StartCoroutine(
            go.GetComponent<SkillProjectile>().Fly(target, onImpact));
    }

    private IEnumerator Fly(Transform target, Action onImpact)
    {
        Vector3 start = transform.position;
        float elapsed = 0f;
        const float duration = .38f;
        while (elapsed < duration && target != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 end = target.position + Vector3.up * .8f;
            transform.position = Vector3.Lerp(start, end, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * .35f);
            transform.localScale = Vector3.one * (.5f + Mathf.Sin(t * Mathf.PI) * .2f);
            yield return null;
        }
        if (target != null) onImpact?.Invoke();
        Destroy(gameObject);
    }

    private static Sprite GetOrbSprite()
    {
        if (orbSprite != null) return orbSprite;
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Runtime Skill Orb", filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave
        };
        var pixels = new Color[size * size];
        Vector2 center = Vector2.one * (size - 1) * .5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float distance = Vector2.Distance(new Vector2(x, y), center) / (size * .5f);
            float alpha = Mathf.Clamp01(1f - distance);
            pixels[y * size + x] = new Color(1f, 1f, 1f, alpha * alpha);
        }
        texture.SetPixels(pixels);
        texture.Apply();
        orbSprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f, 32f);
        orbSprite.hideFlags = HideFlags.DontSave;
        return orbSprite;
    }

    private static Color ElementColor(Element element) => element switch
    {
        Element.Fire => new Color(1f, .22f, .025f, 1f),
        Element.Cold => new Color(.15f, .75f, 1f, 1f),
        Element.Light => new Color(1f, .92f, .15f, 1f),
        Element.Poison => new Color(.3f, 1f, .2f, 1f),
        Element.Void => new Color(.65f, .25f, .9f, 1f),
        _ => Color.white
    };

    private static AudioClip CreateImpactClip()
    {
        const int rate = 22050;
        const int samples = 2205;
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)rate;
            float envelope = Mathf.Exp(-32f * t);
            data[i] = (Mathf.Sin(2f * Mathf.PI * 72f * t) * .7f
                       + Mathf.Sin(2f * Mathf.PI * 39f * t) * .3f) * envelope;
        }
        var clip = AudioClip.Create("Heavy Strike Impact", samples, 1, rate, false);
        clip.SetData(data, 0);
        clip.hideFlags = HideFlags.DontSave;
        return clip;
    }
}
