using System;
using System.Collections.Generic;
using System.IO;

using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;

using UnityEngine;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace UnityTexture2DReplacer
{
    [BepInPlugin(
        "com.unitytexture2dreplacer.plugin",
        "UnityTexture2DReplacer",
        "1.0.0")]
    public class Plugin : BasePlugin
    {
        internal static ManualLogSource PluginLog;

        public override void Load()
        {
            PluginLog = Log;

            Log.LogInfo("=================================");
            Log.LogInfo("UnityTexture2DReplacer 1.0.0");
            Log.LogInfo("Runtime Texture2D replacer");
            Log.LogInfo("=================================");

            AddComponent<TextureReplacerBehaviour>();
        }
    }

    public class TextureReplacerBehaviour : MonoBehaviour
    {
        // -------------------------------------------------
        // 외부 PNG 목록
        //
        // Key   = Texture2D 이름
        // Value = PNG 전체 경로
        // -------------------------------------------------

        private readonly Dictionary<string, string> replacementFiles =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase
            );

        // 이미 교체한 Texture 인스턴스를 저장합니다
        private readonly HashSet<int> replacedInstances =
            new HashSet<int>();

        private float nextScanTime = 0f;

        private const float ScanInterval = 2.0f;

        private string textureDirectory;

        // 네이티브 객체 생성자 입니다. 지우지 마세요.
        public TextureReplacerBehaviour(IntPtr ptr) : base(ptr)
        {
        }

        private void Start()
        {
            // =============================================
            // 변경할 텍스처 저장 경로는 다음과 같습니다.
            // BepInEx\plugins\UnityTexture2DReplacer\Texture2D
            // =============================================

            textureDirectory = Path.Combine(
                Paths.PluginPath,
                "UnityTexture2DReplacer",
                "Texture2D"
            );

            Plugin.PluginLog.LogInfo(
                "[TextureReplacer] Directory:"
            );

            Plugin.PluginLog.LogInfo(
                textureDirectory
            );

            // 폴더가 없으면 자동 생성
            if (!Directory.Exists(textureDirectory))
            {
                Directory.CreateDirectory(
                    textureDirectory
                );

                Plugin.PluginLog.LogWarning(
                    "[TextureReplacer] Texture2D directory created."
                );
            }

            LoadReplacementFileList();
        }

        private void Update()
        {
            if (replacementFiles.Count == 0)
                return;

            if (Time.unscaledTime < nextScanTime)
                return;

            nextScanTime =
                Time.unscaledTime + ScanInterval;

            ScanTextures();
        }

        // =================================================
        // Texture2D 폴더의 PNG 목록 읽기
        // =================================================

        private void LoadReplacementFileList()
        {
            replacementFiles.Clear();

            if (!Directory.Exists(textureDirectory))
                return;

            string[] files =
                Directory.GetFiles(
                    textureDirectory,
                    "*.png",
                    SearchOption.TopDirectoryOnly
                );

            foreach (string file in files)
            {
                string textureName =
                    Path.GetFileNameWithoutExtension(file);

                if (string.IsNullOrEmpty(textureName))
                    continue;

                replacementFiles[textureName] =
                    file;

                Plugin.PluginLog.LogInfo(
                    "[TextureReplacer] Registered: " +
                    textureName
                );
            }

            Plugin.PluginLog.LogInfo(
                "[TextureReplacer] " +
                replacementFiles.Count +
                " replacement texture(s) registered."
            );
        }

        // =================================================
        // 현재 로드되어 있는 Texture2D 검색
        // =================================================

        private void ScanTextures()
        {
            UnityEngine.Object[] objects =
                Resources.FindObjectsOfTypeAll(
                    Il2CppInterop.Runtime.Il2CppType
                        .Of<Texture2D>()
                );

            foreach (UnityEngine.Object obj in objects)
            {
                if (obj == null)
                    continue;

                Texture2D texture =
                    obj.TryCast<Texture2D>();

                if (texture == null)
                    continue;

                string textureName =
                    texture.name;

                if (string.IsNullOrEmpty(textureName))
                    continue;

                // 교체 파일이 없는 Texture는 무시
                if (!replacementFiles.TryGetValue(
                        textureName,
                        out string replacementPath))
                {
                    continue;
                }

                int instanceId =
                    texture.GetInstanceID();

                // 동일 Texture 인스턴스는 한 번만
                if (replacedInstances.Contains(instanceId))
                    continue;

                Plugin.PluginLog.LogInfo(
                    "[TextureReplacer] Target found: " +
                    textureName +
                    " (" +
                    texture.width +
                    "x" +
                    texture.height +
                    ")"
                );

                if (ReplaceTexture(
                        texture,
                        replacementPath))
                {
                    replacedInstances.Add(
                        instanceId
                    );
                }
            }
        }

        // =================================================
        // PNG -> Texture2D 교체
        // =================================================

        private bool ReplaceTexture(
            Texture2D target,
            string pngPath)
        {
            try
            {
                byte[] pngData =
                    File.ReadAllBytes(pngPath);

                if (pngData == null ||
                    pngData.Length == 0)
                {
                    Plugin.PluginLog.LogError(
                        "[TextureReplacer] PNG is empty: " +
                        pngPath
                    );

                    return false;
                }

                Il2CppStructArray<byte> il2cppData =
                    new Il2CppStructArray<byte>(
                        pngData.Length
                    );

                for (int i = 0; i < pngData.Length; i++)
                {
                    il2cppData[i] = pngData[i];
                }

                Plugin.PluginLog.LogInfo(
                    "[TextureReplacer] Loading directly into: " +
                    target.name +
                    " | Before=" +
                    target.width +
                    "x" +
                    target.height +
                    " Format=" +
                    target.format
                );

                // 현재 게임이 사용하는 Texture2D 자체를 변경합니다.
                bool loaded =
                    ImageConversion.LoadImage(
                        target,
                        il2cppData,
                        false
                    );

                if (!loaded)
                {
                    Plugin.PluginLog.LogError(
                        "[TextureReplacer] Direct LoadImage failed: " +
                        target.name
                    );

                    return false;
                }

                Plugin.PluginLog.LogWarning(
                    "[TextureReplacer] DIRECT REPLACED: " +
                    target.name +
                    " | After=" +
                    target.width +
                    "x" +
                    target.height +
                    " Format=" +
                    target.format
                );

                return true;
            }
            catch (Exception e)
            {
                Plugin.PluginLog.LogError(
                    "[TextureReplacer] ERROR: " +
                    target.name
                );

                Plugin.PluginLog.LogError(
                    e.ToString()
                );

                return false;
            }
        }
    }
}