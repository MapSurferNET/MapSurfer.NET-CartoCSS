//==========================================================================================
//
//		MapSurfer.Styling.Formats.CartoCSS
//		Copyright (c) 2008-2015, MapSurfer.NET
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
      : base("CartoCSS", new string[] { ".mml" }, FileTypeFlags.ReadSupport)
    {

    }

    protected override Map OnLoad(Stream input, ProgressEventHandler callback, object userInfo)
    {
      object[] objArray = (object[])userInfo;
      string fileName = (string)objArray[0];

//      LogFactory.WriteLogEntry(Logger.Default, string.Format("Loading CartoCSS project from '{0}' ...", fileName), LogEntryType.Information);

      using (StreamReader sr = new StreamReader(input))
      {
        return CartoReader.ReadFromFile(sr.ReadToEnd(), fileName);
      }
    }

    public new Map Load(string fileName, ProgressEventHandler callback, object userInfo)
    {
  //    LogFactory.WriteLogEntry(Logger.Default, string.Format("Loading CartoCSS project from '{0}' ...", fileName), LogEntryType.Information);

      return CartoReader.ReadFromFile(File.ReadAllText(fileName), fileName);
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
