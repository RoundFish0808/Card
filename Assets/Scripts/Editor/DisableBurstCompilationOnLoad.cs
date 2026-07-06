using Unity.Burst;
using UnityEditor;

[InitializeOnLoad]
public static class DisableBurstCompilationOnLoad
{
    private const string BurstCompilationEditorPref = "BurstCompilation";

    static DisableBurstCompilationOnLoad()
    {
        EditorPrefs.SetBool(BurstCompilationEditorPref, false);
        BurstCompiler.Options.EnableBurstCompilation = false;
    }
}
