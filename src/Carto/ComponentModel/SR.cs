using System.IO;

namespace MapSurfer.ComponentModel
{
  internal sealed class SR : ResourceLoader
  {
    protected override string GetResourceName()
    {
      return "MapSurfer.Styling.CartoCSS.Locales";
    }

    protected override string GetResourceDir()
    {
      return Path.Combine(Path.GetDirectoryName(MapSurfer.Utilities.MSNEnvironment.GetAssemblyLocation(base.GetType().Assembly, true)), "Locales");
    }
  }
}
