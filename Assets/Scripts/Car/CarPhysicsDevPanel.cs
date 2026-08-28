using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Runtime dev tuner for the demo scene. Press M to open a scrollable modal with preset
/// dropdown, Normal / Advanced tabs, sliders, and numeric fields. Changes apply live.
/// </summary>
[DisallowMultipleComponent]
public class CarPhysicsDevPanel : MonoBehaviour
{
    private enum SettingsTab
    {
        Normal,
        Advanced
    }

    private struct FloatField
    {
        public string Label;
        public SettingsTab Tab;
        public float Min;
        public float Max;
        public Func<float> Get;
        public Action<float> Set;
        public int Decimals;

        public FloatField(
            string label,
            SettingsTab tab,
            float min,
            float max,
            Func<float> get,
            Action<float> set,
            int decimals = 2)
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

    [SerializeField] KenneyCarController targetCar;
    [SerializeField] bool pauseCarInputWhenOpen = true;
    [SerializeField] bool showOnStart;

    private CarPhysicsTier tierComponent;
    private CarKeyboardDrivingAssist keyboardAssist;
    private CarPhysicsSettings working;
    private CarTier selectedTier;
    private SettingsTab activeTab = SettingsTab.Normal;

    private GameObject panelRoot;
    private Canvas canvas;
    private Dropdown tierDropdown;
    private RectTransform normalTabContent;
    private RectTransform advancedTabContent;
    private Button normalTabButton;
    private Button advancedTabButton;
    private ScrollRect scrollRect;
    private Toggle assistToggle;

    private readonly List<FloatField> fields = new List<FloatField>();
    private readonly List<FieldWidgets> widgets = new List<FieldWidgets>();
    private bool uiBuilt;
    private bool isOpen;
    private bool updatingUi;
    private CursorLockMode previousLockMode;
    private bool previousCursorVisible;

    private struct FieldWidgets
    {
        public SettingsTab Tab;
        public GameObject Row;
        public Slider Slider;
        public InputField Input;
        public FloatField Field;
    }

    private void Awake()
    {
        ResolveTarget();
        BuildFieldDefinitions();
    }

    private void Start()
    {
        BuildUi();
        CaptureCurrentSettings();
        RefreshTierDropdown();
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

    private void ResolveTarget()
    {
        if (targetCar != null)
        {
            tierComponent = targetCar.GetComponent<CarPhysicsTier>();
            keyboardAssist = targetCar.GetComponent<CarKeyboardDrivingAssist>();
            if (keyboardAssist == null)
                keyboardAssist = targetCar.gameObject.AddComponent<CarKeyboardDrivingAssist>();
            return;
        }

        targetCar = FindAnyObjectByType<KenneyCarController>();
        if (targetCar != null)
        {
            tierComponent = targetCar.GetComponent<CarPhysicsTier>();
            keyboardAssist = targetCar.GetComponent<CarKeyboardDrivingAssist>();
            if (keyboardAssist == null)
                keyboardAssist = targetCar.gameObject.AddComponent<CarKeyboardDrivingAssist>();
        }
    }

    private void BuildFieldDefinitions()
    {
        fields.Clear();

        fields.Add(new FloatField("Motor Power", SettingsTab.Normal, 400f, 3000f,
            () => working.motorPower, v => working.motorPower = v, 0));
        fields.Add(new FloatField("Max Speed (m/s)", SettingsTab.Normal, 8f, 30f,
            () => working.maxSpeed, v => working.maxSpeed = v, 1));
        fields.Add(new FloatField("Reverse Power", SettingsTab.Normal, 0.2f, 0.8f,
            () => working.reversePower, v => working.reversePower = v));
        fields.Add(new FloatField("Brake Force", SettingsTab.Normal, 1000f, 5000f,
            () => working.brakeForce, v => working.brakeForce = v, 0));
        fields.Add(new FloatField("Handbrake Force", SettingsTab.Normal, 1500f, 6000f,
            () => working.handbrakeForce, v => working.handbrakeForce = v, 0));
        fields.Add(new FloatField("Coast Brake", SettingsTab.Normal, 300f, 2000f,
            () => working.coastBrake, v => working.coastBrake = v, 0));
        fields.Add(new FloatField("Max Steer Angle", SettingsTab.Normal, 5f, 35f,
            () => working.maxSteerAngle, v => working.maxSteerAngle = v, 1));
        fields.Add(new FloatField("Min Steer Angle", SettingsTab.Normal, 3f, 20f,
            () => working.minSteerAngle, v => working.minSteerAngle = v, 1));
        fields.Add(new FloatField("Steer Ramp In", SettingsTab.Normal, 0.5f, 6f,
            () => working.steerRampIn, v => working.steerRampIn = v));
        fields.Add(new FloatField("Steer Ramp Out", SettingsTab.Normal, 0.5f, 6f,
            () => working.steerRampOut, v => working.steerRampOut = v));
        fields.Add(new FloatField("Front Grip", SettingsTab.Normal, 0.5f, 1.5f,
            () => working.frontGrip, v => working.frontGrip = v));
        fields.Add(new FloatField("Rear Grip", SettingsTab.Normal, 0.5f, 1.5f,
            () => working.rearGrip, v => working.rearGrip = v));
        fields.Add(new FloatField("Handbrake Rear Grip", SettingsTab.Normal, 0.15f, 1f,
            () => working.handbrakeRearGrip, v => working.handbrakeRearGrip = v));
        fields.Add(new FloatField("Keyboard Assist Strength", SettingsTab.Normal, 0f, 1f,
            () => keyboardAssist != null ? keyboardAssist.AssistStrength : 0.4f,
            v => { if (keyboardAssist != null) keyboardAssist.AssistStrength = v; }));

        fields.Add(new FloatField("Steer High-Speed Rate", SettingsTab.Advanced, 0.2f, 1f,
            () => working.steerHighSpeedRate, v => working.steerHighSpeedRate = v));
        fields.Add(new FloatField("Steer Speed Falloff", SettingsTab.Advanced, 0.8f, 2.5f,
            () => working.steerSpeedFalloff, v => working.steerSpeedFalloff = v));
        fields.Add(new FloatField("Keyboard Steer Scale", SettingsTab.Advanced, 0.5f, 1f,
            () => working.keyboardSteerScale, v => working.keyboardSteerScale = v));
        fields.Add(new FloatField("Mass", SettingsTab.Advanced, 800f, 1500f,
            () => working.mass, v => working.mass = v, 0));
        fields.Add(new FloatField("COM X", SettingsTab.Advanced, -0.5f, 0.5f,
            () => working.centerOfMass.x, v => working.centerOfMass = WithX(working.centerOfMass, v)));
        fields.Add(new FloatField("COM Y", SettingsTab.Advanced, 0f, 0.5f,
            () => working.centerOfMass.y, v => working.centerOfMass = WithY(working.centerOfMass, v)));
        fields.Add(new FloatField("COM Z", SettingsTab.Advanced, -0.3f, 0.3f,
            () => working.centerOfMass.z, v => working.centerOfMass = WithZ(working.centerOfMass, v)));
        fields.Add(new FloatField("Downforce", SettingsTab.Advanced, 0f, 40f,
            () => working.downforce, v => working.downforce = v, 0));
        fields.Add(new FloatField("Roll Stability", SettingsTab.Advanced, 500f, 4000f,
            () => working.rollStability, v => working.rollStability = v, 0));
        fields.Add(new FloatField("Pitch Stability", SettingsTab.Advanced, 500f, 4000f,
            () => working.pitchStability, v => working.pitchStability = v, 0));
        fields.Add(new FloatField("Handbrake Yaw", SettingsTab.Advanced, 100f, 500f,
            () => working.handbrakeYaw, v => working.handbrakeYaw = v, 0));
        fields.Add(new FloatField("Drift Align Strength", SettingsTab.Advanced, 5f, 40f,
            () => working.driftAlignStrength, v => working.driftAlignStrength = v, 0));
        fields.Add(new FloatField("Max Yaw Rate", SettingsTab.Advanced, 0.5f, 4f,
            () => working.maxYawRate, v => working.maxYawRate = v));
        fields.Add(new FloatField("Drift Angle Threshold", SettingsTab.Advanced, 3f, 20f,
            () => working.driftAngleThreshold, v => working.driftAngleThreshold = v, 1));
        fields.Add(new FloatField("Skid Slip Reference", SettingsTab.Advanced, 0.2f, 1f,
            () => working.skidSlipReference, v => working.skidSlipReference = v));
        fields.Add(new FloatField("Impact Stop (s)", SettingsTab.Advanced, 0.3f, 3f,
            () => working.impactStopSeconds, v => working.impactStopSeconds = v));
        fields.Add(new FloatField("Impact Brake Force", SettingsTab.Advanced, 2000f, 12000f,
            () => working.impactBrakeForce, v => working.impactBrakeForce = v, 0));
    }

    private static Vector3 WithX(Vector3 v, float x) => new Vector3(x, v.y, v.z);
    private static Vector3 WithY(Vector3 v, float y) => new Vector3(v.x, y, v.z);
    private static Vector3 WithZ(Vector3 v, float z) => new Vector3(v.x, v.y, z);

    private void CaptureCurrentSettings()
    {
        if (targetCar == null)
            return;

        working = targetCar.physics.Clone();
        selectedTier = tierComponent != null ? tierComponent.tier : DetectClosestTier(working);
    }

    private static CarTier DetectClosestTier(CarPhysicsSettings current)
    {
        CarTier best = CarTier.Commuter;
        float bestDistance = float.MaxValue;

        foreach (CarTier tier in Enum.GetValues(typeof(CarTier)))
        {
            CarPhysicsSettings preset = CarPhysicsSettings.GetPreset(tier);
            float distance = Mathf.Abs(preset.motorPower - current.motorPower)
                + Mathf.Abs(preset.maxSpeed - current.maxSpeed) * 40f
                + Mathf.Abs(preset.rearGrip - current.rearGrip) * 200f;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = tier;
            }
        }

        return best;
    }

    private void LoadPreset(CarTier tier)
    {
        selectedTier = tier;
        working = CarPhysicsSettings.GetPreset(tier).Clone();
    }

    private void ResetToPreset()
    {
        LoadPreset(selectedTier);
        RefreshAllFields();
        ApplyToCar();
    }

    private void ApplyToCar()
    {
        if (targetCar == null || working == null)
            return;

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
            CaptureCurrentSettings();
            RefreshTierDropdown();
            RefreshAssistToggle();
            RefreshAllFields();
        }
        else
        {
            Cursor.lockState = previousLockMode;
            Cursor.visible = previousCursorVisible;
        }
    }

    private void RefreshTierDropdown()
    {
        if (tierDropdown == null)
            return;

        updatingUi = true;
        tierDropdown.value = (int)selectedTier;
        tierDropdown.RefreshShownValue();
        updatingUi = false;
    }

    private void RefreshAllFields()
    {
        updatingUi = true;
        for (int i = 0; i < widgets.Count; i++)
        {
            FieldWidgets w = widgets[i];
            float value = Mathf.Clamp(w.Field.Get(), w.Field.Min, w.Field.Max);
            w.Slider.value = value;
            w.Input.text = FormatValue(value, w.Field.Decimals);
        }

        updatingUi = false;
    }

    private void OnTierDropdownChanged(int index)
    {
        if (updatingUi)
            return;

        LoadPreset((CarTier)index);
        RefreshAllFields();
        ApplyToCar();
    }

    private void OnFieldSliderChanged(FieldWidgets widget, float value)
    {
        if (updatingUi)
            return;

        value = Mathf.Clamp(value, widget.Field.Min, widget.Field.Max);
        widget.Field.Set(value);
        updatingUi = true;
        widget.Input.text = FormatValue(value, widget.Field.Decimals);
        updatingUi = false;
        ApplyToCar();
    }

    private void OnFieldInputEndEdit(FieldWidgets widget, string text)
    {
        if (updatingUi)
            return;

        if (!float.TryParse(text, out float value))
        {
            RefreshAllFields();
            return;
        }

        value = Mathf.Clamp(value, widget.Field.Min, widget.Field.Max);
        widget.Field.Set(value);
        updatingUi = true;
        widget.Slider.value = value;
        widget.Input.text = FormatValue(value, widget.Field.Decimals);
        updatingUi = false;
        ApplyToCar();
    }

    private void SetActiveTab(SettingsTab tab)
    {
        activeTab = tab;
        bool normal = tab == SettingsTab.Normal;
        normalTabContent.gameObject.SetActive(normal);
        advancedTabContent.gameObject.SetActive(!normal);
        StyleTabButton(normalTabButton, normal);
        StyleTabButton(advancedTabButton, !normal);
        scrollRect.content = normal ? normalTabContent : advancedTabContent;
        scrollRect.verticalNormalizedPosition = 1f;
    }

    private static void StyleTabButton(Button button, bool selected)
    {
        if (button == null)
            return;

        ColorBlock colors = button.colors;
        colors.normalColor = selected ? new Color(0.22f, 0.45f, 0.72f) : new Color(0.18f, 0.18f, 0.2f);
        button.colors = colors;
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

        GameObject canvasGo = new GameObject("CarPhysicsDevCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        canvasGo.AddComponent<GraphicRaycaster>();

        panelRoot = CreateUiObject("Panel", canvasGo.transform);
        Image panelBg = panelRoot.AddComponent<Image>();
        panelBg.color = new Color(0.08f, 0.09f, 0.11f, 0.94f);
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(560f, 680f);
        panelRect.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup panelLayout = panelRoot.AddComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(16, 16, 16, 16);
        panelLayout.spacing = 10f;
        panelLayout.childControlHeight = true;
        panelLayout.childControlWidth = true;
        panelLayout.childForceExpandHeight = false;
        panelLayout.childForceExpandWidth = true;

        CreateHeader(panelRoot.transform);
        tierDropdown = CreateTierDropdown(panelRoot.transform);
        tierDropdown.onValueChanged.AddListener(OnTierDropdownChanged);
        CreateAssistToggleRow(panelRoot.transform);
        CreateTabBar(panelRoot.transform);
        scrollRect = CreateScrollArea(panelRoot.transform, out normalTabContent, out advancedTabContent);
        CreateFieldRows();
        CreateFooter(panelRoot.transform);

        SetActiveTab(SettingsTab.Normal);
        uiBuilt = true;
        panelRoot.SetActive(false);
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

    private void CreateAssistToggleRow(Transform parent)
    {
        GameObject row = CreateUiObject("AssistRow", parent);
        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 34f;

        Text label = CreateText(row.transform, "Keyboard Assist", 15, FontStyle.Normal);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = new Vector2(0f, 0f);
        labelRect.sizeDelta = new Vector2(160f, 28f);

        GameObject toggleGo = CreateUiObject("AssistToggle", row.transform);
        RectTransform toggleRect = toggleGo.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(1f, 0.5f);
        toggleRect.anchorMax = new Vector2(1f, 0.5f);
        toggleRect.pivot = new Vector2(1f, 0.5f);
        toggleRect.anchoredPosition = Vector2.zero;
        toggleRect.sizeDelta = new Vector2(28f, 28f);

        Toggle toggle = toggleGo.AddComponent<Toggle>();
        toggle.isOn = keyboardAssist == null || keyboardAssist.AssistEnabled;

        GameObject bgGo = CreateUiObject("Background", toggleGo.transform);
        Image bg = bgGo.AddComponent<Image>();
        bg.color = new Color(0.16f, 0.17f, 0.2f);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        toggle.targetGraphic = bg;

        GameObject checkGo = CreateUiObject("Checkmark", toggleGo.transform);
        Image check = checkGo.AddComponent<Image>();
        check.color = new Color(0.35f, 0.75f, 0.45f);
        RectTransform checkRect = check.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.5f, 0.5f);
        checkRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkRect.pivot = new Vector2(0.5f, 0.5f);
        checkRect.sizeDelta = new Vector2(16f, 16f);
        toggle.graphic = check;

        assistToggle = toggle;
        assistToggle.onValueChanged.AddListener(OnAssistToggleChanged);
    }

    private void CreateHeader(Transform parent)
    {
        GameObject header = CreateUiObject("Header", parent);
        LayoutElement headerLayout = header.AddComponent<LayoutElement>();
        headerLayout.preferredHeight = 52f;

        Text title = CreateText(header.transform, "Car Physics Tuner (Dev)", 22, FontStyle.Bold);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        Text hint = CreateText(header.transform, "Press M to toggle", 13, FontStyle.Italic);
        hint.color = new Color(0.75f, 0.78f, 0.82f);
        RectTransform hintRect = hint.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0f, 0f);
        hintRect.anchorMax = new Vector2(1f, 0f);
        hintRect.pivot = new Vector2(1f, 0f);
        hintRect.anchoredPosition = new Vector2(-4f, 2f);
        hintRect.sizeDelta = new Vector2(0f, 18f);
        hint.alignment = TextAnchor.LowerRight;
    }

    private Dropdown CreateTierDropdown(Transform parent)
    {
        GameObject row = CreateUiObject("PresetRow", parent);
        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 36f;

        HorizontalLayoutGroup rowGroup = row.AddComponent<HorizontalLayoutGroup>();
        rowGroup.spacing = 10f;
        rowGroup.childAlignment = TextAnchor.MiddleLeft;
        rowGroup.childControlWidth = true;
        rowGroup.childControlHeight = true;
        rowGroup.childForceExpandWidth = false;
        rowGroup.childForceExpandHeight = true;

        Text label = CreateText(row.transform, "Preset", 15, FontStyle.Normal);
        LayoutElement labelLayout = label.gameObject.AddComponent<LayoutElement>();
        labelLayout.preferredWidth = 64f;
        labelLayout.minWidth = 64f;
        labelLayout.flexibleWidth = 0f;
        label.alignment = TextAnchor.MiddleLeft;

        GameObject dropdownGo = CreateUiObject("TierDropdown", row.transform);
        LayoutElement dropdownLayout = dropdownGo.AddComponent<LayoutElement>();
        dropdownLayout.preferredWidth = 180f;
        dropdownLayout.minWidth = 160f;
        dropdownLayout.preferredHeight = 30f;
        dropdownLayout.flexibleWidth = 0f;

        Image dropdownBg = dropdownGo.AddComponent<Image>();
        dropdownBg.color = new Color(0.18f, 0.22f, 0.28f);
        Dropdown dropdown = dropdownGo.AddComponent<Dropdown>();

        GameObject labelGo = CreateUiObject("Label", dropdownGo.transform);
        Text caption = CreateText(labelGo.transform, "Commuter", 14, FontStyle.Normal);
        caption.alignment = TextAnchor.MiddleLeft;
        RectTransform captionRect = caption.rectTransform;
        captionRect.anchorMin = Vector2.zero;
        captionRect.anchorMax = Vector2.one;
        captionRect.offsetMin = new Vector2(12f, 2f);
        captionRect.offsetMax = new Vector2(-30f, -2f);
        dropdown.captionText = caption;

        GameObject arrowGo = CreateUiObject("Arrow", dropdownGo.transform);
        Text arrow = CreateText(arrowGo.transform, "▼", 10, FontStyle.Normal);
        arrow.color = new Color(0.75f, 0.8f, 0.88f);
        arrow.alignment = TextAnchor.MiddleCenter;
        RectTransform arrowRect = arrow.rectTransform;
        arrowRect.anchorMin = new Vector2(1f, 0.5f);
        arrowRect.anchorMax = new Vector2(1f, 0.5f);
        arrowRect.pivot = new Vector2(1f, 0.5f);
        arrowRect.anchoredPosition = new Vector2(-6f, 0f);
        arrowRect.sizeDelta = new Vector2(22f, 22f);

        GameObject template = CreateUiObject("Template", dropdownGo.transform);
        template.SetActive(false);
        RectTransform templateRect = template.GetComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0f, 0f);
        templateRect.anchorMax = new Vector2(1f, 0f);
        templateRect.pivot = new Vector2(0.5f, 1f);
        templateRect.anchoredPosition = new Vector2(0f, 2f);
        templateRect.sizeDelta = new Vector2(0f, 140f);

        ScrollRect templateScroll = template.AddComponent<ScrollRect>();
        templateScroll.horizontal = false;
        Image templateBg = template.AddComponent<Image>();
        templateBg.color = new Color(0.12f, 0.13f, 0.16f);

        GameObject viewport = CreateUiObject("Viewport", template.transform);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        viewport.AddComponent<Image>().color = Color.white;
        templateScroll.viewport = viewportRect;

        GameObject content = CreateUiObject("Content", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 28f);
        VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandHeight = false;
        templateScroll.content = contentRect;

        GameObject item = CreateUiObject("Item", content.transform);
        LayoutElement itemLayout = item.AddComponent<LayoutElement>();
        itemLayout.preferredHeight = 28f;
        Toggle itemToggle = item.AddComponent<Toggle>();

        GameObject itemBg = CreateUiObject("Item Background", item.transform);
        Image itemBgImage = itemBg.AddComponent<Image>();
        itemBgImage.color = new Color(0.18f, 0.19f, 0.22f);
        RectTransform itemBgRect = itemBg.GetComponent<RectTransform>();
        itemBgRect.anchorMin = Vector2.zero;
        itemBgRect.anchorMax = Vector2.one;
        itemBgRect.offsetMin = Vector2.zero;
        itemBgRect.offsetMax = Vector2.zero;
        itemToggle.targetGraphic = itemBgImage;

        GameObject itemCheck = CreateUiObject("Item Checkmark", item.transform);
        Image checkImage = itemCheck.AddComponent<Image>();
        checkImage.color = new Color(0.35f, 0.65f, 0.95f);
        RectTransform checkRect = itemCheck.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0f, 0.5f);
        checkRect.anchorMax = new Vector2(0f, 0.5f);
        checkRect.pivot = new Vector2(0f, 0.5f);
        checkRect.anchoredPosition = new Vector2(6f, 0f);
        checkRect.sizeDelta = new Vector2(12f, 12f);
        itemToggle.graphic = checkImage;

        GameObject itemLabelGo = CreateUiObject("Item Label", item.transform);
        Text itemLabel = CreateText(itemLabelGo.transform, "Option", 13, FontStyle.Normal);
        itemLabel.alignment = TextAnchor.MiddleLeft;
        RectTransform itemLabelRect = itemLabel.rectTransform;
        itemLabelRect.anchorMin = Vector2.zero;
        itemLabelRect.anchorMax = Vector2.one;
        itemLabelRect.offsetMin = new Vector2(24f, 0f);
        itemLabelRect.offsetMax = new Vector2(-4f, 0f);

        dropdown.template = templateRect;
        dropdown.itemText = itemLabel;

        dropdown.options.Clear();
        foreach (CarTier tier in Enum.GetValues(typeof(CarTier)))
            dropdown.options.Add(new Dropdown.OptionData(tier.ToString()));

        return dropdown;
    }

    private void CreateTabBar(Transform parent)
    {
        GameObject tabBar = CreateUiObject("TabBar", parent);
        HorizontalLayoutGroup layout = tabBar.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childForceExpandWidth = true;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        LayoutElement tabBarLayout = tabBar.AddComponent<LayoutElement>();
        tabBarLayout.preferredHeight = 34f;

        normalTabButton = CreateTabButton(tabBar.transform, "Normal", () => SetActiveTab(SettingsTab.Normal));
        advancedTabButton = CreateTabButton(tabBar.transform, "Advanced Physics", () => SetActiveTab(SettingsTab.Advanced));
    }

    private Button CreateTabButton(Transform parent, string label, Action onClick)
    {
        GameObject buttonGo = CreateUiObject(label + "Tab", parent);
        Image bg = buttonGo.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.18f, 0.2f);
        Button button = buttonGo.AddComponent<Button>();
        button.targetGraphic = bg;
        button.onClick.AddListener(() => onClick());

        Text text = CreateText(buttonGo.transform, label, 14, FontStyle.Bold);
        text.alignment = TextAnchor.MiddleCenter;
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return button;
    }

    private ScrollRect CreateScrollArea(Transform parent, out RectTransform normalContent, out RectTransform advancedContent)
    {
        GameObject scrollGo = CreateUiObject("ScrollArea", parent);
        LayoutElement scrollLayout = scrollGo.AddComponent<LayoutElement>();
        scrollLayout.flexibleHeight = 1f;
        scrollLayout.minHeight = 360f;

        Image scrollBg = scrollGo.AddComponent<Image>();
        scrollBg.color = new Color(0.11f, 0.12f, 0.14f, 0.85f);
        ScrollRect scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;

        GameObject viewport = CreateUiObject("Viewport", scrollGo.transform);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(4f, 4f);
        viewportRect.offsetMax = new Vector2(-4f, -4f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        viewport.AddComponent<Image>().color = Color.white;
        scroll.viewport = viewportRect;

        normalContent = CreateTabContent(viewport.transform, "NormalContent");
        advancedContent = CreateTabContent(viewport.transform, "AdvancedContent");
        advancedContent.gameObject.SetActive(false);
        scroll.content = normalContent;
        return scroll;
    }

    private RectTransform CreateTabContent(Transform parent, string name)
    {
        GameObject contentGo = CreateUiObject(name, parent);
        RectTransform contentRect = contentGo.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup layout = contentGo.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(6, 6, 6, 6);
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        ContentSizeFitter fitter = contentGo.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return contentRect;
    }

    private void CreateFieldRows()
    {
        widgets.Clear();
        for (int i = 0; i < fields.Count; i++)
        {
            FloatField field = fields[i];
            Transform parent = field.Tab == SettingsTab.Normal ? normalTabContent : advancedTabContent;
            FieldWidgets widget = CreateFieldRow(parent, field);
            widgets.Add(widget);
        }
    }

    private FieldWidgets CreateFieldRow(Transform parent, FloatField field)
    {
        const float valueFieldWidth = 58f;

        GameObject row = CreateUiObject(field.Label, parent);
        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 54f;

        VerticalLayoutGroup rowGroup = row.AddComponent<VerticalLayoutGroup>();
        rowGroup.spacing = 2f;
        rowGroup.childControlHeight = true;
        rowGroup.childControlWidth = true;
        rowGroup.childForceExpandHeight = false;
        rowGroup.childForceExpandWidth = true;

        Text label = CreateText(row.transform, field.Label, 13, FontStyle.Normal);
        label.color = new Color(0.82f, 0.85f, 0.9f);
        LayoutElement labelLayout = label.gameObject.AddComponent<LayoutElement>();
        labelLayout.preferredHeight = 18f;

        GameObject controls = CreateUiObject("Controls", row.transform);
        LayoutElement controlsLayoutElement = controls.AddComponent<LayoutElement>();
        controlsLayoutElement.preferredHeight = 26f;
        // No HorizontalLayoutGroup — pin a narrow value box on the right so it can't stretch.

        GameObject sliderGo = CreateUiObject("Slider", controls.transform);
        RectTransform sliderRect = sliderGo.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 0.5f);
        sliderRect.anchorMax = new Vector2(1f, 0.5f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.anchoredPosition = Vector2.zero;
        sliderRect.sizeDelta = new Vector2(-(valueFieldWidth + 8f), 22f);
        sliderRect.offsetMin = new Vector2(0f, -11f);
        sliderRect.offsetMax = new Vector2(-(valueFieldWidth + 8f), 11f);

        Slider slider = sliderGo.AddComponent<Slider>();
        slider.minValue = field.Min;
        slider.maxValue = field.Max;
        slider.wholeNumbers = field.Decimals <= 0;

        GameObject sliderBg = CreateUiObject("Background", sliderGo.transform);
        Image sliderBgImage = sliderBg.AddComponent<Image>();
        sliderBgImage.color = new Color(0.2f, 0.21f, 0.24f);
        RectTransform sliderBgRect = sliderBg.GetComponent<RectTransform>();
        sliderBgRect.anchorMin = new Vector2(0f, 0.42f);
        sliderBgRect.anchorMax = new Vector2(1f, 0.58f);
        sliderBgRect.offsetMin = Vector2.zero;
        sliderBgRect.offsetMax = Vector2.zero;

        GameObject fillArea = CreateUiObject("Fill Area", sliderGo.transform);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.42f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.58f);
        fillAreaRect.offsetMin = new Vector2(4f, 0f);
        fillAreaRect.offsetMax = new Vector2(-4f, 0f);

        GameObject fill = CreateUiObject("Fill", fillArea.transform);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.28f, 0.55f, 0.86f);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        GameObject handleSlide = CreateUiObject("Handle Slide Area", sliderGo.transform);
        RectTransform handleSlideRect = handleSlide.GetComponent<RectTransform>();
        handleSlideRect.anchorMin = Vector2.zero;
        handleSlideRect.anchorMax = Vector2.one;
        handleSlideRect.offsetMin = new Vector2(4f, 0f);
        handleSlideRect.offsetMax = new Vector2(-4f, 0f);

        GameObject handle = CreateUiObject("Handle", handleSlide.transform);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = new Color(0.92f, 0.94f, 0.98f);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(8f, 12f);

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;

        GameObject inputGo = CreateUiObject("Input", controls.transform);
        RectTransform inputRect = inputGo.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(1f, 0.5f);
        inputRect.anchorMax = new Vector2(1f, 0.5f);
        inputRect.pivot = new Vector2(1f, 0.5f);
        inputRect.anchoredPosition = Vector2.zero;
        inputRect.sizeDelta = new Vector2(valueFieldWidth, 24f);

        Image inputBg = inputGo.AddComponent<Image>();
        inputBg.color = new Color(0.16f, 0.17f, 0.2f);
        InputField input = inputGo.AddComponent<InputField>();
        input.contentType = InputField.ContentType.DecimalNumber;

        // Text must be a direct child of the InputField object.
        Text inputText = CreateText(inputGo.transform, "0", 12, FontStyle.Normal);
        inputText.alignment = TextAnchor.MiddleCenter;
        RectTransform inputTextRect = inputText.rectTransform;
        inputTextRect.anchorMin = Vector2.zero;
        inputTextRect.anchorMax = Vector2.one;
        inputTextRect.offsetMin = new Vector2(2f, 0f);
        inputTextRect.offsetMax = new Vector2(-2f, 0f);
        input.textComponent = inputText;
        input.text = "0";

        FieldWidgets widget = new FieldWidgets
        {
            Tab = field.Tab,
            Row = row,
            Slider = slider,
            Input = input,
            Field = field
        };

        slider.onValueChanged.AddListener(value => OnFieldSliderChanged(widget, value));
        input.onEndEdit.AddListener(text => OnFieldInputEndEdit(widget, text));
        return widget;
    }

    private void CreateFooter(Transform parent)
    {
        GameObject footer = CreateUiObject("Footer", parent);
        LayoutElement footerLayout = footer.AddComponent<LayoutElement>();
        footerLayout.preferredHeight = 40f;

        Button resetButton = CreateActionButton(footer.transform, "Reset to Preset", ResetToPreset);
        RectTransform resetRect = resetButton.GetComponent<RectTransform>();
        resetRect.anchorMin = new Vector2(0f, 0.5f);
        resetRect.anchorMax = new Vector2(0f, 0.5f);
        resetRect.pivot = new Vector2(0f, 0.5f);
        resetRect.anchoredPosition = Vector2.zero;
        resetRect.sizeDelta = new Vector2(180f, 34f);

        Text footerHint = CreateText(footer.transform, "Edits apply live to the car", 12, FontStyle.Italic);
        footerHint.color = new Color(0.65f, 0.68f, 0.72f);
        footerHint.alignment = TextAnchor.MiddleRight;
        RectTransform hintRect = footerHint.rectTransform;
        hintRect.anchorMin = new Vector2(0f, 0f);
        hintRect.anchorMax = new Vector2(1f, 1f);
        hintRect.offsetMin = new Vector2(190f, 0f);
        hintRect.offsetMax = Vector2.zero;
    }

    private Button CreateActionButton(Transform parent, string label, Action onClick)
    {
        GameObject buttonGo = CreateUiObject(label + "Button", parent);
        Image bg = buttonGo.AddComponent<Image>();
        bg.color = new Color(0.24f, 0.28f, 0.34f);
        Button button = buttonGo.AddComponent<Button>();
        button.targetGraphic = bg;
        button.onClick.AddListener(() => onClick());

        Text text = CreateText(buttonGo.transform, label, 14, FontStyle.Bold);
        text.alignment = TextAnchor.MiddleCenter;
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return button;
    }

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
            return;

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static Text CreateText(Transform parent, string content, int fontSize, FontStyle style)
    {
        GameObject textGo = CreateUiObject("Text", parent);
        Text text = textGo.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.text = content;
        text.supportRichText = false;
        return text;
    }
}
