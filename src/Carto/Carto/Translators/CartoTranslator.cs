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

using MapSurfer.Styling.Formats.CartoCSS.Translators.Referencers;

namespace MapSurfer.Styling.Formats.CartoCSS.Translators
{
  internal abstract class CartoTranslator : ICartoTranslator
  {
    protected ICartoPropertyReferencer m_referencer;
    protected Logger m_logger;

    public Dictionary<string, FontSet> FontSets { get; set; }

    public abstract Symbolizer ToSymbolizer(string symbolizer, NodePropertyValue[] properties);

    public string GetSymbolizerName(string property)
    {
      return m_referencer.GetSymbolizerName(property);
    }

    public bool IsSymbolizerPropertyValid(string symbolizer, NodePropertyValue property)
    {
      return m_referencer.IsSymbolizerPropertyValid(symbolizer, property);
    }

    public bool HasRequiredProperties(string symbolizer, NodePropertyValue[] properties, ref string missingProperty)
    {
      return m_referencer.HasRequiredProperties(symbolizer, properties, ref missingProperty);
    }

    public abstract bool IsFontSetProperty(string value);

    public string ToCompositingOperation(CompositingMode mode)
    {
      switch (mode)
      {
        case CompositingMode.Clear:
          return "clear";
        case CompositingMode.Source:
          return "src";
        case CompositingMode.Destination:
          return "dst";
        case CompositingMode.SourceOver:
          return "src-over";
        case CompositingMode.DestinationOver:
          return "dst-over";
        case CompositingMode.SourceIn:
          return "src-in";
        case CompositingMode.DestinationIn:
          return "dst-in";
        case CompositingMode.SourceOut:
          return "src-out";
        case CompositingMode.DestinationOut:
          return "dst-out";
        case CompositingMode.SourceATop:
          return "src-atop";
        case CompositingMode.DestinationATop:
          return "dst-atop";
        case CompositingMode.Xor:
          return "xor";
        case CompositingMode.Plus:
          return "plus";
        case CompositingMode.Minus:
          return "minus";
        case CompositingMode.Multiply:
          return "multiply";
        case CompositingMode.Screen:
          return "screen";
        case CompositingMode.Overlay:
          return "overlay";
        case CompositingMode.Darken:
          return "darken";
        case CompositingMode.Lighten:
          return "lighten";
        case CompositingMode.ColorDodge:
          return "color-dodge";
        case CompositingMode.ColorBurn:
          return "color-burn";
        case CompositingMode.HardLight:
          return "hard-light";
        case CompositingMode.SoftLight:
          return "soft-light";
        case CompositingMode.Difference:
          return "difference";
        case CompositingMode.Exclusion:
          return "exclusion";
        case CompositingMode.Contrast:
          return "contrast";
        case CompositingMode.Invert:
          return "invert";
        case CompositingMode.InvertRGB:
          return "invert-rgb";
        case CompositingMode.GrainMerge:
          return "grain-merge";
        case CompositingMode.GrainExtract:
          return "grain-extract";
        case CompositingMode.Hue:
          return "hue";
        case CompositingMode.Saturation:
          return "saturation";
        case CompositingMode.Color:
          return "color";
        case CompositingMode.Value:
          return "value";
        default:
          return "src-over";
      }
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

    public ImageResamplingMode ToImageResamplingMode(string mode)
    {
      switch (mode.ToLower())
      {
        case "near":
          return ImageResamplingMode.NearestNeighbor;
        case "fast":
          return ImageResamplingMode.Low;
        case   "bilinear":
          return ImageResamplingMode.Bilinear;
        case "bilinear8":
          return ImageResamplingMode.HighQualityBilinear;
        case "bicubic":
          return ImageResamplingMode.Bicubic;
          // not supported 
        case "spline16":
        case "spline36":
        case "hanning":
        case "hamming":
        case "hermite":
        case "kaiser":
        case "quadric":
        case "catrom":
        case "gaussian":
        case "bessel":
        case "mitchell":
        case "sinc":
        case "lanczos":
        case "blackman":
          return ImageResamplingMode.Default;
      }

      return ImageResamplingMode.Default;
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

    public abstract ParameterCollection ToDatasourceParameters(CartoDatasource datasource);

    public abstract CartoDatasource ToDatasource(ParameterCollection parameters);

    public abstract string ToImageFilter(string filter);

    public abstract string ToPath(string url);
    
    public abstract string ToFilter(string key, string op, string value);
    
    public abstract void ProcessStyles(FeatureTypeStyleCollection styles);

    public void SetLogger(Logger logger)
    {
      m_logger = logger;
    }
  }
}
