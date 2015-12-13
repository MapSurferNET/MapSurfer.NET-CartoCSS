namespace MapSurfer.Styling.Formats.CartoCSS.Tests
{
  using System.Drawing;

  using NUnit.Framework;

  using MapSurfer.Layers;
  using MapSurfer.Drawing;

  [TestFixture]
  public class ProjectTests
  {
    [Test]
    public void GetThumbnial()  
    {
      CartoCSSFileType cssType = new CartoCSSFileType(); 
      using (Bitmap bitmap = cssType.GetThumbnail(@"..\..\..\projects\road-trip\project.mml"))
      {
        Assert.That(bitmap != null);    
      }
    }

    [Test]
    public void MapProperties()
    {
      CartoCSSFileType cssType = new CartoCSSFileType();
      Map map = cssType.Load(@"..\..\..\projects\road-trip\project.mml", null, null);
    }

    [Test]
    public void FontSets()
    {
      CartoCSSFileType cssType = new CartoCSSFileType();
      Map map = cssType.Load(@"..\..\..\projects\road-trip\project.mml", null, null);

      Assert.AreEqual(3, map.FontSets.Count, "Wrong number of fontsets");

      FontSet fontSet = map.FontSets[0];
      Assert.AreEqual((int)fontSet.FontStyle, (int)FontStyle.Regular);
      Assert.That(fontSet.FontNames.Contains("Arial"));
      Assert.That(fontSet.FontNames.Contains("DejaVu Sans"));

      fontSet = map.FontSets[1];
      Assert.AreEqual((int)fontSet.FontStyle, (int)FontStyle.Regular);
      Assert.That(fontSet.FontNames.Contains("Times New Roman"));
      Assert.That(fontSet.FontNames.Contains("DejaVu Serif"));

      fontSet = map.FontSets[2];
      Assert.AreEqual((int)fontSet.FontStyle, (int)FontStyle.Italic);
      Assert.That(fontSet.FontNames.Contains("Times New Roman"));
      Assert.That(fontSet.FontNames.Contains("DejaVu Serif"));
    }

    [Test]
    public void LayersList()
    {
      CartoCSSFileType cssType = new CartoCSSFileType();
      Map map = cssType.Load(@"..\..\..\projects\road-trip\project.mml", null, null);

      string[] layers = new string[] { "countries", "us-parks", "us-park-line", "lake", "state_line", "country_border_marine", "road" , "rail" , "country_border", "road_na", "country_label", "city", "park_label" };

      foreach (string layerName in layers)
      {
        Layer layer = map.GetLayerByName(layerName);
        Assert.That(layer != null);
      }
    }
  } 
}     
   