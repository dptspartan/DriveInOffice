using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Persist CarPhysicsSettings (+ camera / assist) as key=value .txt and PlayerPrefs.
/// </summary>
public static class CarTuneStore
{
    public const string PrefsPrefix = "CarTune_";
    public const string LastPresetKey = "CarTune_LastPreset";
    public const string AssistEnabledKey = "CarTune_AssistEnabled";
    public const string AssistStrengthKey = "CarTune_AssistStrength";

    public static string TunesFolder => Path.Combine(Application.persistentDataPath, "CarTunes");

    public static string FilePath(CarTier tier) => Path.Combine(TunesFolder, tier + ".txt");

    public static void Save(
        CarTier tier,
        CarPhysicsSettings settings,
        CarFollowCamera cam,
        CarKeyboardDrivingAssist assist)
    {
        string txt = ToTxt(settings, cam, assist);
        try
        {
            Directory.CreateDirectory(TunesFolder);
            File.WriteAllText(FilePath(tier), txt);
        }
        catch (Exception e)
        {
            Debug.LogWarning("CarTune file save failed: " + e.Message);
        }

        PlayerPrefs.SetString(PrefsPrefix + tier, txt);
        PlayerPrefs.SetInt(LastPresetKey, (int)tier);
        if (assist != null)
        {
            PlayerPrefs.SetInt(AssistEnabledKey, assist.AssistEnabled ? 1 : 0);
            PlayerPrefs.SetFloat(AssistStrengthKey, assist.AssistStrength);
        }
        PlayerPrefs.Save();
    }

    public static CarPhysicsSettings Load(CarTier tier, out float camFollow, out float camRot,
        out float camDrift, out float camPull, out float camLookAhead, out float camLookHeight,
        out float camMinFov, out float camMaxFov, out bool assistOn, out float assistStrength)
    {
        camFollow = 0.14f;
        camRot = 8f;
        camDrift = 0.55f;
        camPull = 0.65f;
        camLookAhead = 0.06f;
        camLookHeight = 0.55f;
        camMinFov = 55f;
        camMaxFov = 62f;
        assistOn = true;
        assistStrength = 0.4f;

        CarPhysicsSettings settings = CarPhysicsSettings.GetPreset(tier).Clone();
        string text = null;
        try
        {
            string path = FilePath(tier);
            if (File.Exists(path))
                text = File.ReadAllText(path);
        }
        catch (Exception e)
        {
            Debug.LogWarning("CarTune file load failed: " + e.Message);
        }

        if (string.IsNullOrEmpty(text))
            text = PlayerPrefs.GetString(PrefsPrefix + tier, string.Empty);

        if (!string.IsNullOrEmpty(text))
        {
            ApplyTxt(text, settings,
                ref camFollow, ref camRot, ref camDrift, ref camPull,
                ref camLookAhead, ref camLookHeight, ref camMinFov, ref camMaxFov,
                ref assistOn, ref assistStrength);
        }
        else
        {
            if (PlayerPrefs.HasKey(AssistEnabledKey))
                assistOn = PlayerPrefs.GetInt(AssistEnabledKey, 1) == 1;
            if (PlayerPrefs.HasKey(AssistStrengthKey))
                assistStrength = PlayerPrefs.GetFloat(AssistStrengthKey, 0.4f);
        }

        return settings;
    }

    public static string ToTxt(CarPhysicsSettings s, CarFollowCamera cam, CarKeyboardDrivingAssist assist)
    {
        var sb = new StringBuilder(2048);
        sb.AppendLine("# DriveInOffice car tune — key=value");
        W(sb, "motorPower", s.motorPower);
        W(sb, "maxSpeed", s.maxSpeed);
        W(sb, "reversePower", s.reversePower);
        W(sb, "brakeForce", s.brakeForce);
        W(sb, "handbrakeForce", s.handbrakeForce);
        W(sb, "coastBrake", s.coastBrake);
        W(sb, "maxSteerAngle", s.maxSteerAngle);
        W(sb, "minSteerAngle", s.minSteerAngle);
        W(sb, "steerRampIn", s.steerRampIn);
        W(sb, "steerRampOut", s.steerRampOut);
        W(sb, "steerHighSpeedRate", s.steerHighSpeedRate);
        W(sb, "steerSpeedFalloff", s.steerSpeedFalloff);
        W(sb, "keyboardSteerScale", s.keyboardSteerScale);
        W(sb, "frontGrip", s.frontGrip);
        W(sb, "rearGrip", s.rearGrip);
        W(sb, "handbrakeRearGrip", s.handbrakeRearGrip);
        W(sb, "mass", s.mass);
        W(sb, "comX", s.centerOfMass.x);
        W(sb, "comY", s.centerOfMass.y);
        W(sb, "comZ", s.centerOfMass.z);
        W(sb, "downforce", s.downforce);
        W(sb, "rollStability", s.rollStability);
        W(sb, "pitchStability", s.pitchStability);
        W(sb, "handbrakeYaw", s.handbrakeYaw);
        W(sb, "driftAlignStrength", s.driftAlignStrength);
        W(sb, "maxYawRate", s.maxYawRate);
        W(sb, "driftAngleThreshold", s.driftAngleThreshold);
        W(sb, "skidSlipReference", s.skidSlipReference);
        W(sb, "impactStopSeconds", s.impactStopSeconds);
        W(sb, "impactBrakeForce", s.impactBrakeForce);

        if (cam != null)
        {
            W(sb, "camFollowSmooth", cam.followSmooth);
            W(sb, "camRotationSharpness", cam.rotationSharpness);
            W(sb, "camDriftLateral", cam.driftLateral);
            W(sb, "camSpeedPullBack", cam.speedPullBack);
            W(sb, "camLookAhead", cam.lookAhead);
            W(sb, "camLookHeight", cam.lookHeight);
            W(sb, "camMinFov", cam.minFov);
            W(sb, "camMaxFov", cam.maxFov);
        }

        if (assist != null)
        {
            W(sb, "assistEnabled", assist.AssistEnabled ? 1f : 0f);
            W(sb, "assistStrength", assist.AssistStrength);
        }

        return sb.ToString();
    }

    private static void ApplyTxt(
        string text,
        CarPhysicsSettings s,
        ref float camFollow, ref float camRot, ref float camDrift, ref float camPull,
        ref float camLookAhead, ref float camLookHeight, ref float camMinFov, ref float camMaxFov,
        ref bool assistOn, ref float assistStrength)
    {
        string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line.Length == 0 || line[0] == '#')
                continue;
            int eq = line.IndexOf('=');
            if (eq <= 0)
                continue;
            string key = line.Substring(0, eq).Trim();
            if (!float.TryParse(line.Substring(eq + 1).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
                continue;

            switch (key)
            {
                case "motorPower": s.motorPower = v; break;
                case "maxSpeed": s.maxSpeed = v; break;
                case "reversePower": s.reversePower = v; break;
                case "brakeForce": s.brakeForce = v; break;
                case "handbrakeForce": s.handbrakeForce = v; break;
                case "coastBrake": s.coastBrake = v; break;
                case "maxSteerAngle": s.maxSteerAngle = v; break;
                case "minSteerAngle": s.minSteerAngle = v; break;
                case "steerRampIn": s.steerRampIn = v; break;
                case "steerRampOut": s.steerRampOut = v; break;
                case "steerHighSpeedRate": s.steerHighSpeedRate = v; break;
                case "steerSpeedFalloff": s.steerSpeedFalloff = v; break;
                case "keyboardSteerScale": s.keyboardSteerScale = v; break;
                case "frontGrip": s.frontGrip = v; break;
                case "rearGrip": s.rearGrip = v; break;
                case "handbrakeRearGrip": s.handbrakeRearGrip = v; break;
                case "mass": s.mass = v; break;
                case "comX": s.centerOfMass = new Vector3(v, s.centerOfMass.y, s.centerOfMass.z); break;
                case "comY": s.centerOfMass = new Vector3(s.centerOfMass.x, v, s.centerOfMass.z); break;
                case "comZ": s.centerOfMass = new Vector3(s.centerOfMass.x, s.centerOfMass.y, v); break;
                case "downforce": s.downforce = v; break;
                case "rollStability": s.rollStability = v; break;
                case "pitchStability": s.pitchStability = v; break;
                case "handbrakeYaw": s.handbrakeYaw = v; break;
                case "driftAlignStrength": s.driftAlignStrength = v; break;
                case "maxYawRate": s.maxYawRate = v; break;
                case "driftAngleThreshold": s.driftAngleThreshold = v; break;
                case "skidSlipReference": s.skidSlipReference = v; break;
                case "impactStopSeconds": s.impactStopSeconds = v; break;
                case "impactBrakeForce": s.impactBrakeForce = v; break;
                case "camFollowSmooth": camFollow = v; break;
                case "camRotationSharpness": camRot = v; break;
                case "camDriftLateral": camDrift = v; break;
                case "camSpeedPullBack": camPull = v; break;
                case "camLookAhead": camLookAhead = v; break;
                case "camLookHeight": camLookHeight = v; break;
                case "camMinFov": camMinFov = v; break;
                case "camMaxFov": camMaxFov = v; break;
                case "assistEnabled": assistOn = v >= 0.5f; break;
                case "assistStrength": assistStrength = v; break;
            }
        }
    }

    private static void W(StringBuilder sb, string key, float value)
    {
        sb.Append(key).Append('=').Append(value.ToString("G9", CultureInfo.InvariantCulture)).AppendLine();
    }
}
