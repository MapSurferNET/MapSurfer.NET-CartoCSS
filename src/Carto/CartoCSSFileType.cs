//==========================================================================================
//
//		MapSurfer.Styling.Formats.CartoCSS
//		Copyright (c) 2008-2016, MapSurfer.NET
//
//    Authors: Maxim Rylov
//
//==========================================================================================
using System.Drawing;
using System.IO;

using MapSurfer.Logging;
using MapSurfer.IO.FileTypes;

namespace MapSurfer.Styling.Formats.CartoCSS
{
  public class CartoCSSFileType : FileType<Map>
  {
    /// <summary>
    /// Initializes a new instance of the class <see cref="MpaSurfer.Styling.Formats.CartoCSS.CartoCSSFileType"/>.
    /// </summary>
    public CartoCSSFileType()
      : base("CartoCSS", new string[] { ".mml", ".yaml", ".yml" }, FileTypeFlags.ReadSupport)
    {

    }

    protected override Map OnLoad(Stream input, IProgressIndicator progress, object userInfo)
    {
      object[] objArray = (object[])userInfo;
      string fileName = (string)objArray[0];

//      LogFactory.WriteLogEntry(Logger.Default, string.Format("Loading CartoCSS project from '{0}' ...", fileName), LogEntryType.Information);

      using (StreamReader sr = new StreamReader(input))
      {
        CartoProject cartoProject = CartoProject.FromFile(sr.ReadToEnd(), Path.GetExtension(fileName));
        return CartoProcessor.GetMap(cartoProject, Path.GetDirectoryName(fileName), progress);
      }
    }

    public new Map Load(string fileName, IProgressIndicator progress, object userInfo)
    {
      //    LogFactory.WriteLogEntry(Logger.Default, string.Format("Loading CartoCSS project from '{0}' ...", fileName), LogEntryType.Information);

      CartoProject cartoProject = CartoProject.FromFile(File.ReadAllText(fileName), Path.GetExtension(fileName));
      return CartoProcessor.GetMap(cartoProject, Path.GetDirectoryName(fileName), progress);
    }

    public override Bitmap GetThumbnail(string fileName)
    {
      string thumbFilename = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(fileName), ".thumb.png"));
      if (File.Exists(thumbFilename))
        return (Bitmap)Bitmap.FromFile(thumbFilename);
      else
        return null;
    }
  }
}
