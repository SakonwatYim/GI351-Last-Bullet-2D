using UnityEngine;

// World-space health bar that floats above a Monster.
// Red fill drops instantly on hit, then a white "damage" bar catches up after a short delay.
// Monster adds this automatically; add it to a prefab yourself only to tweak the settings.
// The bar is built from SpriteRenderers at runtime and is NOT parented to the monster,
// so it doesn't flip when the monster turns around (localScale.x = -1).
[RequireComponent(typeof(Monster))]
public class MonsterHealthBar : MonoBehaviour
{
    [Header("Size / Position")]
    [SerializeField] private Vector2 size = new Vector2(1.2f, 0.18f);
    [SerializeField] private float border = 0.05f;           // black edge around the bar
    [SerializeField] private float heightAboveSprite = 0.25f;

    [Header("Colors")]
    [SerializeField] private Color backgroundColor = Color.black;
    [SerializeField] private Color damageColor = Color.white;
    [SerializeField] private Color fillColor = new Color(0.9f, 0.1f, 0.1f);

    [Header("White Bar")]
    [SerializeField] private float damageDelay = 0.4f;     // wait before the white bar starts dropping
    [SerializeField] private float damageDropSpeed = 1.5f; // fraction of the bar per second

    [Header("Visibility")]
    [SerializeField] private bool hideWhenFull = false;
    [SerializeField] private int sortingOrderOffset = 10;

    private static Sprite pixel;

    private Monster monster;
    private SpriteRenderer monsterSprite;
    private Transform root;
    private Transform damageBar;
    private Transform fillBar;
    private float shownFill = 1f;
    private float damageFill = 1f;
    private float damageDropTime;

    private void Awake()
    {
        monster = GetComponent<Monster>();
        monsterSprite = GetComponentInChildren<SpriteRenderer>();
        Build();
    }

    private void OnDestroy()
    {
        if (root != null) Destroy(root.gameObject);
    }

    private void LateUpdate()
    {
        float target = monster.MaxHealth > 0f ? Mathf.Clamp01(monster.Health / monster.MaxHealth) : 0f;

        if (target < shownFill) damageDropTime = Time.time + damageDelay; // got hit -> restart the delay
        shownFill = target;

        if (damageFill < shownFill)
            damageFill = shownFill;                                        // healed -> snap up
        else if (Time.time >= damageDropTime)
            damageFill = Mathf.MoveTowards(damageFill, shownFill, damageDropSpeed * Time.deltaTime);

        SetBar(fillBar, shownFill);
        SetBar(damageBar, damageFill);

        root.gameObject.SetActive(!(hideWhenFull && damageFill >= 1f));
        root.position = BarPosition();
    }

    private Vector3 BarPosition()
    {
        float top = monsterSprite != null ? monsterSprite.bounds.max.y : transform.position.y + 0.5f;
        return new Vector3(transform.position.x, top + heightAboveSprite, transform.position.z);
    }

    private void Build()
    {
        root = new GameObject($"{name} HealthBar").transform;
        root.position = BarPosition();

        int layer = monsterSprite != null ? monsterSprite.sortingLayerID : 0;
        int order = (monsterSprite != null ? monsterSprite.sortingOrder : 0) + sortingOrderOffset;

        CreatePart("Background", size + Vector2.one * border * 2f, backgroundColor, layer, order);
        damageBar = CreatePart("Damage", size, damageColor, layer, order + 1);
        fillBar = CreatePart("Fill", size, fillColor, layer, order + 2);
    }

    private Transform CreatePart(string partName, Vector2 partSize, Color color, int layer, int order)
    {
        var go = new GameObject(partName);
        go.transform.SetParent(root, false);
        go.transform.localScale = new Vector3(partSize.x, partSize.y, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetPixel();
        sr.color = color;
        sr.sortingLayerID = layer;
        sr.sortingOrder = order;
        return go.transform;
    }

    // Shrinks a bar toward its left edge.
    private void SetBar(Transform bar, float fill)
    {
        bar.localScale = new Vector3(size.x * fill, size.y, 1f);
        bar.localPosition = new Vector3(-size.x * (1f - fill) * 0.5f, 0f, 0f);
    }

    private static Sprite GetPixel()
    {
        if (pixel != null) return pixel;
        var tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        pixel = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return pixel;
    }
}