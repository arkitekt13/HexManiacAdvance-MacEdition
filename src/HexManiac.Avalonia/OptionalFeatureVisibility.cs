using Avalonia.Input;

namespace HavenSoft.HexManiac.AvaloniaUI;

/// <summary>Temporary UI availability switches; the optional implementations remain compiled.</summary>
public static class OptionalFeatureVisibility {
   public static bool ShowBuiltInEmulator => false;
   public static bool ShowClaudeIntegration => false;

   public static KeyGesture BuiltInEmulatorGesture => ShowBuiltInEmulator ? new KeyGesture(Key.F6) : null;
   public static KeyGesture ClaudeChatGesture => ShowClaudeIntegration ? KeyGesture.Parse("Cmd+Shift+C") : null;
}
