using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static FrontierUi;

public sealed class MainMenu : MonoBehaviour
{
    private const string MissionScene = "MarsOutpost";
    private const float OrbitSeconds = 18, Tilt = 12 * Mathf.Deg2Rad, Flatten = .18f;
    // Visible bounds of the approved art, as fractions of each texture.
    private const float LogoLeft = .097f, LogoRight = .905f, LogoTop = .034f, LogoBottom = .949f;
    private const float MarsRadius = .448f;
    private static readonly Color32 ArcColor = new Color32(240, 168, 150, 255);

    [SerializeField] private Texture2D logo;
    [SerializeField] private Texture2D mars;
    [SerializeField] private Texture2D satelliteArt;
    [SerializeField] private Texture2D sky;
    [SerializeField] private Texture2D blackHoleArt;

    private RectTransform canvas, layout;
    private RawImage title, planet, satellite, stars, blackHole;
    private OrbitArc rearArc, frontArc;
    private Image star, fade;
    private RectTransform helpPanel;
    private MenuButton start, help;
    private Button motion, back;
    private EventSystem events;
    private Vector2 size, center;
    private float orbitWidth, motionTime;
    private bool narrow, reduced, launching;

    private void Awake()
    {
        reduced = ReduceMotion;
        canvas = CreateCanvas(out events);
        stars = Art("Star field", canvas, sky);
        layout = FitLayout(canvas);
        blackHole = Art("Distant black hole", layout, Crisp(blackHoleArt));
        blackHole.color = new Color(1, 1, 1, .6f);

        // Back to front: rear arc, relay on its rear pass, Mars, front arc, relay on its near pass.
        rearArc = Arc("Rear orbit", false);
        satellite = Art("Relay", layout, Crisp(satelliteArt));
        planet = Art("Mars", layout, mars);
        frontArc = Arc("Front orbit", true);
        title = Art("Red Frontier", layout, logo);
        star = Fill("Title star glow", title.transform, Color.clear);
        Fill("Star glow center", star.transform, new Color32(255, 230, 191, 255))
            .rectTransform.localScale = new Vector3(.4f, .4f, 1);

        start = MenuAction("Start Mission", 32, true);
        help = MenuAction("Controls / Help", 28, false);
        helpPanel = Fill("Help panel", layout, Panel).rectTransform;
        SetInPanel(Label("Heading", helpPanel, "Controls / Help", 30).rectTransform, .78f, .95f);
        SetInPanel(Label("Instructions", helpPanel,
            "Click or tap a button.\nArrow keys or WASD move focus.\nEnter or Space selects.\nEsc closes this panel.", 22)
            .rectTransform, .38f, .78f);
        motion = Action("Reduce motion", helpPanel, 24);
        back = Action("Back", helpPanel, 24, true);
        SetInPanel(motion.GetComponent<RectTransform>(), .23f, .37f);
        SetInPanel(back.GetComponent<RectTransform>(), .05f, .19f);
        UpdateMotionLabel();

        fade = Fill("Scene fade", canvas, new Color32(32, 29, 39, 0));
        helpPanel.gameObject.SetActive(false);

        start.onClick.AddListener(Launch);
        help.onClick.AddListener(() => ShowHelp(true));
        back.onClick.AddListener(() => ShowHelp(false));
        motion.onClick.AddListener(ToggleMotion);
        events.firstSelectedGameObject = start.gameObject;
        start.Select();
        ApplyLayout(IsNarrow);
    }

    private static bool IsNarrow => Screen.width < Screen.height * 1.25f;

    private void Update()
    {
        if (narrow != IsNarrow)
            ApplyLayout(IsNarrow);
        CoverSky();

        bool helpOpen = helpPanel.gameObject.activeSelf;
        var selected = events.currentSelectedGameObject;
        if (helpOpen && !launching)
        {
            if (Input.GetButtonDown("Cancel"))
                ShowHelp(false);
            else if (selected == null || !selected.transform.IsChildOf(helpPanel))
                motion.Select();
        }
        else if (!launching && selected == null &&
                 (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0))
            start.Select();

        if (!reduced)
        {
            motionTime += Time.unscaledDeltaTime;
            DrawMotion();
        }
    }

    private void ApplyLayout(bool isNarrow)
    {
        narrow = isNarrow;
        size = narrow ? new Vector2(500, 840) : new Vector2(1280, 720);
        Resize(canvas, layout, size);

        // The logo's visible lettering and both buttons share one left edge and width.
        float left = narrow ? 50 : 72, width = narrow ? 400 : 440, top = narrow ? 32 : 130;
        float logoWidth = width / (LogoRight - LogoLeft);
        float logoHeight = logoWidth * logo.height / logo.width;
        Place(title.rectTransform, size, left - LogoLeft * logoWidth, top - LogoTop * logoHeight, logoWidth, logoHeight);
        Place(star.rectTransform, new Vector2(logoWidth, logoHeight),
            logoWidth * .717f - 7, logoHeight * .175f - 7, 14, 14);

        float logoEnd = top + (LogoBottom - LogoTop) * logoHeight;
        float startY = narrow ? 636 : logoEnd + 48;
        Place(start.GetComponent<RectTransform>(), size, left, startY, width, 72);
        Place(help.GetComponent<RectTransform>(), size, left, startY + 92, width, 72);
        Place(helpPanel, size, narrow ? 25 : 360, narrow ? 198 : 132, narrow ? 450 : 560, narrow ? 444 : 456);

        // Mars fills the right half; the orbit's ends clear the silhouette by the relay's half-diagonal.
        float radius = narrow ? 158 : 248;
        center = narrow ? new Vector2(250, (logoEnd + startY) / 2) : new Vector2(960, 360);
        float planetSize = radius / MarsRadius;
        Place(planet.rectTransform, size, center.x - planetSize / 2, center.y - planetSize / 2, planetSize, planetSize);
        // Small, static accent in the open sky between the title and Mars.
        float hole = blackHoleArt.width;
        Place(blackHole.rectTransform, size, narrow ? 380 : 560, narrow ? 220 : 10, hole, hole);
        orbitWidth = radius + 32;
        foreach (var arc in new[] { rearArc, frontArc })
            arc.Set(size, center, orbitWidth);
        CoverSky();
        DrawMotion();
    }

    private void DrawMotion()
    {
        float tau = 2 * Mathf.PI;
        float angle = Mathf.PI + tau * (reduced ? .17f : motionTime / OrbitSeconds + .02f);
        Vector2 point = OrbitArc.Point(center, orbitWidth, angle);
        // Whole design pixels keep the point-filtered relay from shimmering as it moves.
        float w = satellite.texture.width, h = satellite.texture.height;
        Place(satellite.rectTransform, size, Mathf.Round(point.x - w / 2), Mathf.Round(point.y - h / 2), w, h);

        // The near pass is the lower half of the ellipse. Depth switches at the ends, clear of Mars.
        bool near = Mathf.Sin(angle) < 0;
        int relay = satellite.transform.GetSiblingIndex(), behind = planet.transform.GetSiblingIndex();
        if (near && relay < behind)
            satellite.transform.SetSiblingIndex(frontArc.transform.GetSiblingIndex());
        else if (!near && relay > behind)
            satellite.transform.SetSiblingIndex(behind);

        float pulse = reduced ? 0 : .65f * Mathf.Pow(.5f - .5f * Mathf.Cos(motionTime * tau / 6), 4);
        var glow = new Color(1, .88f, .70f, pulse);
        star.color = glow;
        star.transform.GetChild(0).GetComponent<Image>().color = glow;
    }

    // Centered, aspect-preserving crop that covers the whole viewport.
    private void CoverSky()
    {
        float view = (float)Screen.width / Screen.height, art = (float)sky.width / sky.height;
        Vector2 crop = view > art ? new Vector2(1, art / view) : new Vector2(view / art, 1);
        stars.uvRect = new Rect((Vector2.one - crop) / 2, crop);
    }

    private void ShowHelp(bool visible)
    {
        start.interactable = !visible;
        help.interactable = !visible;
        helpPanel.gameObject.SetActive(visible);
        if (visible)
            motion.Select();
        else
            help.Select();
    }

    private void ToggleMotion()
    {
        reduced = !reduced;
        PlayerPrefs.SetInt(MotionPreference, reduced ? 1 : 0);
        PlayerPrefs.Save();
        UpdateMotionLabel();
        DrawMotion();
    }

    private void UpdateMotionLabel()
    {
        motion.GetComponentInChildren<Text>().text = reduced ? "Reduce motion  [On]" : "Reduce motion  [Off]";
    }

    private void Launch()
    {
        if (launching)
            return;

        launching = true;
        start.interactable = false;
        help.interactable = false;
        start.face.color = start.down;
        fade.raycastTarget = true;
        if (reduced)
            SceneManager.LoadSceneAsync(MissionScene);
        else
            StartCoroutine(FadeToMission());
    }

    private IEnumerator FadeToMission()
    {
        for (float elapsed = 0; elapsed < .2f; elapsed += Time.unscaledDeltaTime)
        {
            fade.color = new Color32(Background.r, Background.g, Background.b, (byte)(255 * elapsed / .2f));
            yield return null;
        }
        fade.color = Background;
        SceneManager.LoadSceneAsync(MissionScene);
    }

    private MenuButton MenuAction(string caption, int fontSize, bool primary)
    {
        var button = Action<MenuButton>(caption, layout, fontSize, primary);
        var face = button.transform.Find("Face").GetComponent<Image>();
        face.GetComponent<Outline>().enabled = false;
        face.color = Coral;
        button.face = face;
        button.idle = Coral;
        button.down = new Color32(178, 76, 78, 255);
        if (!primary)
        {
            // Charcoal fill inside a 3 px coral outline.
            var inset = Fill("Inset", face.transform, Background);
            inset.rectTransform.offsetMin = new Vector2(3, 3);
            inset.rectTransform.offsetMax = new Vector2(-3, -3);
            button.face = inset;
            button.idle = Background;
            button.down = new Color32(86, 44, 52, 255);
        }
        button.face.color = button.idle;
        return button;
    }

    private OrbitArc Arc(string name, bool front)
    {
        var arc = Box(name, layout).gameObject.AddComponent<OrbitArc>();
        arc.front = front;
        arc.color = front ? ArcColor : new Color32(ArcColor.r, ArcColor.g, ArcColor.b, 110);
        arc.raycastTarget = false;
        return arc;
    }

    // Binary alpha so the scaled-down relay has no soft fringe over the sky or Mars.
    private static Texture2D Crisp(Texture2D source)
    {
        var pixels = source.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
            pixels[i].a = (byte)(pixels[i].a < 128 ? 0 : 255);
        var crisp = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false)
            { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        crisp.SetPixels32(pixels);
        crisp.Apply(false, true);
        return crisp;
    }

    private static void SetInPanel(RectTransform rect, float bottom, float top)
    {
        rect.anchorMin = new Vector2(.08f, bottom);
        rect.anchorMax = new Vector2(.92f, top);
    }

    private sealed class MenuButton : Button
    {
        public Image face;
        public Color idle, down;

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            if (face)
                face.color = state == SelectionState.Pressed ? down : idle;
        }
    }

    // Half of the shared orbit ellipse, drawn in design pixels from the top left.
    [RequireComponent(typeof(CanvasRenderer))]
    private sealed class OrbitArc : MaskableGraphic
    {
        public bool front;
        private Vector2 layoutSize, center;
        private float width;

        public static Vector2 Point(Vector2 center, float width, float angle)
        {
            float x = width * Mathf.Cos(angle), y = -width * Flatten * Mathf.Sin(angle);
            return center + new Vector2(x * Mathf.Cos(Tilt) + y * Mathf.Sin(Tilt),
                -x * Mathf.Sin(Tilt) + y * Mathf.Cos(Tilt));
        }

        public void Set(Vector2 layoutSize, Vector2 center, float width)
        {
            this.layoutSize = layoutSize;
            this.center = center;
            this.width = width;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (width <= 0)
                return;

            const int steps = 96;
            Rect rect = rectTransform.rect;
            Vector2 Local(Vector2 p) => new Vector2(rect.xMin + p.x / layoutSize.x * rect.width,
                rect.yMax - p.y / layoutSize.y * rect.height);

            float start = front ? Mathf.PI : 0, half = front ? 1.5f : 1;
            for (int i = 0; i <= steps; i++)
            {
                Vector2 a = Point(center, width, start + Mathf.PI * i / steps);
                Vector2 b = Point(center, width, start + Mathf.PI * (i + 1) / steps);
                Vector2 normal = new Vector2(b.y - a.y, a.x - b.x).normalized * half;
                mesh.AddVert(Local(a - normal), color, Vector2.zero);
                mesh.AddVert(Local(a + normal), color, Vector2.zero);
                if (i > 0)
                {
                    int v = i * 2;
                    mesh.AddTriangle(v - 2, v - 1, v + 1);
                    mesh.AddTriangle(v - 2, v + 1, v);
                }
            }
        }
    }
}
