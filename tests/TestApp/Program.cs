using MapSurfer;
using MapSurfer.Styling.Formats.CartoCSS;

namespace TestApp
{
  class MainClass
  {
    public static void Main(string[] args)
    {
      CartoCSSFileType cssType = new CartoCSSFileType();
      Map map = cssType.Load(@"..\..\..\projects\road-trip\project.mml", null, null);
    
     // map.Save(@"D:\test.msnm");
    }
  }
}
