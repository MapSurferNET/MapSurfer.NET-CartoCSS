//==========================================================================================
//
//		MapSurfer.Styling.Formats.CartoCSS
//		Copyright (c) 2008-2015, MapSurfer.NET
//
//    Authors: Maxim Rylov
//
//==========================================================================================
using System;

namespace MapSurfer.Styling.Formats.CartoCSS.Translators
{
  using Mapnik;
  using GeoServer;

  internal static class CartoGeneratorConverterFactory
  {
    public static ICartoTranslator CreateTranslator(object name)
    {
      return CreateTranslator(name as string);
    }

    public static ICartoTranslator CreateTranslator(string name)
    {
      if (string.IsNullOrEmpty(name))
        throw new ArgumentNullException("name");

      switch (name.ToLower())
      {
        case "mapnik":
        case "tilemill":
        case "kosmtik":
          return new MapnikTranslator();
        case "geoserver":
          return new GeoServerTranslator();
        default:
          return new MapnikTranslator();
      }

      throw new Exception("Unknown translator with name '" + name +  "'");
    }
  }
}
