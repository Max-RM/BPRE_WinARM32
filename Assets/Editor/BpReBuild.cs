using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class BpReBuild
{
	private const string Win64Output = "Builds/Win64/BadPiggiesRebooted.exe";
	private const string WsaOutput = "Builds/WSA";
	private const string WsaOutput64 = "Builds/WSA64";
	private const string WsaOutput86 = "Builds/WSA86";

	public static void BuildWin64()
	{
		Build(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64, Win64Output);
	}

	// 32-bit standalone player. Uses the same 32-bit code path (IntPtr.Size == 4, ARM32 splash)
	// as the UWP ARM32 package, so it doubles as a local test target for the ARM32 build.
	public static void BuildWin32()
	{
		Build(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows, "Builds/Win32/BadPiggiesRebooted.exe");
	}

	public static void BuildWSA()
	{
		PlayerSettings.SetScriptingBackend(BuildTargetGroup.WSA, ScriptingImplementation.IL2CPP);
		SetEditorUserBuildSetting("wsaSDK", "UWP");
		SetEditorUserBuildSetting("wsaArchitecture", "ARM");
		SetEditorUserBuildSetting("wsaUWPBuildType", "D3D");
		Build(BuildTargetGroup.WSA, BuildTarget.WSAPlayer, WsaOutput);
	}

	public static void BuildWSA64()
	{
		PlayerSettings.SetScriptingBackend(BuildTargetGroup.WSA, ScriptingImplementation.IL2CPP);
		SetEditorUserBuildSetting("wsaSDK", "UWP");
		SetEditorUserBuildSetting("wsaArchitecture", "x64");
		SetEditorUserBuildSetting("wsaUWPBuildType", "D3D");
		Build(BuildTargetGroup.WSA, BuildTarget.WSAPlayer, WsaOutput64);
	}

	public static void BuildWSA86()
	{
		PlayerSettings.SetScriptingBackend(BuildTargetGroup.WSA, ScriptingImplementation.IL2CPP);
		SetEditorUserBuildSetting("wsaSDK", "UWP");
		SetEditorUserBuildSetting("wsaArchitecture", "x86");
		SetEditorUserBuildSetting("wsaUWPBuildType", "D3D");
		Build(BuildTargetGroup.WSA, BuildTarget.WSAPlayer, WsaOutput86);
	}

	private static void Build(BuildTargetGroup group, BuildTarget target, string outputPath)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? outputPath);
		EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);

		BuildPlayerOptions options = new BuildPlayerOptions
		{
			scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
			locationPathName = outputPath,
			target = target,
			options = BuildOptions.None
		};

		BuildReport report = BuildPipeline.BuildPlayer(options);
		if (report.summary.result != BuildResult.Succeeded)
		{
			throw new Exception(string.Format("Build failed: {0} ({1} errors)", report.summary.result, report.summary.totalErrors));
		}
	}

	private static void SetEditorUserBuildSetting(string propertyName, string valueName)
	{
		PropertyInfo property = typeof(EditorUserBuildSettings).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
		if (property == null || !property.CanWrite)
		{
			return;
		}

		object value = valueName;
		if (property.PropertyType.IsEnum)
		{
			value = Enum.Parse(property.PropertyType, valueName);
		}

		property.SetValue(null, value, null);
	}
}
