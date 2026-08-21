using UnityEngine;

[DefaultExecutionOrder(-100)]
public class WebGLQualityBootstrap : MonoBehaviour
{
    public int targetFrameRate = 60;
    public string qualityLevelName = "Mobile";

    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;

        int qualityIndex = FindQualityIndex(qualityLevelName);
        if (qualityIndex >= 0)
            QualitySettings.SetQualityLevel(qualityIndex, true);
    }

    private static int FindQualityIndex(string name)
    {
        string[] names = QualitySettings.names;
        for (int i = 0; i < names.Length; i++)
        {
            if (names[i] == name)
                return i;
        }

        return -1;
    }
}
