//==========================================================================================
//
//		MapSurfer.Styling.Formats.CartoCSS
//		Copyright (c) 2008-2016, MapSurfer.NET
//
//    Authors: Maxim Rylov
//
//==========================================================================================

using MapSurfer.IO.FileTypes;

namespace MapSurfer.Styling.Formats.CartoCSS
{
  public class CartoCSSFileTypeFactory : FileTypeFactory<Map>
  {
    public static readonly FileType<Map> CartoCSSProject;

    static CartoCSSFileTypeFactory()
    {
      CartoCSSProject = new CartoCSSFileType();

      m_fileTypes = new FileType<Map>[] { CartoCSSProject };
    }
  }
}
