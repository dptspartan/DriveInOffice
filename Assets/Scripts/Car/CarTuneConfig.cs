using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public enum CarTunePreset
{
    Starter = 0,
    Commuter = 1,
    Sport = 2,
    Super = 3
}

/// <summary>
/// All gameplay car tune fields. Saved as key=value .txt per preset + PlayerPrefs (WebGL localStorage).
/// </summary>
[Serializable]
public class CarTuneConfig
{
    public const string PrefsPrefix = "CarTune_";
    public const string LastPresetKey = "CarTune_LastPreset";

    public float motorForce = 1550f;
    public float maxSpeed = 18f;
    public float reverseMotorScale = 0.85f;
    public float footBrakeForce = 2800f;
    public float handbrakeForce = 3500f;
    public float engineBrakeForce = 1400f;
    public float maxSteerAngle = 17f;
    public float minSteerAngle = 8f;
    public float steerSpeed = 2.4f;
    public float steerFalloff = 1.25f;
    public float highSpeedSteerRate = 0.38f;
    public float steerReleaseMultiplier = 1.35f;
    public float keyboardSteerScale = 0.9f;
    public float frontSidewaysStiffness = 2.4f;
    public float rearSidewaysStiffness = 2.35f;
    public float handbrakeRearStiffness = 0.65f;
    public float forwardStiffness = 2.35f;
    public float mass = 1200f;
    public float comX;
    public float comY = 0.16f;
    public float comZ = 0.05f;
    public float angularDamping = 0.55f;
    public float downforce = 16f;
    public float stabilityYaw = 2100f;
    public float handbrakeYaw = 320f;
    public float maxSpinRate = 1.8f;
    public float impactStopSeconds = 1.35f;
    public float impactBrakeForce = 8000f;

    // Camera (gameplay feel)
    public float camFollowSmooth = 0.14f;
    public float camRotationSharpness = 8f;
    public float camDriftLateral = 0.55f;
    public float camSpeedPullBack = 0.65f;
    public float camLookAhead = 0.06f;
    public float camLookHeight = 0.55f;
    public float camMinFov = 55f;
    public float camMaxFov = 62f;

    public static string TunesFolder => Path.Combine(Application.persistentDataPath, "CarTunes");

    public static string FilePath(CarTunePreset preset)
    {
        return Path.Combine(TunesFolder, preset + ".txt");
    }

    public CarTuneConfig Clone()
    {
        return (CarTuneConfig)MemberwiseClone();
    }

    public static CarTuneConfig FromCar(KenneyCarController car, CarFollowCamera cam)
    {
        var c = new CarTuneConfig();
        if (car != null)
        {
            c.motorForce = car.motorForce;
            c.maxSpeed = car.maxSpeed;
            c.reverseMotorScale = car.reverseMotorScale;
            c.footBrakeForce = car.footBrakeForce;
            c.handbrakeForce = car.handbrakeForce;
            c.engineBrakeForce = car.engineBrakeForce;
            c.maxSteerAngle = car.maxSteerAngle;
            c.minSteerAngle = car.minSteerAngle;
            c.steerSpeed = car.steerSpeed;
            c.steerFalloff = car.steerFalloff;
            c.highSpeedSteerRate = car.highSpeedSteerRate;
            c.steerReleaseMultiplier = car.steerReleaseMultiplier;
            c.keyboardSteerScale = car.keyboardSteerScale;
            c.frontSidewaysStiffness = car.frontSidewaysStiffness;
            c.rearSidewaysStiffness = car.rearSidewaysStiffness;
            c.handbrakeRearStiffness = car.handbrakeRearStiffness;
            c.forwardStiffness = car.forwardStiffness;
            c.mass = car.mass;
            c.comX = car.centerOfMass.x;
            c.comY = car.centerOfMass.y;
            c.comZ = car.centerOfMass.z;
            c.angularDamping = car.angularDamping;
            c.downforce = car.downforce;
            c.stabilityYaw = car.stabilityYaw;
            c.handbrakeYaw = car.handbrakeYaw;
            c.maxSpinRate = car.maxSpinRate;
            c.impactStopSeconds = car.impactStopSeconds;
            c.impactBrakeForce = car.impactBrakeForce;
        }

        if (cam != null)
        {
            c.camFollowSmooth = cam.followSmooth;
            c.camRotationSharpness = cam.rotationSharpness;
            c.camDriftLateral = cam.driftLateral;
            c.camSpeedPullBack = cam.speedPullBack;
            c.camLookAhead = cam.lookAhead;
            c.camLookHeight = cam.lookHeight;
            c.camMinFov = cam.minFov;
            c.camMaxFov = cam.maxFov;
        }

        return c;
    }

    public void ApplyTo(KenneyCarController car, CarFollowCamera cam)
    {
        if (car != null)
        {
            car.motorForce = motorForce;
            car.maxSpeed = maxSpeed;
            car.reverseMotorScale = reverseMotorScale;
            car.footBrakeForce = footBrakeForce;
            car.handbrakeForce = handbrakeForce;
            car.engineBrakeForce = engineBrakeForce;
            car.maxSteerAngle = maxSteerAngle;
            car.minSteerAngle = minSteerAngle;
            car.steerSpeed = steerSpeed;
            car.steerFalloff = steerFalloff;
            car.highSpeedSteerRate = highSpeedSteerRate;
            car.steerReleaseMultiplier = steerReleaseMultiplier;
            car.keyboardSteerScale = keyboardSteerScale;
            car.frontSidewaysStiffness = frontSidewaysStiffness;
            car.rearSidewaysStiffness = rearSidewaysStiffness;
            car.handbrakeRearStiffness = handbrakeRearStiffness;
            car.forwardStiffness = forwardStiffness;
            car.mass = mass;
            car.centerOfMass = new Vector3(comX, comY, comZ);
            car.angularDamping = angularDamping;
            car.downforce = downforce;
            car.stabilityYaw = stabilityYaw;
            car.handbrakeYaw = handbrakeYaw;
            car.maxSpinRate = maxSpinRate;
            car.impactStopSeconds = impactStopSeconds;
            car.impactBrakeForce = impactBrakeForce;
            car.ApplyLiveTune();
        }

        if (cam != null)
        {
            cam.followSmooth = camFollowSmooth;
            cam.rotationSharpness = camRotationSharpness;
            cam.driftLateral = camDriftLateral;
            cam.speedPullBack = camSpeedPullBack;
            cam.lookAhead = camLookAhead;
            cam.lookHeight = camLookHeight;
            cam.minFov = camMinFov;
            cam.maxFov = camMaxFov;
        }
    }

    public string ToTxt()
    {
        var sb = new StringBuilder(1024);
        sb.AppendLine("# DriveInOffice car tune — key=value");
        Write(sb, nameof(motorForce), motorForce);
        Write(sb, nameof(maxSpeed), maxSpeed);
        Write(sb, nameof(reverseMotorScale), reverseMotorScale);
        Write(sb, nameof(footBrakeForce), footBrakeForce);
        Write(sb, nameof(handbrakeForce), handbrakeForce);
        Write(sb, nameof(engineBrakeForce), engineBrakeForce);
        Write(sb, nameof(maxSteerAngle), maxSteerAngle);
        Write(sb, nameof(minSteerAngle), minSteerAngle);
        Write(sb, nameof(steerSpeed), steerSpeed);
        Write(sb, nameof(steerFalloff), steerFalloff);
        Write(sb, nameof(highSpeedSteerRate), highSpeedSteerRate);
        Write(sb, nameof(steerReleaseMultiplier), steerReleaseMultiplier);
        Write(sb, nameof(keyboardSteerScale), keyboardSteerScale);
        Write(sb, nameof(frontSidewaysStiffness), frontSidewaysStiffness);
        Write(sb, nameof(rearSidewaysStiffness), rearSidewaysStiffness);
        Write(sb, nameof(handbrakeRearStiffness), handbrakeRearStiffness);
        Write(sb, nameof(forwardStiffness), forwardStiffness);
        Write(sb, nameof(mass), mass);
        Write(sb, nameof(comX), comX);
        Write(sb, nameof(comY), comY);
        Write(sb, nameof(comZ), comZ);
        Write(sb, nameof(angularDamping), angularDamping);
        Write(sb, nameof(downforce), downforce);
        Write(sb, nameof(stabilityYaw), stabilityYaw);
        Write(sb, nameof(handbrakeYaw), handbrakeYaw);
        Write(sb, nameof(maxSpinRate), maxSpinRate);
        Write(sb, nameof(impactStopSeconds), impactStopSeconds);
        Write(sb, nameof(impactBrakeForce), impactBrakeForce);
        Write(sb, nameof(camFollowSmooth), camFollowSmooth);
        Write(sb, nameof(camRotationSharpness), camRotationSharpness);
        Write(sb, nameof(camDriftLateral), camDriftLateral);
        Write(sb, nameof(camSpeedPullBack), camSpeedPullBack);
        Write(sb, nameof(camLookAhead), camLookAhead);
        Write(sb, nameof(camLookHeight), camLookHeight);
        Write(sb, nameof(camMinFov), camMinFov);
        Write(sb, nameof(camMaxFov), camMaxFov);
        return sb.ToString();
    }

    public static CarTuneConfig FromTxt(string text, CarTuneConfig fallback = null)
    {
        CarTuneConfig c = fallback != null ? fallback.Clone() : new CarTuneConfig();
        if (string.IsNullOrEmpty(text))
            return c;

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
            string val = line.Substring(eq + 1).Trim();
            if (!float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
                continue;
            SetField(c, key, f);
        }

        return c;
    }

    public void SavePreset(CarTunePreset preset)
    {
        try
        {
            Directory.CreateDirectory(TunesFolder);
            File.WriteAllText(FilePath(preset), ToTxt());
        }
        catch (Exception e)
        {
            Debug.LogWarning("CarTune file save failed: " + e.Message);
        }

        PlayerPrefs.SetString(PrefsPrefix + preset, ToTxt());
        PlayerPrefs.SetInt(LastPresetKey, (int)preset);
        PlayerPrefs.Save();
    }

    public static CarTuneConfig LoadPreset(CarTunePreset preset, CarTuneConfig builtinFallback)
    {
        string path = FilePath(preset);
        try
        {
            if (File.Exists(path))
                return FromTxt(File.ReadAllText(path), builtinFallback);
        }
        catch (Exception e)
        {
            Debug.LogWarning("CarTune file load failed: " + e.Message);
        }

        string prefs = PlayerPrefs.GetString(PrefsPrefix + preset, string.Empty);
        if (!string.IsNullOrEmpty(prefs))
            return FromTxt(prefs, builtinFallback);

        return builtinFallback != null ? builtinFallback.Clone() : GetBuiltin(preset);
    }

    public static CarTuneConfig GetBuiltin(CarTunePreset preset)
    {
        switch (preset)
        {
            case CarTunePreset.Starter:
                return new CarTuneConfig
                {
                    motorForce = 1100f, maxSpeed = 14f, reverseMotorScale = 0.8f,
                    footBrakeForce = 2600f, handbrakeForce = 3000f, engineBrakeForce = 1200f,
                    maxSteerAngle = 20f, minSteerAngle = 10f, steerSpeed = 3.2f,
                    steerFalloff = 1.1f, highSpeedSteerRate = 0.55f, steerReleaseMultiplier = 1.5f,
                    keyboardSteerScale = 0.85f,
                    frontSidewaysStiffness = 2.6f, rearSidewaysStiffness = 2.55f,
                    handbrakeRearStiffness = 0.75f, forwardStiffness = 2.5f,
                    mass = 1250f, comY = 0.15f, comZ = 0.04f, angularDamping = 0.65f,
                    downforce = 12f, stabilityYaw = 2400f, handbrakeYaw = 260f, maxSpinRate = 1.4f
                };
            case CarTunePreset.Sport:
                return new CarTuneConfig
                {
                    motorForce = 1900f, maxSpeed = 22f, reverseMotorScale = 0.85f,
                    footBrakeForce = 3200f, handbrakeForce = 3800f, engineBrakeForce = 1500f,
                    maxSteerAngle = 16f, minSteerAngle = 7f, steerSpeed = 2.8f,
                    steerFalloff = 1.3f, highSpeedSteerRate = 0.42f, steerReleaseMultiplier = 1.4f,
                    keyboardSteerScale = 0.88f,
                    frontSidewaysStiffness = 2.3f, rearSidewaysStiffness = 2.15f,
                    handbrakeRearStiffness = 0.55f, forwardStiffness = 2.3f,
                    mass = 1100f, comY = 0.14f, comZ = 0.06f, angularDamping = 0.5f,
                    downforce = 20f, stabilityYaw = 1900f, handbrakeYaw = 360f, maxSpinRate = 2.0f
                };
            case CarTunePreset.Super:
                return new CarTuneConfig
                {
                    motorForce = 2400f, maxSpeed = 26f, reverseMotorScale = 0.9f,
                    footBrakeForce = 3600f, handbrakeForce = 4200f, engineBrakeForce = 1600f,
                    maxSteerAngle = 15f, minSteerAngle = 6f, steerSpeed = 2.6f,
                    steerFalloff = 1.4f, highSpeedSteerRate = 0.4f, steerReleaseMultiplier = 1.45f,
                    keyboardSteerScale = 0.86f,
                    frontSidewaysStiffness = 2.2f, rearSidewaysStiffness = 2.0f,
                    handbrakeRearStiffness = 0.45f, forwardStiffness = 2.25f,
                    mass = 1050f, comY = 0.13f, comZ = 0.08f, angularDamping = 0.45f,
                    downforce = 26f, stabilityYaw = 1700f, handbrakeYaw = 420f, maxSpinRate = 2.2f
                };
            default: // Commuter
                return new CarTuneConfig();
        }
    }

    private static void Write(StringBuilder sb, string key, float value)
    {
        sb.Append(key).Append('=').Append(value.ToString("G9", CultureInfo.InvariantCulture)).AppendLine();
    }

    private static void SetField(CarTuneConfig c, string key, float v)
    {
        switch (key)
        {
            case nameof(motorForce): c.motorForce = v; break;
            case nameof(maxSpeed): c.maxSpeed = v; break;
            case nameof(reverseMotorScale): c.reverseMotorScale = v; break;
            case nameof(footBrakeForce): c.footBrakeForce = v; break;
            case nameof(handbrakeForce): c.handbrakeForce = v; break;
            case nameof(engineBrakeForce): c.engineBrakeForce = v; break;
            case nameof(maxSteerAngle): c.maxSteerAngle = v; break;
            case nameof(minSteerAngle): c.minSteerAngle = v; break;
            case nameof(steerSpeed): c.steerSpeed = v; break;
            case nameof(steerFalloff): c.steerFalloff = v; break;
            case nameof(highSpeedSteerRate): c.highSpeedSteerRate = v; break;
            case nameof(steerReleaseMultiplier): c.steerReleaseMultiplier = v; break;
            case nameof(keyboardSteerScale): c.keyboardSteerScale = v; break;
            case nameof(frontSidewaysStiffness): c.frontSidewaysStiffness = v; break;
            case nameof(rearSidewaysStiffness): c.rearSidewaysStiffness = v; break;
            case nameof(handbrakeRearStiffness): c.handbrakeRearStiffness = v; break;
            case nameof(forwardStiffness): c.forwardStiffness = v; break;
            case nameof(mass): c.mass = v; break;
            case nameof(comX): c.comX = v; break;
            case nameof(comY): c.comY = v; break;
            case nameof(comZ): c.comZ = v; break;
            case nameof(angularDamping): c.angularDamping = v; break;
            case nameof(downforce): c.downforce = v; break;
            case nameof(stabilityYaw): c.stabilityYaw = v; break;
            case nameof(handbrakeYaw): c.handbrakeYaw = v; break;
            case nameof(maxSpinRate): c.maxSpinRate = v; break;
            case nameof(impactStopSeconds): c.impactStopSeconds = v; break;
            case nameof(impactBrakeForce): c.impactBrakeForce = v; break;
            case nameof(camFollowSmooth): c.camFollowSmooth = v; break;
            case nameof(camRotationSharpness): c.camRotationSharpness = v; break;
            case nameof(camDriftLateral): c.camDriftLateral = v; break;
            case nameof(camSpeedPullBack): c.camSpeedPullBack = v; break;
            case nameof(camLookAhead): c.camLookAhead = v; break;
            case nameof(camLookHeight): c.camLookHeight = v; break;
            case nameof(camMinFov): c.camMinFov = v; break;
            case nameof(camMaxFov): c.camMaxFov = v; break;
        }
    }
}
