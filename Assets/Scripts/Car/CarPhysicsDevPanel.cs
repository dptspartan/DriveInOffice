using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Press M: tune all car/camera/assist gameplay settings.
/// Save writes per-preset .txt under persistentDataPath/CarTunes + PlayerPrefs.
/// </summary>
[DisallowMultipleComponent]
public class CarPhysicsDevPanel : MonoBehaviour
{
    private enum Tab { Drive, Handling, BodyCam }

    private struct FloatField
    {
        public string Label;
        public Tab Tab;
        public float Min;
        public float Max;
        public int Decimals;
        public Func<float> Get;
        public Action<float> Set;

        public FloatField(string label, Tab tab, float min, float max, Func<float> get, Action<float> set, int decimals = 2)
        {
            Label = label;
            Tab = tab;
            Min = min;
            Max = max;
            Get = get;
            Set = set;
            Decimals = decimals;
        }
    }

    private struct FieldWidgets
    {
        public Slider Slider;
        public InputField Input;
        public FloatField Field;
    }

    [SerializeField] KenneyCarController targetCar;
    [SerializeField] CarFollowCamera targetCamera;
    [SerializeField] bool pauseCarInputWhenOpen = true;
    [SerializeField] bool showOnStart;
    [SerializeField] bool loadSavedOnStart = true;

    private CarPhysicsSettings working;
    private CarPhysicsTier tierComponent;
    private CarKeyboardDrivingAssist keyboardAssist;
    private CarTier selectedTier = CarTier.Commuter;
    private Tab activeTab = Tab.Drive;

    private GameObject panelRoot;
    private Dropdown presetDropdown;
    private ScrollRect scrollRect;
    private RectTransform driveContent;
    private RectTransform handlingContent;
    private RectTransform bodyContent;
    private Button driveTabButton;
    private Button handlingTabButton;
    private Button bodyTabButton;
    private Toggle assistToggle;
    private Text statusText;

    private readonly List<FloatField> fields = new List<FloatField>();
    private readonly List<FieldWidgets> widgets = new List<FieldWidgets>();
    private bool uiBuilt;
    private bool isOpen;
    private bool updatingUi;
    private CursorLockMode previousLockMode;
    private bool previousCursorVisible;

    private void Awake()
    {
        ResolveTargets();
    }

    private void Start()
    {
        BuildFieldDefinitions();
        BuildUi();

        if (loadSavedOnStart && PlayerPrefs.HasKey(CarTuneStore.LastPresetKey))
            selectedTier = (CarTier)PlayerPrefs.GetInt(CarTuneStore.LastPresetKey, (int)CarTier.Commuter);

        LoadSelectedPreset();
        RefreshPresetDropdown();
        RefreshAssistToggle();
        RefreshAllFields();
        SetPanelOpen(showOnStart);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
            SetPanelOpen(!isOpen);
    }

    private void OnDestroy()
    {
        if (targetCar != null && pauseCarInputWhenOpen)
            targetCar.DevInputBlocked = false;
    }

    private void ResolveTargets()
    {
        if (targetCar == null)
            targetCar = FindAnyObjectByType<KenneyCarController>();
        if (targetCar == null)
            return;

        tierComponent = targetCar.GetComponent<CarPhysicsTier>();
        keyboardAssist = targetCar.GetComponent<CarKeyboardDrivingAssist>();
        if (keyboardAssist == null)
            keyboardAssist = targetCar.gameObject.AddComponent<CarKeyboardDrivingAssist>();

        if (targetCamera == null)
            targetCamera = targetCar.GetComponentInChildren<CarFollowCamera>(true);
        if (targetCamera == null)
            targetCamera = FindAnyObjectByType<CarFollowCamera>();
    }

    private void BuildFieldDefinitions()
    {
        fields.Clear();
        fields.Add(new FloatField("Motor Power", Tab.Drive, 400f, 3500f, () => working.motorPower, v => working.motorPower = v, 0));
        fields.Add(new FloatField("Max Speed (m/s)", Tab.Drive, 8f, 35f, () => working.maxSpeed, v => working.maxSpeed = v, 1));
        fields.Add(new FloatField("Reverse Power", Tab.Drive, 0.2f, 0.8f, () => working.reversePower, v => working.reversePower = v));
        fields.Add(new FloatField("Brake Force", Tab.Drive, 800f, 6000f, () => working.brakeForce, v => working.brakeForce = v, 0));
        fields.Add(new FloatField("Handbrake Force", Tab.Drive, 1000f, 7000f, () => working.handbrakeForce, v => working.handbrakeForce = v, 0));
        fields.Add(new FloatField("Coast Brake", Tab.Drive, 200f, 3000f, () => working.coastBrake, v => working.coastBrake = v, 0));
        fields.Add(new FloatField("Impact Stop (s)", Tab.Drive, 0.2f, 3f, () => working.impactStopSeconds, v => working.impactStopSeconds = v));
        fields.Add(new FloatField("Impact Brake Force", Tab.Drive, 2000f, 15000f, () => working.impactBrakeForce, v => working.impactBrakeForce = v, 0));
        fields.Add(new FloatField("Keyboard Assist Strength", Tab.Drive, 0f, 1f,
            () => keyboardAssist != null ? keyboardAssist.AssistStrength : 0.4f,
            v => { if (keyboardAssist != null) keyboardAssist.AssistStrength = v; }));

        fields.Add(new FloatField("Max Steer Angle", Tab.Handling, 5f, 40f, () => working.maxSteerAngle, v => working.maxSteerAngle = v, 1));
        fields.Add(new FloatField("Min Steer Angle", Tab.Handling, 3f, 25f, () => working.minSteerAngle, v => working.minSteerAngle = v, 1));
        fields.Add(new FloatField("Steer Ramp In", Tab.Handling, 0.5f, 8f, () => working.steerRampIn, v => working.steerRampIn = v));
        fields.Add(new FloatField("Steer Ramp Out", Tab.Handling, 0.5f, 8f, () => working.steerRampOut, v => working.steerRampOut = v));
        fields.Add(new FloatField("High-Speed Steer Rate", Tab.Handling, 0.2f, 1f, () => working.steerHighSpeedRate, v => working.steerHighSpeedRate = v));
        fields.Add(new FloatField("Steer Speed Falloff", Tab.Handling, 0.8f, 2.5f, () => working.steerSpeedFalloff, v => working.steerSpeedFalloff = v));
        fields.Add(new FloatField("Keyboard Steer Scale", Tab.Handling, 0.5f, 1f, () => working.keyboardSteerScale, v => working.keyboardSteerScale = v));
        fields.Add(new FloatField("Front Grip", Tab.Handling, 0.5f, 1.5f, () => working.frontGrip, v => working.frontGrip = v));
        fields.Add(new FloatField("Rear Grip", Tab.Handling, 0.5f, 1.5f, () => working.rearGrip, v => working.rearGrip = v));
        fields.Add(new FloatField("Handbrake Rear Grip", Tab.Handling, 0.15f, 1f, () => working.handbrakeRearGrip, v => working.handbrakeRearGrip = v));
        fields.Add(new FloatField("Downforce", Tab.Handling, 0f, 50f, () => working.downforce, v => working.downforce = v, 0));
        fields.Add(new FloatField("Handbrake Yaw", Tab.Handling, 50f, 800f, () => working.handbrakeYaw, v => working.handbrakeYaw = v, 0));
        fields.Add(new FloatField("Drift Align Strength", Tab.Handling, 5f, 40f, () => working.driftAlignStrength, v => working.driftAlignStrength = v, 0));
        fields.Add(new FloatField("Max Yaw Rate", Tab.Handling, 0.5f, 4f, () => working.maxYawRate, v => working.maxYawRate = v));
        fields.Add(new FloatField("Drift Angle Threshold", Tab.Handling, 3f, 20f, () => working.driftAngleThreshold, v => working.driftAngleThreshold = v, 1));
        fields.Add(new FloatField("Skid Slip Reference", Tab.Handling, 0.2f, 1f, () => working.skidSlipReference, v => working.skidSlipReference = v));

        fields.Add(new FloatField("Mass", Tab.BodyCam, 600f, 2000f, () => working.mass, v => working.mass = v, 0));
        fields.Add(new FloatField("COM X", Tab.BodyCam, -0.5f, 0.5f, () => working.centerOfMass.x, v => working.centerOfMass = new Vector3(v, working.centerOfMass.y, working.centerOfMass.z)));
        fields.Add(new FloatField("COM Y", Tab.BodyCam, 0f, 0.6f, () => working.centerOfMass.y, v => working.centerOfMass = new Vector3(working.centerOfMass.x, v, working.centerOfMass.z)));
        fields.Add(new FloatField("COM Z", Tab.BodyCam, -0.4f, 0.4f, () => working.centerOfMass.z, v => working.centerOfMass = new Vector3(working.centerOfMass.x, working.centerOfMass.y, v)));
        fields.Add(new FloatField("Roll Stability", Tab.BodyCam, 500f, 5000f, () => working.rollStability, v => working.rollStability = v, 0));
        fields.Add(new FloatField("Pitch Stability", Tab.BodyCam, 500f, 5000f, () => working.pitchStability, v => working.pitchStability = v, 0));
        fields.Add(new FloatField("Cam Follow Smooth", Tab.BodyCam, 0.02f, 0.5f, () => targetCamera != null ? targetCamera.followSmooth : 0.14f, v => { if (targetCamera != null) targetCamera.followSmooth = v; }));
        fields.Add(new FloatField("Cam Rotation Sharpness", Tab.BodyCam, 1f, 20f, () => targetCamera != null ? targetCamera.rotationSharpness : 8f, v => { if (targetCamera != null) targetCamera.rotationSharpness = v; }, 1));
        fields.Add(new FloatField("Cam Drift Lateral", Tab.BodyCam, 0f, 2f, () => targetCamera != null ? targetCamera.driftLateral : 0.55f, v => { if (targetCamera != null) targetCamera.driftLateral = v; }));
        fields.Add(new FloatField("Cam Speed Pull Back", Tab.BodyCam, 0f, 2f, () => targetCamera != null ? targetCamera.speedPullBack : 0.65f, v => { if (targetCamera != null) targetCamera.speedPullBack = v; }));
        fields.Add(new FloatField("Cam Look Ahead", Tab.BodyCam, 0f, 0.3f, () => targetCamera != null ? targetCamera.lookAhead : 0.06f, v => { if (targetCamera != null) targetCamera.lookAhead = v; }));
        fields.Add(new FloatField("Cam Look Height", Tab.BodyCam, 0.1f, 2f, () => targetCamera != null ? targetCamera.lookHeight : 0.55f, v => { if (targetCamera != null) targetCamera.lookHeight = v; }));
        fields.Add(new FloatField("Cam Min FOV", Tab.BodyCam, 40f, 70f, () => targetCamera != null ? targetCamera.minFov : 55f, v => { if (targetCamera != null) targetCamera.minFov = v; }, 0));
        fields.Add(new FloatField("Cam Max FOV", Tab.BodyCam, 45f, 85f, () => targetCamera != null ? targetCamera.maxFov : 62f, v => { if (targetCamera != null) targetCamera.maxFov = v; }, 0));
    }

    private void LoadSelectedPreset()
    {
        working = CarTuneStore.Load(selectedTier,
            out float camFollow, out float camRot, out float camDrift, out float camPull,
            out float camLookAhead, out float camLookHeight, out float camMinFov, out float camMaxFov,
            out bool assistOn, out float assistStrength);

        if (targetCar != null)
            targetCar.ApplySettings(working);
        if (tierComponent != null)
            tierComponent.tier = selectedTier;

        if (targetCamera != null)
        {
            targetCamera.followSmooth = camFollow;
            targetCamera.rotationSharpness = camRot;
            targetCamera.driftLateral = camDrift;
            targetCamera.speedPullBack = camPull;
            targetCamera.lookAhead = camLookAhead;
            targetCamera.lookHeight = camLookHeight;
            targetCamera.minFov = camMinFov;
            targetCamera.maxFov = camMaxFov;
        }

        if (keyboardAssist != null)
        {
            keyboardAssist.AssistEnabled = assistOn;
            keyboardAssist.AssistStrength = assistStrength;
        }
    }

    private void ApplyWorking()
    {
        if (targetCar != null)
            targetCar.ApplySettings(working);
        if (tierComponent != null)
            tierComponent.tier = selectedTier;
    }

    private void SetPanelOpen(bool open)
    {
        isOpen = open;
        if (panelRoot != null)
            panelRoot.SetActive(open);

        if (pauseCarInputWhenOpen && targetCar != null)
            targetCar.DevInputBlocked = open;

        if (open)
        {
            previousLockMode = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (targetCar != null)
                working = targetCar.physics.Clone();
            RefreshAssistToggle();
            RefreshAllFields();
            SetStatus("Live edit — Save Preset writes .txt + local storage");
        }
        else
        {
            Cursor.lockState = previousLockMode;
            Cursor.visible = previousCursorVisible;
        }
    }

    private void OnPresetChanged(int index)
    {
        if (updatingUi)
            return;
        selectedTier = (CarTier)index;
        LoadSelectedPreset();
        RefreshAssistToggle();
        RefreshAllFields();
        SetStatus("Loaded " + selectedTier);
    }

    private void ResetToBuiltin()
    {
        working = CarPhysicsSettings.GetPreset(selectedTier).Clone();
        ApplyWorking();
        RefreshAllFields();
        SetStatus("Reset to builtin " + selectedTier);
    }

    private void SaveCurrentPreset()
    {
        CarTuneStore.Save(selectedTier, working, targetCamera, keyboardAssist);
        SetStatus("Saved " + selectedTier + " → " + CarTuneStore.FilePath(selectedTier));
        Debug.Log("[CarTune] " + CarTuneStore.FilePath(selectedTier));
    }

    private void SaveAllPresets()
    {
        CarTuneStore.Save(selectedTier, working, targetCamera, keyboardAssist);
        foreach (CarTier tier in Enum.GetValues(typeof(CarTier)))
        {
            if (tier == selectedTier)
                continue;
            CarPhysicsSettings s = CarTuneStore.Load(tier,
                out _, out _, out _, out _, out _, out _, out _, out _, out bool aOn, out float aStr);
            var tempAssist = keyboardAssist;
            if (tempAssist != null)
            {
                bool prevOn = tempAssist.AssistEnabled;
                float prevStr = tempAssist.AssistStrength;
                tempAssist.AssistEnabled = aOn;
                tempAssist.AssistStrength = aStr;
                CarTuneStore.Save(tier, s, targetCamera, tempAssist);
                tempAssist.AssistEnabled = prevOn;
                tempAssist.AssistStrength = prevStr;
            }
            else
            {
                CarTuneStore.Save(tier, s, targetCamera, null);
            }
        }
        SetStatus("Saved all presets under " + CarTuneStore.TunesFolder);
    }

    private void RefreshPresetDropdown()
    {
        if (presetDropdown == null)
            return;
        updatingUi = true;
        presetDropdown.value = (int)selectedTier;
        presetDropdown.RefreshShownValue();
        updatingUi = false;
    }

    private void RefreshAssistToggle()
    {
        if (assistToggle == null || keyboardAssist == null)
            return;
        updatingUi = true;
        assistToggle.isOn = keyboardAssist.AssistEnabled;
        updatingUi = false;
    }

    private void OnAssistToggleChanged(bool enabled)
    {
        if (updatingUi || keyboardAssist == null)
            return;
        keyboardAssist.AssistEnabled = enabled;
    }

    private void RefreshAllFields()
    {
        if (working == null)
            return;
        updatingUi = true;
        for (int i = 0; i < widgets.Count; i++)
        {
            FieldWidgets w = widgets[i];
            float value = Mathf.Clamp(w.Field.Get(), w.Field.Min, w.Field.Max);
            w.Slider.SetValueWithoutNotify(value);
            w.Input.SetTextWithoutNotify(FormatValue(value, w.Field.Decimals));
        }
        updatingUi = false;
    }

    private void OnSlider(FieldWidgets w, float value)
    {
        if (updatingUi)
            return;
        value = Mathf.Clamp(value, w.Field.Min, w.Field.Max);
        w.Field.Set(value);
        updatingUi = true;
        w.Input.SetTextWithoutNotify(FormatValue(value, w.Field.Decimals));
        updatingUi = false;
        ApplyWorking();
    }

    private void OnInput(FieldWidgets w, string text)
    {
        if (updatingUi)
            return;
        if (!float.TryParse(text, out float value))
        {
            RefreshAllFields();
            return;
        }
        value = Mathf.Clamp(value, w.Field.Min, w.Field.Max);
        w.Field.Set(value);
        updatingUi = true;
        w.Slider.SetValueWithoutNotify(value);
        w.Input.SetTextWithoutNotify(FormatValue(value, w.Field.Decimals));
        updatingUi = false;
        ApplyWorking();
    }

    private void SetActiveTab(Tab tab)
    {
        activeTab = tab;
        driveContent.gameObject.SetActive(tab == Tab.Drive);
        handlingContent.gameObject.SetActive(tab == Tab.Handling);
        bodyContent.gameObject.SetActive(tab == Tab.BodyCam);
        StyleTab(driveTabButton, tab == Tab.Drive);
        StyleTab(handlingTabButton, tab == Tab.Handling);
        StyleTab(bodyTabButton, tab == Tab.BodyCam);
        scrollRect.content = tab == Tab.Drive ? driveContent : tab == Tab.Handling ? handlingContent : bodyContent;
        scrollRect.verticalNormalizedPosition = 1f;
    }

    private static void StyleTab(Button button, bool on)
    {
        if (button == null)
            return;
        ColorBlock c = button.colors;
        c.normalColor = on ? new Color(0.22f, 0.45f, 0.72f) : new Color(0.18f, 0.18f, 0.2f);
        button.colors = c;
    }

    private void SetStatus(string msg)
    {
        if (statusText != null)
            statusText.text = msg;
    }

    private static string FormatValue(float value, int decimals)
    {
        if (decimals <= 0)
            return Mathf.RoundToInt(value).ToString();
        return value.ToString("F" + decimals);
    }

    private void BuildUi()
    {
        if (uiBuilt)
            return;
        EnsureEventSystem();

        GameObject canvasGo = Create("CarPhysicsDevCanvas", transform);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        canvasGo.AddComponent<GraphicRaycaster>();

        panelRoot = Create("Panel", canvasGo.transform);
        panelRoot.AddComponent<Image>().color = new Color(0.08f, 0.09f, 0.11f, 0.94f);
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(580f, 700f);

        VerticalLayoutGroup panelLayout = panelRoot.AddComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(14, 14, 14, 14);
        panelLayout.spacing = 8f;
        panelLayout.childControlHeight = true;
        panelLayout.childControlWidth = true;
        panelLayout.childForceExpandHeight = false;
        panelLayout.childForceExpandWidth = true;

        CreateHeader(panelRoot.transform);
        presetDropdown = CreatePresetDropdown(panelRoot.transform);
        presetDropdown.onValueChanged.AddListener(OnPresetChanged);
        CreateAssistRow(panelRoot.transform);
        CreateTabBar(panelRoot.transform);
        scrollRect = CreateScroll(panelRoot.transform, out driveContent, out handlingContent, out bodyContent);
        CreateFieldRows();
        CreateFooter(panelRoot.transform);

        SetActiveTab(Tab.Drive);
        uiBuilt = true;
        panelRoot.SetActive(false);
    }

    private void CreateHeader(Transform parent)
    {
        GameObject header = Create("Header", parent);
        header.AddComponent<LayoutElement>().preferredHeight = 42f;
        Text title = AddText(header.transform, "Car Physics Tuner (Dev)", 20, FontStyle.Bold);
        Stretch(title.rectTransform);
        Text hint = AddText(header.transform, "M toggle", 12, FontStyle.Italic);
        hint.color = new Color(0.75f, 0.78f, 0.82f);
        hint.alignment = TextAnchor.LowerRight;
        RectTransform hr = hint.rectTransform;
        hr.anchorMin = new Vector2(0f, 0f);
        hr.anchorMax = new Vector2(1f, 0f);
        hr.pivot = new Vector2(1f, 0f);
        hr.sizeDelta = new Vector2(0f, 16f);
    }

    private void CreateAssistRow(Transform parent)
    {
        GameObject row = Create("AssistRow", parent);
        row.AddComponent<LayoutElement>().preferredHeight = 30f;

        Text label = AddText(row.transform, "Keyboard Assist", 14, FontStyle.Normal);
        RectTransform lr = label.rectTransform;
        lr.anchorMin = new Vector2(0f, 0.5f);
        lr.anchorMax = new Vector2(0f, 0.5f);
        lr.pivot = new Vector2(0f, 0.5f);
        lr.anchoredPosition = Vector2.zero;
        lr.sizeDelta = new Vector2(160f, 28f);

        GameObject toggleGo = Create("AssistToggle", row.transform);
        RectTransform tr = toggleGo.GetComponent<RectTransform>();
        tr.anchorMin = tr.anchorMax = tr.pivot = new Vector2(1f, 0.5f);
        tr.sizeDelta = new Vector2(28f, 28f);

        Toggle toggle = toggleGo.AddComponent<Toggle>();
        Image bg = Create("Background", toggleGo.transform).AddComponent<Image>();
        bg.color = new Color(0.16f, 0.17f, 0.2f);
        Stretch(bg.rectTransform);
        toggle.targetGraphic = bg;

        Image check = Create("Checkmark", toggleGo.transform).AddComponent<Image>();
        check.color = new Color(0.35f, 0.75f, 0.45f);
        RectTransform cr = check.rectTransform;
        cr.anchorMin = cr.anchorMax = cr.pivot = new Vector2(0.5f, 0.5f);
        cr.sizeDelta = new Vector2(16f, 16f);
        toggle.graphic = check;
        assistToggle = toggle;
        assistToggle.onValueChanged.AddListener(OnAssistToggleChanged);
    }

    private Dropdown CreatePresetDropdown(Transform parent)
    {
        GameObject row = Create("PresetRow", parent);
        row.AddComponent<LayoutElement>().preferredHeight = 34f;
        HorizontalLayoutGroup h = row.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 10f;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;

        Text label = AddText(row.transform, "Preset", 14, FontStyle.Normal);
        label.alignment = TextAnchor.MiddleLeft;
        LayoutElement ll = label.gameObject.AddComponent<LayoutElement>();
        ll.preferredWidth = 56f;
        ll.flexibleWidth = 0f;

        GameObject ddGo = Create("Dropdown", row.transform);
        LayoutElement dl = ddGo.AddComponent<LayoutElement>();
        dl.preferredWidth = 170f;
        dl.preferredHeight = 30f;
        dl.flexibleWidth = 0f;
        ddGo.AddComponent<Image>().color = new Color(0.18f, 0.22f, 0.28f);
        Dropdown dropdown = ddGo.AddComponent<Dropdown>();

        Text caption = AddText(ddGo.transform, "Commuter", 13, FontStyle.Normal);
        caption.alignment = TextAnchor.MiddleLeft;
        RectTransform cr = caption.rectTransform;
        Stretch(cr);
        cr.offsetMin = new Vector2(10f, 2f);
        cr.offsetMax = new Vector2(-28f, -2f);
        dropdown.captionText = caption;

        Text arrow = AddText(ddGo.transform, "▼", 10, FontStyle.Normal);
        arrow.alignment = TextAnchor.MiddleCenter;
        RectTransform ar = arrow.rectTransform;
        ar.anchorMin = ar.anchorMax = ar.pivot = new Vector2(1f, 0.5f);
        ar.anchoredPosition = new Vector2(-4f, 0f);
        ar.sizeDelta = new Vector2(22f, 22f);

        const float itemH = 28f;
        GameObject template = Create("Template", ddGo.transform);
        template.SetActive(false);
        RectTransform tr = template.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0f, 0f);
        tr.anchorMax = new Vector2(1f, 0f);
        tr.pivot = new Vector2(0.5f, 1f);
        tr.anchoredPosition = new Vector2(0f, 2f);
        tr.sizeDelta = new Vector2(0f, itemH * 4f + 8f);
        template.AddComponent<Image>().color = new Color(0.12f, 0.13f, 0.16f);
        ScrollRect tscroll = template.AddComponent<ScrollRect>();
        tscroll.horizontal = false;
        tscroll.movementType = ScrollRect.MovementType.Clamped;
        tscroll.scrollSensitivity = 12f;

        GameObject viewport = Create("Viewport", template.transform);
        RectTransform vr = viewport.GetComponent<RectTransform>();
        Stretch(vr);
        vr.offsetMin = new Vector2(2f, 2f);
        vr.offsetMax = new Vector2(-2f, -2f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        viewport.AddComponent<Image>().color = Color.white;
        tscroll.viewport = vr;

        GameObject content = Create("Content", viewport.transform);
        RectTransform contentR = content.GetComponent<RectTransform>();
        contentR.anchorMin = new Vector2(0f, 1f);
        contentR.anchorMax = new Vector2(1f, 1f);
        contentR.pivot = new Vector2(0.5f, 1f);
        contentR.sizeDelta = new Vector2(0f, itemH);
        VerticalLayoutGroup cl = content.AddComponent<VerticalLayoutGroup>();
        cl.childControlHeight = true;
        cl.childControlWidth = true;
        cl.childForceExpandHeight = false;
        cl.childForceExpandWidth = true;
        cl.spacing = 0f;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        tscroll.content = contentR;

        GameObject item = Create("Item", content.transform);
        LayoutElement ile = item.AddComponent<LayoutElement>();
        ile.preferredHeight = itemH;
        ile.minHeight = itemH;
        ile.flexibleHeight = 0f;
        Toggle toggle = item.AddComponent<Toggle>();
        toggle.toggleTransition = Toggle.ToggleTransition.None;

        Image ibg = Create("Item Background", item.transform).AddComponent<Image>();
        ibg.color = new Color(0.18f, 0.19f, 0.22f);
        Stretch(ibg.rectTransform);
        toggle.targetGraphic = ibg;

        Image cig = Create("Item Checkmark", item.transform).AddComponent<Image>();
        cig.color = new Color(0.28f, 0.48f, 0.78f, 0.85f);
        Stretch(cig.rectTransform);
        toggle.graphic = cig;

        Text itemLabel = AddText(item.transform, "Option", 13, FontStyle.Normal);
        itemLabel.alignment = TextAnchor.MiddleLeft;
        itemLabel.raycastTarget = false;
        RectTransform ilr = itemLabel.rectTransform;
        Stretch(ilr);
        ilr.offsetMin = new Vector2(10f, 0f);
        ilr.offsetMax = new Vector2(-4f, 0f);

        dropdown.template = tr;
        dropdown.itemText = itemLabel;
        dropdown.options.Clear();
        foreach (CarTier tier in Enum.GetValues(typeof(CarTier)))
            dropdown.options.Add(new Dropdown.OptionData(tier.ToString()));
        return dropdown;
    }

    private void CreateTabBar(Transform parent)
    {
        GameObject bar = Create("TabBar", parent);
        bar.AddComponent<LayoutElement>().preferredHeight = 32f;
        HorizontalLayoutGroup h = bar.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 6f;
        h.childForceExpandWidth = true;
        h.childControlWidth = true;
        h.childControlHeight = true;
        driveTabButton = MakeTab(bar.transform, "Drive", () => SetActiveTab(Tab.Drive));
        handlingTabButton = MakeTab(bar.transform, "Handling", () => SetActiveTab(Tab.Handling));
        bodyTabButton = MakeTab(bar.transform, "Body / Cam", () => SetActiveTab(Tab.BodyCam));
    }

    private Button MakeTab(Transform parent, string label, Action click)
    {
        GameObject go = Create(label, parent);
        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.18f, 0.2f);
        Button b = go.AddComponent<Button>();
        b.targetGraphic = bg;
        b.onClick.AddListener(() => click());
        Text t = AddText(go.transform, label, 13, FontStyle.Bold);
        t.alignment = TextAnchor.MiddleCenter;
        Stretch(t.rectTransform);
        return b;
    }

    private ScrollRect CreateScroll(Transform parent, out RectTransform drive, out RectTransform handling, out RectTransform body)
    {
        GameObject scrollGo = Create("Scroll", parent);
        LayoutElement le = scrollGo.AddComponent<LayoutElement>();
        le.flexibleHeight = 1f;
        le.minHeight = 380f;
        scrollGo.AddComponent<Image>().color = new Color(0.11f, 0.12f, 0.14f, 0.9f);
        ScrollRect scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 8f;
        scroll.inertia = true;
        scroll.decelerationRate = 0.2f;

        GameObject viewport = Create("Viewport", scrollGo.transform);
        RectTransform vr = viewport.GetComponent<RectTransform>();
        Stretch(vr);
        vr.offsetMin = new Vector2(4f, 4f);
        vr.offsetMax = new Vector2(-4f, -4f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        viewport.AddComponent<Image>().color = Color.white;
        scroll.viewport = vr;

        drive = MakeContent(viewport.transform, "DriveContent");
        handling = MakeContent(viewport.transform, "HandlingContent");
        body = MakeContent(viewport.transform, "BodyContent");
        handling.gameObject.SetActive(false);
        body.gameObject.SetActive(false);
        scroll.content = drive;
        return scroll;
    }

    private RectTransform MakeContent(Transform parent, string name)
    {
        GameObject go = Create(name, parent);
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0f, 1f);
        r.anchorMax = new Vector2(1f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.sizeDelta = Vector2.zero;
        VerticalLayoutGroup v = go.AddComponent<VerticalLayoutGroup>();
        v.spacing = 6f;
        v.padding = new RectOffset(4, 4, 4, 4);
        v.childControlHeight = true;
        v.childControlWidth = true;
        v.childForceExpandHeight = false;
        v.childForceExpandWidth = true;
        go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return r;
    }

    private void CreateFieldRows()
    {
        widgets.Clear();
        for (int i = 0; i < fields.Count; i++)
        {
            FloatField f = fields[i];
            Transform parent = f.Tab == Tab.Drive ? driveContent : f.Tab == Tab.Handling ? handlingContent : bodyContent;
            widgets.Add(CreateFieldRow(parent, f));
        }
    }

    private FieldWidgets CreateFieldRow(Transform parent, FloatField field)
    {
        const float valueW = 58f;
        GameObject row = Create(field.Label, parent);
        row.AddComponent<LayoutElement>().preferredHeight = 50f;
        VerticalLayoutGroup vg = row.AddComponent<VerticalLayoutGroup>();
        vg.spacing = 2f;
        vg.childControlHeight = true;
        vg.childControlWidth = true;
        vg.childForceExpandHeight = false;
        vg.childForceExpandWidth = true;

        Text label = AddText(row.transform, field.Label, 12, FontStyle.Normal);
        label.color = new Color(0.82f, 0.85f, 0.9f);
        label.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;

        GameObject controls = Create("Controls", row.transform);
        controls.AddComponent<LayoutElement>().preferredHeight = 24f;

        GameObject sliderGo = Create("Slider", controls.transform);
        RectTransform sr = sliderGo.GetComponent<RectTransform>();
        sr.anchorMin = new Vector2(0f, 0.5f);
        sr.anchorMax = new Vector2(1f, 0.5f);
        sr.pivot = new Vector2(0.5f, 0.5f);
        sr.offsetMin = new Vector2(0f, -10f);
        sr.offsetMax = new Vector2(-(valueW + 8f), 10f);

        Slider slider = sliderGo.AddComponent<Slider>();
        slider.minValue = field.Min;
        slider.maxValue = field.Max;
        slider.wholeNumbers = field.Decimals <= 0;

        Image track = Create("Background", sliderGo.transform).AddComponent<Image>();
        track.color = new Color(0.2f, 0.21f, 0.24f);
        RectTransform trackR = track.rectTransform;
        trackR.anchorMin = new Vector2(0f, 0.42f);
        trackR.anchorMax = new Vector2(1f, 0.58f);
        trackR.offsetMin = trackR.offsetMax = Vector2.zero;

        GameObject fillArea = Create("Fill Area", sliderGo.transform);
        RectTransform far = fillArea.GetComponent<RectTransform>();
        far.anchorMin = new Vector2(0f, 0.42f);
        far.anchorMax = new Vector2(1f, 0.58f);
        far.offsetMin = new Vector2(4f, 0f);
        far.offsetMax = new Vector2(-4f, 0f);
        Image fill = Create("Fill", fillArea.transform).AddComponent<Image>();
        fill.color = new Color(0.28f, 0.55f, 0.86f);
        Stretch(fill.rectTransform);

        GameObject handleArea = Create("Handle Slide Area", sliderGo.transform);
        RectTransform har = handleArea.GetComponent<RectTransform>();
        Stretch(har);
        har.offsetMin = new Vector2(4f, 0f);
        har.offsetMax = new Vector2(-4f, 0f);
        Image handle = Create("Handle", handleArea.transform).AddComponent<Image>();
        handle.color = new Color(0.92f, 0.94f, 0.98f);
        RectTransform hr = handle.rectTransform;
        hr.anchorMin = hr.anchorMax = hr.pivot = new Vector2(0.5f, 0.5f);
        hr.sizeDelta = new Vector2(8f, 12f);
        slider.fillRect = fill.rectTransform;
        slider.handleRect = hr;
        slider.targetGraphic = handle;

        GameObject inputGo = Create("Input", controls.transform);
        RectTransform ir = inputGo.GetComponent<RectTransform>();
        ir.anchorMin = ir.anchorMax = ir.pivot = new Vector2(1f, 0.5f);
        ir.sizeDelta = new Vector2(valueW, 22f);
        inputGo.AddComponent<Image>().color = new Color(0.16f, 0.17f, 0.2f);
        InputField input = inputGo.AddComponent<InputField>();
        input.contentType = InputField.ContentType.DecimalNumber;
        Text it = AddText(inputGo.transform, "0", 12, FontStyle.Normal);
        it.alignment = TextAnchor.MiddleCenter;
        Stretch(it.rectTransform);
        it.rectTransform.offsetMin = new Vector2(2f, 0f);
        it.rectTransform.offsetMax = new Vector2(-2f, 0f);
        input.textComponent = it;

        FieldWidgets w = new FieldWidgets { Slider = slider, Input = input, Field = field };
        slider.onValueChanged.AddListener(v => OnSlider(w, v));
        input.onEndEdit.AddListener(t => OnInput(w, t));
        return w;
    }

    private void CreateFooter(Transform parent)
    {
        GameObject footer = Create("Footer", parent);
        footer.AddComponent<LayoutElement>().preferredHeight = 72f;
        VerticalLayoutGroup v = footer.AddComponent<VerticalLayoutGroup>();
        v.spacing = 6f;
        v.childControlHeight = true;
        v.childControlWidth = true;
        v.childForceExpandWidth = true;

        GameObject buttons = Create("Buttons", footer.transform);
        buttons.AddComponent<LayoutElement>().preferredHeight = 32f;
        HorizontalLayoutGroup h = buttons.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 8f;
        h.childForceExpandWidth = true;
        h.childControlWidth = true;
        h.childControlHeight = true;
        MakeAction(buttons.transform, "Save Preset", SaveCurrentPreset);
        MakeAction(buttons.transform, "Save All", SaveAllPresets);
        MakeAction(buttons.transform, "Reset Builtin", ResetToBuiltin);

        statusText = AddText(footer.transform, "Edits apply live. Save writes .txt + PlayerPrefs.", 11, FontStyle.Italic);
        statusText.color = new Color(0.7f, 0.74f, 0.8f);
        statusText.alignment = TextAnchor.MiddleLeft;
        statusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
    }

    private void MakeAction(Transform parent, string label, Action click)
    {
        GameObject go = Create(label, parent);
        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.24f, 0.28f, 0.34f);
        Button b = go.AddComponent<Button>();
        b.targetGraphic = bg;
        b.onClick.AddListener(() => click());
        Text t = AddText(go.transform, label, 12, FontStyle.Bold);
        t.alignment = TextAnchor.MiddleCenter;
        Stretch(t.rectTransform);
    }

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
            return;
        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    private static GameObject Create(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static Text AddText(Transform parent, string content, int size, FontStyle style)
    {
        GameObject go = Create("Text", parent);
        Text t = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.fontStyle = style;
        t.color = Color.white;
        t.text = content;
        t.supportRichText = false;
        t.raycastTarget = false;
        return t;
    }

    private static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }
}
