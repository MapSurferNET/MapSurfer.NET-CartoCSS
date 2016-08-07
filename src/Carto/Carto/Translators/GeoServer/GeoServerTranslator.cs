//==========================================================================================
//
//		MapSurfer.Styling.Formats.CartoCSS.DatasourceParamerterConverters
//		Copyright (c) 2008-2016, MapSurfer.NET
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

    public override Symbolizer ToSymbolizer(string symbolizer, NodePropertyValue[] properties )
    {
      if (string.IsNullOrEmpty(symbolizer))
        return null;

      if (symbolizer.IndexOf('_') >= 0)
        symbolizer = symbolizer.Split('_')[0];

      switch (symbolizer)
      {
        case "LineSymbolizer":
          return CreateLineSymbolizer(properties);
        case "LinePatternSymbolizer":
          return CreateLinePatternSymbolizer(properties);
        case "PolygonSymbolizer":
          return CreatePolygonSymbolizer(properties);
        case "TextSymbolizer":
          return CreateTextSymbolizer(properties);
        case "PointSymbolizer":
          return CreatePointSymbolizer(properties);
        case "GraphicTextSymbolizer":
          return CreateGraphicTextSymbolizer(properties);
        case "RasterSymbolizer":
          return CreateRasterSymbolizer(properties);
        case "ExtrudedPolygonSymbolizer":
          return CreateExtrudedPolygonSymbolizer(properties);
        default:
          break;
      }

      return null;
    }

    private LineSymbolizer CreateLineSymbolizer(NodePropertyValue[] properties)
    {
      throw new NotImplementedException();
    }

    private LinePatternSymbolizer CreateLinePatternSymbolizer(NodePropertyValue[] properties)
    {
      throw new NotImplementedException();
    }

    private PolygonSymbolizer CreatePolygonSymbolizer(NodePropertyValue[] properties)
    {
      throw new NotImplementedException();
    }

    private TextSymbolizer CreateTextSymbolizer(NodePropertyValue[] properties)
    {
      throw new NotImplementedException();
    }

    private GraphicTextSymbolizer CreateGraphicTextSymbolizer(NodePropertyValue[] properties)
    {
      throw new NotImplementedException();
    }

    private PointSymbolizer CreatePointSymbolizer(NodePropertyValue[] properties)
    {
      throw new NotImplementedException();
    }

    private RasterSymbolizer CreateRasterSymbolizer(NodePropertyValue[] properties)
    {
      throw new NotImplementedException();
    }

    private ExtrudedPolygonSymbolizer CreateExtrudedPolygonSymbolizer(NodePropertyValue[] properties)
    {
      throw new NotImplementedException();
    }

    public override ParameterCollection ToDatasourceParameters(CartoDatasource datasource)
    {
      throw new NotImplementedException();
    }

    public override CartoDatasource ToDatasource(ParameterCollection parameters)
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
    
		public override string ToFilter(string key, string op, string value)
		{
			throw new NotImplementedException();
		}
            
    public override void ProcessStyles(FeatureTypeStyleCollection styles)
    {
    	throw new NotImplementedException();
    }

    public override bool IsFontSetProperty(string value)
    {
      throw new NotImplementedException();
    }
  }
}
