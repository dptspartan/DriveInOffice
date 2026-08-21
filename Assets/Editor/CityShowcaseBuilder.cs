using UnityEditor;

public static class CityShowcaseBuilder
{
    [MenuItem("DriveInOffice/Build Cartoon City Showcase")]
    public static void Build()
    {
        OrganicCityBaker.Bake();
    }
}
