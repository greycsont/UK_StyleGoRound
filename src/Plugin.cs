using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using System;
using System.Reflection;
using HarmonyLib;


namespace StyleGoRound;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInProcess("ULTRAKILL.exe")]
public class Plugin : BaseUnityPlugin
{
    private Harmony harmony;
    private void Awake()
    {
        LogHelper.log = base.Logger;
        LoadMainModule();
        LoadOptionalModule();

        PatchHarmony(); 
        LogHelper.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void LoadMainModule()
    {
        
    }
    private void LoadOptionalModule()
    {

    }
    private void PatchHarmony()
    {
        harmony = new Harmony(PluginInfo.PLUGIN_GUID+".harmony");
        harmony.PatchAll();
    }

}
