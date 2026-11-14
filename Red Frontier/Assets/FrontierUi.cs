using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal static class FrontierUi
{
    public static readonly Color32 Background = new Color32(32, 29, 39, 255);
    public static readonly Color32 Panel = new Color32(51, 42, 53, 255);
    public static readonly Color32 Cream = new Color32(248, 229, 200, 255);
    public static readonly Color32 Coral = new Color32(228, 108, 107, 255);
    public const string MotionPreference = "ReduceMotion";
    public static bool ReduceMotion => PlayerPrefs.GetInt(MotionPreference) == 1;

    public static RectTransform CreateCanvas(out EventSystem events)
    {
        var root = new GameObject("Frontier Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        events = new GameObject("Event System", typeof(EventSystem),
            typeof(StandaloneInputModule)).GetComponent<EventSystem>();
        Fill("Backdrop", root.transform, Background);
        return root.GetComponent<RectTransform>();
    }

    public static RectTransform FitLayout(Transform canvas)
    {
        var layout = Box("Layout", canvas);
        layout.gameObject.AddComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        return layout;
    }

    public static void Resize(RectTransform canvas, RectTransform layout, Vector2 size)
    {
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = size;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        layout.GetComponent<AspectRatioFitter>().aspectRatio = size.x / size.y;
    }

    // Screen coordinates in design pixels, measured from the top left.
    public static void Place(RectTransform rect, Vector2 layoutSize, float x, float y, float width, float height)
    {
        rect.anchorMin = new Vector2(x / layoutSize.x, 1 - (y + height) / layoutSize.y);
        rect.anchorMax = new Vector2((x + width) / layoutSize.x, 1 - y / layoutSize.y);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static RectTransform Box(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    public static Image Fill(string name, Transform parent, Color color)
    {
        var image = Box(name, parent).gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    public static RawImage Art(string name, Transform parent, Texture texture)
    {
        var image = Box(name, parent).gameObject.AddComponent<RawImage>();
        image.texture = texture;
        image.raycastTarget = false;
        return image;
    }

    public static Text Label(string name, Transform parent, string caption, int size,
        TextAnchor alignment = TextAnchor.MiddleCenter)
    {
        var label = Box(name, parent).gameObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = size;
        label.fontStyle = FontStyle.Bold;
        label.alignment = alignment;
        label.color = Cream;
        label.text = caption;
        label.raycastTarget = false;
        return label;
    }

    public static Button Action(string caption, Transform parent, int size, bool primary = false) =>
        Action<Button>(caption, parent, size, primary);

    public static T Action<T>(string caption, Transform parent, int size, bool primary = false) where T : Button
    {
        var hit = Fill(caption, parent, Color.clear);
        hit.raycastTarget = true;
        var button = hit.gameObject.AddComponent<T>();
        var focus = Fill("Focus", hit.transform, Color.white);
        focus.rectTransform.offsetMin = new Vector2(-3, -3);
        focus.rectTransform.offsetMax = new Vector2(3, 3);
        button.targetGraphic = focus;
        button.colors = new ColorBlock
        {
            normalColor = Color.clear,
            highlightedColor = Cream,
            selectedColor = Cream,
            pressedColor = Coral,
            disabledColor = Color.clear,
            colorMultiplier = 1,
            fadeDuration = 0
        };
        var face = Fill("Face", hit.transform, primary ? Coral : Panel);
        var outline = face.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color32(122, 90, 96, 255);
        outline.effectDistance = new Vector2(1, 1);
        var label = Label("Label", hit.transform, caption, size);
        label.color = primary ? Background : Cream;
        return button;
    }
}

