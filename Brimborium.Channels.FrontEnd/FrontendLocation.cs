namespace Brimborium.Channels.FrontEnd;

public static class FrontendLocation {
  public static string GetLocationUI() {
    string projectRoot = GetProjectRoot();
    return System.IO.Path.Combine(projectRoot, "wwwroot", "ui");
    static string GetProjectRoot([System.Runtime.CompilerServices.CallerFilePath] string CallerFilePath = "") {
      return System.IO.Path.GetDirectoryName(CallerFilePath) ?? throw new ArgumentException("do not");
    }
  }
}
