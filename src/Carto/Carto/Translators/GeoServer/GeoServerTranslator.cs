//==========================================================================================
//
//		MapSurfer.Styling.Formats.CartoCSS.DatasourceParamerterConverters
//		Copyright (c) 2008-2015, MapSurfer.NET
//
//    Authors: Maxim Rylov
//
//==========================================================================================
using System;

using MapSurfer.Configuration;
using MapSurfer.Drawing.Drawing2D;

using MapSurfer.Styling.Formats.CartoCSS.Translators.Referencers;

namespace MapSurfer.Styling.Formats.CartoCSS.Translators.GeoServer
{
  internal class GeoServerTranslator : CartoTranslator
  {
    public GeoServerTranslator()
    {
      m_referencer = new GeoServerPropertyReferencer();
      m_referencer.Prepare();
    }

    public override Symbolizer ToSymbolizer(string symbolizer, string[] properties, string[] values)
    {
      if (string.IsNullOrEmpty(symbolizer))
        return null;

      if (symbolizer.IndexOf('_') >= 0)
        symbolizer = symbolizer.Split('_')[0];

      switch (symbolizer)
      {
        case "LineSymbolizer":
          return CreateLineSymbolizer(properties, values);
        case "LinePatternSymbolizer":
          return CreateLinePatternSymbolizer(properties, values);
        case "PolygonSymbolizer":
          return CreatePolygonSymbolizer(properties, values);
        case "TextSymbolizer":
          return CreateTextSymbolizer(properties, values);
        case "PointSymbolizer":
          return CreatePointSymbolizer(properties, values);
        case "GraphicTextSymbolizer":
          return CreateGraphicTextSymbolizer(properties, values);
        case "RasterSymbolizer":
          return CreateRasterSymbolizer(properties, values);
        case "ExtrudedPolygonSymbolizer":
          return CreateExtrudedPolygonSymbolizer(properties, values);
        default:
          break;
      }

      return null;
    }

    private LineSymbolizer CreateLineSymbolizer(string[] properties, string[] values)
    {
      throw new NotImplementedException();
    }

    private LinePatternSymbolizer CreateLinePatternSymbolizer(string[] properties, string[] values)
    {
      throw new NotImplementedException();
    }

    private PolygonSymbolizer CreatePolygonSymbolizer(string[] properties, string[] values)
    {
      throw new NotImplementedException();
    }

    private TextSymbolizer CreateTextSymbolizer(string[] properties, string[] values)
    {
      throw new NotImplementedException();
    }

    private GraphicTextSymbolizer CreateGraphicTextSymbolizer(string[] properties, string[] values)
    {
      throw new NotImplementedException();
    }

    private PointSymbolizer CreatePointSymbolizer(string[] properties, string[] values)
    {
      throw new NotImplementedException();
    }

    private RasterSymbolizer CreateRasterSymbolizer(string[] properties, string[] values)
    {
      throw new NotImplementedException();
    }

    private ExtrudedPolygonSymbolizer CreateExtrudedPolygonSymbolizer(string[] properties, string[] values)
    {
      throw new NotImplementedException();
    }

    public override ParameterCollection ToDatasourceParameters(CartoLayer layer)
    {
      throw new NotImplementedException();
    }

    public string ToCoordinateSystem(string srs)
    {
      throw new NotImplementedException();
    }

    public override string ToImageFilter(string filter)
    {
      throw new NotImplementedException();
    }

    public override string ToPath(string url)
    {
      throw new NotImplementedException();
    }
            
    public override void ProcessStyles(FeatureTypeStyleCollection styles)
    {
    	throw new NotImplementedException();
    }
  }
}
