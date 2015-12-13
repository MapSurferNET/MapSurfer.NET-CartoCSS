//==========================================================================================
//
//		MapSurfer.Styling.Formats.CartoCSS
//		Copyright (c) 2008-2015, MapSurfer.NET
//
//    Authors: Maxim Rylov
//
//==========================================================================================
using System;
using System.Collections.Generic;

using MapSurfer.Configuration;
using MapSurfer.Drawing;
using MapSurfer.Drawing.Drawing2D;
using MapSurfer.Logging;
using MapSurfer.Styling;

using MapSurfer.Styling.Formats.CartoCSS.Translators.Referencers;

namespace MapSurfer.Styling.Formats.CartoCSS.Translators
{
  internal abstract class CartoTranslator : ICartoTranslator
  {
    protected ICartoPropertyReferencer m_referencer;

    public Dictionary<string, FontSet> FontSets { get; set; }

    public abstract Symbolizer ToSymbolizer(string symbolizer, string[] properties, string[] values);

    public string GetSymbolizerName(string property)
    {
      return m_referencer.GetSymbolizerName(property);
    }

    public bool HasRequiredProperties(string symbolizer, string[] properties, ref string missingProperty)
    {
      return m_referencer.HasRequiredProperties(symbolizer, properties, ref missingProperty);
    }

    public CompositingMode ToCompositingMode(string comp)
    {
      switch (comp.ToLower())
      {
        case "clear":
          return CompositingMode.Clear;
        case "src":
          return CompositingMode.Source;
        case "dst":
          return CompositingMode.Destination;
        case "src-over":
          return CompositingMode.SourceOver;
        case "dst-over":
          return CompositingMode.DestinationOver;
        case "src-in":
          return CompositingMode.SourceIn;
        case "dst-in":
          return CompositingMode.DestinationIn;
        case "src-out":
          return CompositingMode.SourceOut;
        case "dst-out":
          return CompositingMode.DestinationOut;
        case "src-atop":
          return CompositingMode.SourceATop;
        case "dst-atop":
          return CompositingMode.DestinationATop;
        case "xor":
          return CompositingMode.Xor;
        case "plus":
          return CompositingMode.Plus;
        case "minus":
          return CompositingMode.Minus;
        case "multiply":
          return CompositingMode.Multiply;
        case "screen":
          return CompositingMode.Screen;
        case "overlay":
          return CompositingMode.Overlay;
        case "darken":
          return CompositingMode.Darken;
        case "lighten":
          return CompositingMode.Lighten;
        case "color-dodge":
          return CompositingMode.ColorDodge;
        case "color-burn":
          return CompositingMode.ColorBurn;
        case "hard-light":
          return CompositingMode.HardLight;
        case "soft-light":
          return CompositingMode.SoftLight;
        case "difference":
          return CompositingMode.Difference;
        case "exclusion":
          return CompositingMode.Exclusion;
        case "contrast":
          return CompositingMode.Contrast;
        case "invert":
          return CompositingMode.Invert;
        case "invert-rgb":
          return CompositingMode.InvertRGB;
        case "grain-merge":
          return CompositingMode.GrainMerge;
        case "grain-extract":
          return CompositingMode.GrainExtract;
        case "hue":
          return CompositingMode.Hue;
        case "saturation":
          return CompositingMode.Saturation;
        case "color":
          return CompositingMode.Color;
        case "value":
          return CompositingMode.Value;
        default:
          return CompositingMode.SourceOver;
      }
    }

    public string ToCoordinateSystem(string srs, bool isName = false)
    {
      try
      {
        return SpatialReferenceUtility.ToCoordinateSystem(srs, isName);
      }
      catch (Exception ex)
      {
        LogFactory.WriteLogEntry(Logger.Default, ex);
      }

      return null;
    }

    public abstract ParameterCollection ToDatasourceParameters(CartoLayer layer);

    public abstract string ToImageFilter(string filter);

    public abstract string ToPath(string url);
    
    public abstract void ProcessStyles(FeatureTypeStyleCollection styles);
  }
}
