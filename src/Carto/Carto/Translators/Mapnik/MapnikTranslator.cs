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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;

using dotless.Core.Parser;

using MapSurfer.Configuration;
using MapSurfer.Drawing;
using MapSurfer.Drawing.Imaging;
using MapSurfer.Drawing.Text;
using MapSurfer.Labeling;
using MapSurfer.Styling.Formats.CartoCSS.Exceptions;
using MapSurfer.Logging;

namespace MapSurfer.Styling.Formats.CartoCSS.Translators.Mapnik
{
  internal class MapnikTranslator : CartoTranslator
  {
    private enum SymbolizerType : byte
    {
      Marker,
      Point,
      Shield,
      Text
    }

    private Regex m_regexFunc;
    private Regex m_regexFuncParams;
    private Regex m_colorMapStops;
    private Regex m_regexTextFormat;

    public MapnikTranslator()
    {
      m_referencer = new MapnikPropertyReferencer();
      m_referencer.Prepare();

      m_regexFunc = new Regex(@"(?<func>[^()]+)+(\((.*)\)$)", RegexOptions.Compiled);
      m_regexFuncParams = new Regex(@"([^,]+\(.+?\)')|([^,]+)", RegexOptions.Compiled);
      m_regexTextFormat = new Regex(@"<Format ?(?<args>[0-9a-zA-Z-]*=\s*('|"").*?('|""))*>+(?<text>(.*))</Format>", RegexOptions.Compiled);
      m_colorMapStops = new Regex(@"(stop((?:\((?>[^()]+|\((?<open>)|\)(?<-open>))*\)))*)+", RegexOptions.Compiled);
    }

    public override Symbolizer ToSymbolizer(string symbolizer, NodePropertyValue[] properties)
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

    private ExtrudedPolygonSymbolizer CreateExtrudedPolygonSymbolizer(NodePropertyValue[] properties)
    {
      ExtrudedPolygonSymbolizer symExtPoly = new ExtrudedPolygonSymbolizer();
      symExtPoly.Clip = false;

      NodePropertyValue pv = null;

      try
      {
        int nProps = properties.Length;

        for (int i = 0; i < nProps; i++)
        {
          pv = properties[i];

          switch (pv.Name)
          {
            case "building-fill":
              Color clr = ColorUtility.FromHtml(pv.Value);
              symExtPoly.FacesColor = clr;
              symExtPoly.TopColor = clr;
              break;
            case "building-fill-opacity":
              symExtPoly.FillOpacity = Convert.ToSingle(pv.Value);
              break;
            case "building-height":
              symExtPoly.HeightExpression = ToExpression(pv.Value);
              break;
          }
        }
      }
      catch (Exception ex)
      {
        ThrowParsingException(ex, pv);
      }

      return symExtPoly;
    }

    private RasterSymbolizer CreateRasterSymbolizer(NodePropertyValue[] properties)
    {
      RasterSymbolizer symRaster = new RasterSymbolizer();

      NodePropertyValue pv = null;

      try
      {
        int nProps = properties.Length;
        for (int i = 0; i < nProps; i++)
        {
          pv = properties[i];
          switch (pv.Name)
          {
            case "raster-opacity":
              symRaster.Opacity = Convert.ToSingle(pv.Value);
              break;
            case "raster-filter-factor":
              UnsupportedProperty(pv);
              break;
            case "raster-scaling":
              symRaster.InterpolationMode = ToImageResamplingMode(pv.Value);
              break;
            case "raster-mesh-size":
              UnsupportedProperty(pv);
              break;
            case "raster-comp-op":
              symRaster.CompositingMode = ToCompositingMode(pv.Value);
              break;
            case "raster-colorizer-default-mode":
              symRaster.ColorMap.DefaultMode = ToColorMapMode(pv.Value);
              break;
            case "raster-colorizer-default-color":
              symRaster.ColorMap.DefaultColor = ColorUtility.FromHtml(pv.Value);
              break;
            case "raster-colorizer-epsilon":
              symRaster.ColorMap.Epsilon = Convert.ToSingle(pv.Value);
              break;
            case "raster-colorizer-stops":
              MatchCollection stopsMatch = m_colorMapStops.Matches(pv.Value.ToLower());
              if (stopsMatch.Count > 0)
              {
                symRaster.ShadedRelief.ShaderType = ShaderType.Custom;

                foreach (Match match in stopsMatch)
                {
                  ColorMapEntry entry = new ColorMapEntry();
                  MatchCollection args = m_regexFuncParams.Matches(match.Groups[2].Value.Trim(new char[] { '(', ')' }));

                  for (int a = 0; a < args.Count; a++)
                  {
                    Match arg = args[a];
                    string av = arg.Value.Trim();
                    if (a == 0)
                      entry.Quantity = Convert.ToDouble(av);
                    else if (a == 1)
                      entry.Color = ColorUtility.FromHtml(av);
                    else if (a == 2)
                      entry.Mode = ToColorMapMode(av);
                  }

                  symRaster.ColorMap.Entries.Add(entry);
                }
              }
              break;
          }
        }
      }
      catch (Exception ex)
      {
        ThrowParsingException(ex, pv);
      }

      return symRaster;
    }

    private GraphicTextSymbolizer CreateGraphicTextSymbolizer(NodePropertyValue[] properties)
    {
      GraphicTextSymbolizer symText = new GraphicTextSymbolizer();
      symText.Clip = true;
      symText.TextLayout.Alignment = TextAlignment.CenterAligned;
      ExternalGraphicSymbol gsImage = new ExternalGraphicSymbol();
      symText.Graphic.GraphicSymbols.Add(gsImage);

      GeometryTransformInfo geomTrans = new GeometryTransformInfo();

      LabelPlacementInfo lpi = new LabelPlacementInfo();
      lpi.Placement = "point";
      lpi.Symbolizer = (int)SymbolizerType.Shield;

      int blockIndex = -1;
      TextLayoutBlock textBlock = new TextLayoutBlock();
      string textTransform = null;
      int nProps = properties.Length;
      float text_dx = 0F, text_dy = 0F;

      NodePropertyValue pv = null;

      try
      {
        for (int i = 0; i < nProps; i++)
        {
          pv = properties[i];

          switch (pv.Name)
          {
            case "shield-name":
              if (pv.Value != null)
              {
                string textExpr = ToExpression(pv.Value);
                if (!string.IsNullOrEmpty(textTransform))
                  textExpr = GetTextTransform(textExpr, textTransform);
                textBlock.TextExpression = textExpr;

                blockIndex++;
                if (blockIndex > 0)
                  textBlock = new TextLayoutBlock();

                symText.TextLayout.Blocks.Add(textBlock);
              }
              break;
            case "shield-face-name":
              SetFontName(textBlock, pv.Value);
              break;
            case "shield-file":
              gsImage.Path = ToPath(pv.Value);
              break;
            case "shield-text-transform":
              if (string.IsNullOrEmpty(textBlock.TextExpression))
                textTransform = pv.Value;
              else
                textBlock.TextExpression = GetTextTransform(textBlock.TextExpression, pv.Value);
              break;
            case "shield-fill":
              textBlock.TextFormat.TextStyle.Color = ColorUtility.FromHtml(pv.Value);
              break;
            case "shield-text-opacity":
              textBlock.TextFormat.TextStyle.Opacity = Convert.ToSingle(pv.Value);
              break;
            case "shield-opacity":
              gsImage.Opacity = Convert.ToSingle(pv.Value);
              break;
            case "shield-size":
              textBlock.TextFormat.TextStyle.Font.Size = Convert.ToSingle(pv.Value);
              break;
            case "shield-halo-radius":
              textBlock.TextFormat.TextStyle.Halo.Radius = Convert.ToSingle(pv.Value);
              break;
            case "shield-halo-fill":
              Color clr = ColorUtility.FromHtml(pv.Value);
              textBlock.TextFormat.TextStyle.Halo.Color = clr;
              if (clr.A != 255)
                textBlock.TextFormat.TextStyle.Halo.Opacity = clr.A / 255.0F;
              break;
            case "shield-halo-opacity":
              textBlock.TextFormat.TextStyle.Halo.Opacity = Convert.ToSingle(pv.Value);
              break;
            case "shield-clip":
              symText.Clip = Convert.ToBoolean(pv.Value);
              break;
            case "shield-horizontal-alignment":
              UnsupportedProperty(pv);
              break;
            case "shield-vertical-alignment":
              UnsupportedProperty(pv);
              break;
            case "shield-justify-alignment":
              UnsupportedProperty(pv);
              break;
            case "shield-transform":
              UnsupportedProperty(pv);
              break;
            case "shield-wrap-width":
              textBlock.TextFormat.TextWrapping.MaxWidth = Convert.ToUInt32(pv.Value);
              textBlock.TextFormat.TextWrapping.Mode = Drawing.Text.TextWrapMode.WrapByMaxWidthPixels;
              break;
            case "shield-wrap-before":
              TextWrapping tw = textBlock.TextFormat.TextWrapping;
              if (tw.Characters == null)
                tw.Characters = new WrapCharacter[] { new WrapCharacter() };

              tw.Characters[0].WrapType = CharacterWrapType.Before;
              break;
            case "shield-wrap-character":
              TextWrapping tw2 = textBlock.TextFormat.TextWrapping;
              if (tw2.Characters == null)
                tw2.Characters = new WrapCharacter[] { new WrapCharacter() };

              tw2.Characters[0].Character = pv.Value;
              break;
            case "shield-character-spacing":
              textBlock.TextFormat.TextSpacing.CharacterSpacing = Convert.ToSingle(pv.Value);
              break;
            case "shield-line-spacing":
              textBlock.TextFormat.TextSpacing.Leading = Convert.ToSingle(pv.Value);
              break;
            case "shield-allow-overlap":
              symText.LabelBehaviour.AllowOverlap = Convert.ToBoolean(pv.Value);
              break;
            case "shield-min-distance":
              symText.LabelBehaviour.CollisionMeasures.Add(string.Format("MinimumDistance({0})", pv.Value));
              break;
            case "shield-avoid-edges":
              symText.LabelBehaviour.AvoidEdges = Convert.ToBoolean(pv.Value);
              break;
            case "shield-spacing":
              lpi.Properties.Add(new KeyValuePair<string, string>(pv.Name, pv.Value));
              break;
            case "shield-min-padding":
              UnsupportedProperty(pv);
              break;
            case "shield-placement-type":
              lpi.Properties.Add(new KeyValuePair<string, string>(pv.Name, pv.Value));
              break;
            case "shield-placements":
              lpi.Properties.Add(new KeyValuePair<string, string>(pv.Name, pv.Value));
              break;
            case "shield-text-dx":
              text_dx = Convert.ToSingle(pv.Value);
              break;
            case "shield-text-dy":
              text_dy = Convert.ToSingle(pv.Value);
              break;
            case "shield-dx":
              lpi.Properties.Add(new KeyValuePair<string, string>(pv.Name, pv.Value));
              break;
            case "shield-dy":
              lpi.Properties.Add(new KeyValuePair<string, string>(pv.Name, pv.Value));
              break;
            case "shield-comp-op":
              UnsupportedProperty(pv);
              break;
            case "shield-unlock-image":
              UnsupportedProperty(pv);
              break;
            case "shield-simplify":
              geomTrans.Simplify = pv.Value;
              break;
            case "shield-simplify-algorithm":
              geomTrans.SimplifyAlgorithm = pv.Value;
              break;
            case "shield-smooth":
              geomTrans.Smooth = pv.Value;
              break;
            default:
              break;
          }
        }
      }
      catch (Exception ex)
      {
        ThrowParsingException(ex, pv);
      }

    //  if (text_dx != 0F || text_dy != 0F)
    //    symText.Graphic.Size = new SizeF(text_dx, text_dy);

      ApplyTextBlockFormat(symText.TextLayout);
      symText.LabelPlacement = CreateLabelPlacement(lpi);
      if ("point".Equals(lpi.Placement))
        geomTrans.DisplacementX = geomTrans.DisplacementY = string.Empty;
      symText.GeometryExpression = ToGeometryExpression(geomTrans);

      return symText;
    }

    private LinePatternSymbolizer CreateLinePatternSymbolizer(NodePropertyValue[] properties)
    {
      LinePatternSymbolizer symLinePattern = new LinePatternSymbolizer();
      symLinePattern.Clip = true;
      symLinePattern.LabelBehaviour.AllowOverlap = true;
      symLinePattern.LabelBehaviour.CollisionDetectable = false;

      GeometryTransformInfo geomTrans = new GeometryTransformInfo();
      int nProps = properties.Length;

      NodePropertyValue pv = null;

      try
      {
        for (int i = 0; i < nProps; i++)
        {
          pv = properties[i];

          switch (pv.Name)
          {
            case "line-pattern-file":
              symLinePattern.FileName = ToPath(pv.Value);
              break;
            case "line-pattern-clip":
              symLinePattern.Clip = Convert.ToBoolean(pv.Value);
              break;
            case "line-pattern-simplify":
              geomTrans.Simplify = pv.Value;
              break;
            case "line-pattern-simplify-algorithm":
              geomTrans.SimplifyAlgorithm = pv.Value;
              break;
            case "line-pattern-smooth":
              geomTrans.Smooth = pv.Value;
              break;
            case "line-pattern-offset":
              geomTrans.Offset = pv.Value;
              break;
            case "line-pattern-geometry-transform":
              geomTrans.GeometryTransform = pv.Value;
              break;
            case "line-pattern-comp-op":
             // UnsupportedProperty(pv.Name);
              AddProperty(symLinePattern, "comp-op", pv.Value);
              break;
          }
        }
      }
      catch (Exception ex)
      {
        ThrowParsingException(ex, pv);
      }

      symLinePattern.GeometryExpression = ToGeometryExpression(geomTrans);

      return symLinePattern;
    }

    private TextSymbolizer CreateTextSymbolizer(NodePropertyValue[] properties)
    {
      TextSymbolizer symText = new TextSymbolizer();
      symText.Clip = true;
      symText.TextLayout.Alignment = TextAlignment.CenterAligned;

      GeometryTransformInfo geomTrans = new GeometryTransformInfo();
      LabelPlacementInfo lpi = new LabelPlacementInfo();
      lpi.Symbolizer = (int)SymbolizerType.Text;
      TextLayoutBlock textBlock = new TextLayoutBlock();
      string textTransform = null;
      int blockIndex = -1;
      int nProps = properties.Length;

      NodePropertyValue pv = null;

      try
      {
        for (int i = 0; i < nProps; i++)
        {
          pv = properties[i];

          switch (pv.Name)
          {
            case "text-name":
              if (pv.Value != null)
              {
                string textExpr = ToExpression(pv.Value);
                if (!string.IsNullOrEmpty(textTransform))
                  textExpr = GetTextTransform(textExpr, textTransform);
                textBlock.TextExpression = textExpr;

                // TODO
                if (textBlock.TextExpression == string.Empty)
                  symText.Enabled = false;

                blockIndex++;
                if (blockIndex > 0)
                  textBlock = new TextLayoutBlock();

                symText.TextLayout.Blocks.Add(textBlock);
              }
              break;
            case "text-face-name":
              SetFontName(textBlock, pv.Value);
              break;
            case "text-transform":
              if (string.IsNullOrEmpty(textBlock.TextExpression))
                textTransform = pv.Value;
              else
                textBlock.TextExpression = GetTextTransform(textBlock.TextExpression, pv.Value);
              break;
            case "text-fill":
              Color clr = ColorUtility.FromHtml(pv.Value);
              textBlock.TextFormat.TextStyle.Color = clr;
              if (clr.A != 255)
                textBlock.TextFormat.TextStyle.Opacity = clr.A / 255.0F;
              break;
            case "text-opacity":
              textBlock.TextFormat.TextStyle.Opacity = Convert.ToSingle(pv.Value);
              break;
            case "text-size":
              textBlock.TextFormat.TextStyle.Font.Size = Convert.ToSingle(pv.Value);
              break;
            case "text-halo-radius":
              textBlock.TextFormat.TextStyle.Halo.Radius = Convert.ToSingle(pv.Value);
              break;
            case "text-halo-fill":
              Color clr2 = ColorUtility.FromHtml(pv.Value);
              textBlock.TextFormat.TextStyle.Halo.Color = clr2;
              if (clr2.A != 255)
                textBlock.TextFormat.TextStyle.Halo.Opacity = clr2.A / 255F;
              break;
            case "text-halo-opacity": // This property does not exist in the specification.
              textBlock.TextFormat.TextStyle.Halo.Opacity = Convert.ToSingle(pv.Value);
              break;
            case "text-halo-rasterizer":
              // TODO
              break;
            case "text-ratio":
              //TODO
              break;
            case "text-clip":
              symText.Clip = Convert.ToBoolean(pv.Value);
              break;
            case "text-align":
              textBlock.TextFormat.TextAlignment = ToTextAlignment(pv.Value);
              break;
            case "text-horizontal-alignment":
              // TODO
              break;
            case "text-vertical-alignment":
              // TODO
              break;
            case "text-wrap-width":
              textBlock.TextFormat.TextWrapping.MaxWidth = Convert.ToUInt32(pv.Value);
              textBlock.TextFormat.TextWrapping.Mode = Drawing.Text.TextWrapMode.WrapByMaxWidthPixels;
              break;
            case "text-wrap-before":
              TextWrapping tw = textBlock.TextFormat.TextWrapping;
              if (tw.Characters == null || tw.Characters.Length == 0)
                tw.Characters = new WrapCharacter[] { new WrapCharacter() };
              tw.Characters[0].WrapType = CharacterWrapType.Before;
              break;
            case "text-wrap-character":
              TextWrapping tw2 = textBlock.TextFormat.TextWrapping;
              if (tw2.Characters == null || tw2.Characters.Length == 0)
                tw2.Characters = new WrapCharacter[] { new WrapCharacter() };
              tw2.Characters[0].Character = pv.Value;
              break;
            case "text-character-spacing":
              textBlock.TextFormat.TextSpacing.CharacterSpacing = Convert.ToSingle(pv.Value);
              break;
            case "text-line-spacing":
              textBlock.TextFormat.TextSpacing.Leading = Convert.ToSingle(pv.Value);
              break;
            case "text-allow-overlap":
              symText.LabelBehaviour.AllowOverlap = Convert.ToBoolean(pv.Value);
              break;
            case "text-min-distance":
              symText.LabelBehaviour.CollisionMeasures.Add(string.Format("MinimumDistance({0})", pv.Value));
              break;
            case "text-avoid-edges":
              symText.LabelBehaviour.AvoidEdges = Convert.ToBoolean(pv.Value);
              break;
            case "text-spacing":
              lpi.Properties.Add(new KeyValuePair<string, string>(pv.Name, pv.Value));
              break;
            case "text-max-char-angle-delta":
              lpi.Properties.Add(new KeyValuePair<string, string>(pv.Name, pv.Value));
              break;
            case "text-label-position-tolerance":
              lpi.Properties.Add(new KeyValuePair<string, string>(pv.Name, pv.Value));
              break;
            case "text-min-padding":
              UnsupportedProperty(pv);
              break;
            case "text-min-path-length":
              lpi.Properties.Add(new KeyValuePair<string, string>(pv.Name, pv.Value));
              break;
            case "text-orientation":
              lpi.Properties.Add(new KeyValuePair<string, string>(pv.Name, pv.Value));
              break;
            case "text-placement":
              lpi.Placement = pv.Value;
              if (lpi.Placement == "line")
                geomTrans.OffsetCurve = true;
              break;
            case "text-placement-type":
              lpi.Properties.Add(new KeyValuePair<string, string>(pv.Name, pv.Value));
              break;
            case "text-placements":
              lpi.Properties.Add(new KeyValuePair<string, string>(pv.Name, pv.Value));
              break;
            case "text-dx":
              geomTrans.DisplacementX = pv.Value;
              lpi.Properties.Add(new KeyValuePair<string, string>(pv.Name, pv.Value));
              break;
            case "text-dy":
              geomTrans.DisplacementY = pv.Value;
              lpi.Properties.Add(new KeyValuePair<string, string>(pv.Name, pv.Value));
              break;
            case "text-comp-op":
              UnsupportedProperty(pv);
              break;
            case "text-simplify":
              geomTrans.Simplify = pv.Value;
              break;
            case "text-simplify-algorithm":
              geomTrans.SimplifyAlgorithm = pv.Value;
              break;
            case "text-smooth":
              geomTrans.Smooth = pv.Value;
              break;
            default:
              break;
          }
        }
      }
      catch (Exception ex)
      {
        ThrowParsingException(ex, pv);
      }

      ApplyTextBlockFormat(symText.TextLayout);
      symText.LabelPlacement = CreateLabelPlacement(lpi);
      if ("point".Equals(lpi.Placement))
        geomTrans.DisplacementX = geomTrans.DisplacementY = string.Empty;
      symText.GeometryExpression = ToGeometryExpression(geomTrans);

      return symText;
    }

    private PointSymbolizer CreatePointSymbolizer(NodePropertyValue[] properties)
    {
      PointSymbolizer symPoint = new PointSymbolizer();

      int pointType = properties[0].Name.StartsWith("point-") ? 0 : 1;
      bool isArrow = HasPropertyValue(properties, "marker-file", "shape://arrow");

      if (pointType == 1 && isArrow)
        pointType = 2;

      ExternalGraphicSymbol pointSymbol = null;
      MarkGraphicSymbol markSymbol = null;
      GlyphGraphicSymbol glyphSymbol = null;

      LabelPlacementInfo lpi = new LabelPlacementInfo();
      GeometryTransformInfo geomTrans = (pointType == 0)? null :new GeometryTransformInfo();

      if (pointType == 0 || (!isArrow && HasProperty(properties ,"marker-file")))
      {
        pointSymbol = new ExternalGraphicSymbol();
        symPoint.Graphic.GraphicSymbols.Add(pointSymbol);
        lpi.Placement = "centroid";
        lpi.Symbolizer = (int)SymbolizerType.Point;
      }
      else if (pointType == 1)
      {
        markSymbol = new MarkGraphicSymbol();
        symPoint.Graphic.GraphicSymbols.Add(markSymbol);
        lpi.Symbolizer = (int)SymbolizerType.Marker;
      }
      else
      {
        glyphSymbol = new GlyphGraphicSymbol();
        symPoint.Graphic.GraphicSymbols.Add(glyphSymbol);
        lpi.Symbolizer = (int)SymbolizerType.Marker;
      }

      float width = 10F, height = 0F;
      NodePropertyValue pv = null;

      try
      {
        int nProps = properties.Length;

        for (int i = 0; i < nProps; i++)
        {
          pv = properties[i];

          if (pointType == 0)
          {
            switch (pv.Name)
            {
              case "point-file":
                pointSymbol.Path = ToPath(pv.Value);
                break;
              case "point-allow-overlap":
                symPoint.LabelBehaviour.AllowOverlap = Convert.ToBoolean(pv.Value);
                break;
              case "point-ignore-placement":
                symPoint.LabelBehaviour.CollisionDetectable = !Convert.ToBoolean(pv.Value);
                break;
              case "point-opacity":
                pointSymbol.Opacity = Convert.ToSingle(pv.Value);
                break;
              case "point-placement":
                lpi.Placement = pv.Value;
                break;
              case "point-transform":
                UnsupportedProperty(pv);
                break;
              case "point-comp-op":
                //UnsupportedProperty(pv.Name);
                AddProperty(symPoint, "comp-op", pv.Value);
                break;
            }
          }
          else
          {
            switch (pv.Name)
            {
              case "marker-file":
                if (pointSymbol != null)
                  pointSymbol.Path = ToPath(pv.Value);
                else if (glyphSymbol != null)
                {
                  if (FontUtility.IsFontInstalled("DejaVu Sans"))
                  {
                    glyphSymbol.TextStyle.Font.Name = "DejaVu Sans";
                    glyphSymbol.TextStyle.Font.Stretch = 1.2F;
                    glyphSymbol.Unicode = 8594;
                  }
                  else if (FontUtility.IsFontInstalled("Segoe UI"))
                  {
                    glyphSymbol.TextStyle.Font.Name = "Segoe UI";
                    glyphSymbol.Unicode = 2192;
                  }

                }
                break;
              case "marker-opacity":
                float value = Convert.ToSingle(pv.Value);
                if (pointSymbol != null)
                  pointSymbol.Opacity = value;
                else if (symPoint.Graphic != null)
                  symPoint.Graphic.Opacity /*markSymbol.Opacity*/ = value;

                if (value == 0.0F)
                  symPoint.Enabled = false;
                break;
              case "marker-line-color":
                Color clr = ColorUtility.FromHtml(pv.Value);
                if (markSymbol != null)
                  markSymbol.Stroke.Color = clr;
                else if (glyphSymbol != null)
                  glyphSymbol.TextStyle.Halo.Color = clr;
                break;
              case "marker-line-width":
                if (markSymbol != null)
                  markSymbol.Stroke.Width = Convert.ToSingle(pv.Value);
                else if (glyphSymbol != null)
                  glyphSymbol.TextStyle.Halo.Radius = Convert.ToSingle(pv.Value);
                break;
              case "marker-line-opacity":
                if (markSymbol != null)
                  markSymbol.Stroke.Opacity = Convert.ToSingle(pv.Value);
                else if (glyphSymbol != null)
                  glyphSymbol.TextStyle.Halo.Opacity = Convert.ToSingle(pv.Value);
                break;
              case "marker-placement":
                lpi.Placement = pv.Value;
                if ((glyphSymbol != null || markSymbol != null) && lpi.Placement.Equals("line"))
                  lpi.Placement = "line-point-pattern";
                break;
              case "marker-multi-policy":
                // TODO
                break;
              case "marker-type":
                markSymbol.WellKnownName = pv.Value;
                break;
              case "marker-width":
                width = Convert.ToSingle(pv.Value);
                if (glyphSymbol != null)
                  glyphSymbol.TextStyle.Font.Size = width;
                break;
              case "marker-height":
                height = Convert.ToSingle(pv.Value);
                break;
              case "marker-fill-opacity":
                if (markSymbol != null)
                  markSymbol.Fill.Opacity = Convert.ToSingle(pv.Value);
                else if (glyphSymbol != null)
                  glyphSymbol.TextStyle.Opacity = Convert.ToSingle(pv.Value);
                break;
              case "marker-fill":
                clr = ColorUtility.FromHtml(pv.Value);
                if (markSymbol != null)
                  markSymbol.Fill.Color = clr;
                else if (glyphSymbol != null)
                  glyphSymbol.TextStyle.Color = clr;
                break;
              case "marker-allow-overlap":
                symPoint.LabelBehaviour.AllowOverlap = Convert.ToBoolean(pv.Value);
                break;
              case "marker-ignore-placement":
                symPoint.LabelBehaviour.CollisionDetectable = !Convert.ToBoolean(pv.Value);
                break;
              case "marker-spacing":
                lpi.Properties.Add(new KeyValuePair<string, string>(pv.Name, pv.Value));
                break;
              case "marker-max-error":
                // TODO
                break;
              case "marker-transform":
                ApplySVGTransformation(symPoint.Graphic, pv.Value);
                break;
              case "marker-clip":
                symPoint.Clip = Convert.ToBoolean(pv.Value);
                break;
              case "marker-pattern-simplify":
                geomTrans.Simplify = pv.Value;
                break;
              case "marker-pattern-simplify-algorithm":
                geomTrans.SimplifyAlgorithm = pv.Value;
                break;
              case "marker-pattern-smooth":
                geomTrans.Smooth = pv.Value;
                break;
              case "marker-geometry-transform":
                UnsupportedProperty(pv);
                break;
              case "marker-comp-op":
                UnsupportedProperty(pv);
                AddProperty(symPoint, "comp-op", pv.Value);
                break;
            }
          }
        }
      }
      catch (Exception ex)
      {
        ThrowParsingException(ex, pv);
      }

      if (height == 0F)
        height = width;

      if (markSymbol != null)
        markSymbol.Size = new SizeF(width, height);
      else if (glyphSymbol != null)
        symPoint.Graphic.Size = new SizeF(width, 6);

      symPoint.LabelPlacement = CreateLabelPlacement(lpi);

      return symPoint;
    }

    private PolygonSymbolizer CreatePolygonSymbolizer(NodePropertyValue[] properties)
    {
      PolygonSymbolizer symPolygon = new PolygonSymbolizer();
      symPolygon.Fill.Color = Color.Gray;
      symPolygon.Fill.Outlined = false;
      symPolygon.Clip = true;

      GraphicFill gFill = properties[0].Name.StartsWith("polygon-pattern-") ? new GraphicFill() : null;
      if (gFill != null)
      {
        symPolygon.Fill.GraphicFill = gFill;
        symPolygon.Fill.Opacity = 0.0F;
      }

      GeometryTransformInfo geomTrans = new GeometryTransformInfo();

      NodePropertyValue pv = null;

      try
      {
        int nProps = properties.Length;

        for (int i = 0; i < nProps; i++)
        {
          pv = properties[i];

          if (gFill != null)
          {
            switch (pv.Name)
            {
              case "polygon-pattern-file":
                ExternalGraphicSymbol egs = new ExternalGraphicSymbol();
                egs.Path = ToPath(pv.Value);
                gFill.GraphicSymbols.Add(egs);
                break;
              case "polygon-pattern-opacity":
                gFill.Opacity = Convert.ToSingle(pv.Value);
                break;
              case "polygon-pattern-comp-op":
                AddProperty(symPolygon, "comp-op", pv.Value);
                break;
            }
          }
          else
          {
            switch (pv.Name)
            {
              case "polygon-fill":
                Color clr = ColorUtility.FromHtml(pv.Value);
                symPolygon.Fill.Color = clr;
                if (clr.A != 255)
                  symPolygon.Fill.Opacity = clr.A / 255.0F;
                break;
              case "polygon-opacity":
                symPolygon.Fill.Opacity = Convert.ToSingle(pv.Value);
                break;
              case "polygon-gamma":
                UnsupportedProperty(pv);
                break;
              case "polygon-gamma-method":
                UnsupportedProperty(pv);
                break;
              case "polygon-clip":
                symPolygon.Clip = Convert.ToBoolean(pv.Value);
                break;
              case "polygon-comp-op":
                UnsupportedProperty(pv);
                AddProperty(symPolygon, "comp-op", pv.Value);
                break;
              case "polygon-simplify":
                geomTrans.Simplify = pv.Value;
                break;
              case "polygon-simplify-algorithm":
                geomTrans.SimplifyAlgorithm = pv.Value;
                break;
              case "polygon-smooth":
                geomTrans.Smooth = pv.Value;
                break;
              case "polygon-offset":
                geomTrans.Offset = pv.Value;
                break;
              case "polygon-geometry-transform":
                geomTrans.GeometryTransform = pv.Value;
                break;
            }
          }
        }
      }
      catch (Exception ex)
      {
        ThrowParsingException(ex, pv);
      }

      symPolygon.GeometryExpression = ToGeometryExpression(geomTrans);

      return symPolygon;
    }

    private LineSymbolizer CreateLineSymbolizer(NodePropertyValue[] properties)
    {
      LineSymbolizer symLine = new LineSymbolizer();
      symLine.Stroke.LineCap = System.Drawing.Drawing2D.LineCap.Flat;
      symLine.Stroke.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
      symLine.Clip = true;

      GeometryTransformInfo geomTrans = new GeometryTransformInfo();
      NodePropertyValue pv = null;

      try
      {
        int nProps = properties.Length;
        for (int i = 0; i < nProps; i++)
        {
          pv = properties[i];

          switch (pv.Name)
          {
            case "line-width":
              symLine.Stroke.Width = Convert.ToSingle(pv.Value);
              break;
            case "line-color":
              Color clr = ColorUtility.FromHtml(pv.Value);
              symLine.Stroke.Color = clr;
              if (clr.A != 255)
                symLine.Stroke.Opacity = clr.A / 255.0F;
              break;
            case "line-opacity":
              symLine.Stroke.Opacity = Convert.ToSingle(pv.Value);
              break;
            case "line-join":
              symLine.Stroke.LineJoin = ToLineJoin(pv.Value);
              break;
            case "line-cap":
              symLine.Stroke.LineCap = ToLineCap(pv.Value);
              break;
            case "line-dasharray":
              symLine.Stroke.DashArray = ConvertUtility.ToFloatArray(pv.Value);
              break;
            case "line-miterlimit":
              symLine.Stroke.MiterLimit = Convert.ToSingle(pv.Value);
              break;
            case "line-dash-offset":
              symLine.Stroke.DashOffset = Convert.ToSingle(pv.Value);
              break;
            case "line-comp-op":
              UnsupportedProperty(pv);
              AddProperty(symLine, "comp-op", pv.Value);
              break;
            case "line-rasterizer":
              UnsupportedProperty(pv);
              break;
            case "line-simplify":
              geomTrans.Simplify = pv.Value;
              break;
            case "line-simplify-algorithm":
              geomTrans.SimplifyAlgorithm = pv.Value;
              break;
            case "line-smooth":
              geomTrans.Smooth = pv.Value;
              break;
            case "line-offset":
              geomTrans.Offset = pv.Value;
              break;
            case "line-geometry-transform":
              geomTrans.GeometryTransform = pv.Value;
              break;
          }
        }
      }
      catch(Exception ex)
      {
        ThrowParsingException(ex, pv);
      }

      symLine.GeometryExpression = ToGeometryExpression(geomTrans);

      return symLine;
    }

    private Exception ThrowParsingException(Exception ex, NodePropertyValue pv)
    {
      throw new ParsingException(string.Format("Invalid value '{0}' for property '{1}'.", pv.Value, pv.Name) + " " + ex.Message, pv.Location.FileName, Zone.GetLineNumber(pv.Location));
    }

    private void AddProperty(Symbolizer sym, string key, string value)
    {
      Dictionary<string, string> props = (Dictionary<string, string>)sym.Tag;
      if (props == null)
        props = new Dictionary<string, string>();

      if (!props.ContainsKey(key))
        props.Add(key, value);

      sym.Tag = props;
    }

    private LabelPlacement CreateLabelPlacement(LabelPlacementInfo lpi)
    {
      LabelPlacement result = null;

      switch (lpi.Placement)
      {
        case "point":
        case "interior":
        case "centroid":
          PointPlacement pp = new PointPlacement();
          if (lpi.Placement == "interior")
            pp.PointOnSurface = true;

          float dx = 0F, dy = 0F;

          if (lpi.Symbolizer == (int)SymbolizerType.Text)
          {
            pp.Alignments = GetDefaultLabelAlignment(false);

            foreach (KeyValuePair<string, string> kv in lpi.Properties)
            {
              switch (kv.Key)
              {
                case "text-dx":
                  dx = Convert.ToSingle(kv.Value);
                  break;
                case "text-dy":
                  dy = Convert.ToSingle(kv.Value);
                  break;
                case "text-orientation":
                  pp.AngleExpression = ToExpression(kv.Value);
                  break;
                case "text-placements":
                  string placementType = lpi.GetPropertyValue("text-placement-type");
                  if (placementType == "simple")
                    pp.Alignments = ToLabelAlignments(kv.Value);
                  else
                    pp.Alignments = GetDefaultLabelAlignment(false);
                  break;
                case "text-placement-type":
                  break;
              }
            }
          }
          else if (lpi.Symbolizer == (int)SymbolizerType.Shield)
          {
            pp.Alignments = GetDefaultLabelAlignment(true);

            float text_dx = 0F, text_dy = 0F;

            foreach (KeyValuePair<string, string> kv in lpi.Properties)
            {
              switch (kv.Key)
              {
                case "shield-dx":
                  dx = Convert.ToSingle(kv.Value);
                  break;
                case "shield-dy":
                  dy = Convert.ToSingle(kv.Value);
                  break;
                case "shield-placements":
                  string placementType = lpi.GetPropertyValue("shield-placement-type");
                  if (placementType == "simple")
                    pp.Alignments = ToLabelAlignments(kv.Value);
                  else
                    pp.Alignments = GetDefaultLabelAlignment(true);
                  break;
                case "shield-placement-type":
                  break;
                case "shield-text-dx":
                 // text_dx = Convert.ToSingle(kv.Value);
                  break;
                case "shield-text-dy":
                 // text_dy = Convert.ToSingle(kv.Value);
                  break;
              }
            }

            // TODO text_dx and text_dy
          }

          pp.Displacement = new PointF(dx, dy);

          result = pp;
          break;
        case "line":
          LinePlacement lp = new LinePlacement();

          if (lpi.Symbolizer == (int)SymbolizerType.Text)
          {
            lp.Spacing = 300;
            lp.UpsideDownFactorThreshold = 80;

            foreach (KeyValuePair<string, string> kv in lpi.Properties)
            {
              switch (kv.Key)
              {
                case "text-min-path-length":
                  lp.MinimumFeatureSize = Convert.ToSingle(kv.Value);
                  break;
                case "text-spacing":
                  lp.Spacing = Convert.ToSingle(kv.Value);
                  break;
                case "text-max-char-angle-delta":
                  lp.MaxAngleDelta = Convert.ToSingle(kv.Value);
                  break;
                case "text-label-position-tolerance":
                  lp.PositionTolerance = Convert.ToSingle(kv.Value);
                  break;
              }
            }
          }
          else if (lpi.Symbolizer == (int)SymbolizerType.Shield)
          {
            foreach (KeyValuePair<string, string> kv in lpi.Properties)
            {
              switch (kv.Key)
              {
                case "shield-spacing":
                  lp.Spacing = Convert.ToSingle(kv.Value);
                  break;
              }
            }
          }

          result = lp;
          break;
        case "line-point-pattern":
          LinePointPatternPlacement lppp = new LinePointPatternPlacement();
          lppp.Pattern = new float[] { 2, 100 };

          foreach (KeyValuePair<string, string> kv in lpi.Properties)
          {
            switch (kv.Key)
            {
              case "marker-spacing":
                lppp.Pattern[1] = Convert.ToSingle(kv.Value);
                break;
            }
          }

          result = lppp;
          break;
        case "vertex":
          throw new NotImplementedException();
        default:
          throw new Exception("Unknown placement type " + lpi.Placement);
      }

      return result;
    }

    private LabelAlignment[] GetDefaultLabelAlignment(bool aroundOnly)
    {
      if (aroundOnly)
        return new LabelAlignment[] { LabelAlignment.Around };
      else
        return new LabelAlignment[] { LabelAlignment.MiddleCenter, LabelAlignment.Around };
    }

    private void ApplySVGTransformation(Graphic graphic, string transformation)
    {
      if (string.IsNullOrEmpty(transformation) || graphic.GraphicSymbols.Count == 0)
        return;

      Match func = m_regexFunc.Match(transformation);
      string funcName = func.Groups["func"].Value;
      if (!string.IsNullOrEmpty(funcName))
      {
        var args = m_regexFuncParams.Matches(func.Groups[2].Value);

        switch (funcName.ToLower())
        {
          case "rotate":
            //	string rotate = args[0].Value;
            //ExternalGraphicSymbol ss;
            //MarkGraphicSymbol mg;
            break;
          case "scale":
            //	string scale = args[0].Value;
            break;
          case "translate":
            //	string translate = args[0].Value;
            break;
        }
      }
    }

    private void ApplyTextBlockFormat(TextLayout layout)
    {
      if (layout.Blocks.Count == 1)
      {
        TextLayoutBlock textBlock = layout.Blocks[0];
        if (!string.IsNullOrEmpty(textBlock.TextExpression))
        {
          string expr = textBlock.TextExpression;
          if (m_regexTextFormat.IsMatch(expr))
          {
            layout.Blocks.Clear();

            int p = 0;
            TextLayoutBlock tlb = null;

            foreach (Match match in m_regexTextFormat.Matches(expr))
            {
              if (p - match.Index > 0)
              {
                tlb = (TextLayoutBlock)textBlock.Clone();
                tlb.TextExpression = ReplaceQuotes(Regex.Replace(expr.Substring(p, p - match.Index), @"^([+ ])+|([+ ])+$", string.Empty, RegexOptions.Compiled));
                layout.Blocks.Add(tlb);
              }

              tlb = (TextLayoutBlock)textBlock.Clone();
              tlb.TextExpression = ReplaceQuotes(match.Groups["text"].Value);
              string formatProps = match.Groups["args"].Value;
              if (!string.IsNullOrEmpty(formatProps))
              {
                // We apply format properties
                MatchCollection matchArgs = Regex.Matches(formatProps, @"(?<args>[0-9a-zA-Z-]*=\s*('|"").*?('|""))", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
                for (int j = 0; j < matchArgs.Count; j++)
                {
                  Match matchKeyValue = Regex.Match(matchArgs[j].Value, @"((?<key>[0-9a-zA-Z-]*)=(?<value>\s*('|"").*?('|"")))", RegexOptions.Compiled);
                  string key = matchKeyValue.Groups["key"].Value;

                  if (string.IsNullOrEmpty(key))
                    continue;

                  string value = RemoveQuotes(matchKeyValue.Groups["value"].Value);

                  switch (key.ToLower())
                  {
                    case "fill":
                      tlb.TextFormat.TextStyle.Color = ColorUtility.FromHtml(value);
                      break;
                    case "size":
                      tlb.TextFormat.TextStyle.Font.Size = Convert.ToSingle(value);
                      break;
                    case "face-name":
                      SetFontName(tlb, value);
                      break;
                  }
                }
              }

              layout.Blocks.Add(tlb);

              p = match.Index + match.Length;
            }

            if (p < expr.Length)
            {
              tlb = (TextLayoutBlock)textBlock.Clone();
              tlb.TextExpression = ReplaceQuotes(Regex.Replace(expr.Substring(p, expr.Length - p), @"^([+ ])+|([+ ])+$", string.Empty, RegexOptions.Compiled));
              layout.Blocks.Add(tlb);
            }
          }
        }
      }
    }

    private void SetFontName(TextLayoutBlock textBlock, string value)
    {
      if (!string.IsNullOrEmpty(value))
      {
        string fontsetName = RemoveQuotes(value);
        if (this.FontSets != null)
        {
          FontSet fontSet = null;
          if (this.FontSets.TryGetValue(fontsetName, out fontSet))
          {
            textBlock.FontSetName = fontSet.Name;
            return;
          }
        }

        string[] fontParts = fontsetName.Split(new char[] { ' ' });

        string fontName = fontParts[0];
        FontStyles style = FontStyles.Normal;
        FontWeight weight = FontWeight.Normal;
        for (int i = 1; i < fontParts.Length; i++)
        {
          string part = fontParts[i];
          if (part.Equals("Oblique", StringComparison.OrdinalIgnoreCase))
            style = FontStyles.Oblique;
          else if (part.Equals("Italic", StringComparison.OrdinalIgnoreCase))
            style = FontStyles.Italic;
          else if (part.Equals("Bold", StringComparison.OrdinalIgnoreCase))
            weight = FontWeight.Bold;
          else
            fontName += " " + part;
        }

        textBlock.FontSetName = string.Empty;
        textBlock.TextFormat.TextStyle.Font.Name = fontName.Trim(new char[] { ' ' });
        textBlock.TextFormat.TextStyle.Font.Style = style;
        textBlock.TextFormat.TextStyle.Font.Weight = weight;
      }
    }

    private string GetTextTransform(string text, string transform)
    {
      if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(transform))
        return text;

      switch (transform)
      {
        case "none":
          return text;
        case "uppercase":
          return "(" + text + ").ToUpper()";
        case "lowercase":
          return "(" + text + ").ToLower()";
        case "capitalize":
          return "StringExtensions.Capitalize(" + text + ")";
        case "reverse":
          return "StringExtensions.Reverse(" + text + ")";
      }

      return text;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    public override string ToImageFilter(string filter)
    {
      // the full list of supported filters is available at
      //https://github.com/mapnik/mapnik/blob/8c2f15c94a6de1540bb50a33dc8f7f052a94c943/include/mapnik/image_filter_types.hpp
      int pos1 = filter.IndexOf('(');
      string filterName = filter.Substring(0, pos1);
      int pos2 = filter.LastIndexOf(')');
      string args = filter.Substring(pos1 + 1, pos2 - 1 - pos1);

      switch (filterName)
      {
        case "agg-stack-blur":
          return string.Format("StackBlur({0})", args);
        case "blur":
          return "StackBlur()";
        case "gaussian_blur":
          return "GaussianBlur()";
        case "invert":
          return "InvertColors()";
        case "emboss":
          return "Emboss()";
        case "sharpen":
          return "Sharpen()";
        case "edge_detect":
          return "EdgeDetect()";
        case "normalize":
          return "Normalize()";
        case "equalize":
          return "Equalize()";
        case "sepia":
          return "Sepia()";
        case "grayscale":
          return "Grayscale()";
        default:
          return "unknownfilter()";
      }
    }

    public override CartoDatasource ToDatasource(ParameterCollection parameters)
    {
      CartoDatasource ds = new CartoDatasource();

      if (parameters != null)
      {
        string type = parameters.GetValue("Type");
        string paramValue = null;

        switch (type.ToLower())
        {
          case "shape":
            if (parameters.TryGetValue("File", out paramValue))
            {
              if (paramValue.EndsWith(".shp"))
                ds.Add("type", type.ToLower());
            }
            else
              throw new Exception("'File' parameter is required.");

            ds.Add("file", paramValue);
            if (parameters.TryGetValue("FileBasedIndex", out paramValue))
              ds.Add("indexed", paramValue.ToLower());
            if (parameters.TryGetValue("Encoding", out paramValue))
              ds.Add("encoding", paramValue.ToLower());
            break;
          case "geojson":
            // ds.Add("type", type.ToLower());
            if (parameters.TryGetValue("File", out paramValue))
              ds.Add("file", paramValue);
            else
              throw new Exception("'File' parameter is required.");

            if (parameters.TryGetValue("FileBasedIndex", out paramValue))
              ds.Add("indexed", paramValue.ToLower());
            if (parameters.TryGetValue("Encoding", out paramValue) && !string.Equals(paramValue, "UTF-8", StringComparison.OrdinalIgnoreCase))
              ds.Add("encoding", paramValue.ToLower());
            break;
          case "ogr":
            ds.Add("type", type.ToLower());
            if (parameters.TryGetValue("File", out paramValue))
              ds.Add("file", paramValue);
            else
              throw new Exception("'File' parameter is required.");

            if (ds.TryGetValue("LayerName", out paramValue))
              ds.Add("layer", paramValue);
            break;
          case "postgis":
            ds.Add("type", type.ToLower());

            if (parameters.TryGetValue("Connection", out paramValue) && !string.IsNullOrEmpty(paramValue))
            {
              System.Data.Common.DbConnectionStringBuilder connParams = new System.Data.Common.DbConnectionStringBuilder();
              connParams.ConnectionString = paramValue;
              if (connParams.ContainsKey("Host") && !string.Equals(connParams["Host"], "localhost"))
                ds.Add("host", connParams["Host"]);
              if (connParams.ContainsKey("Port") && !string.Equals(connParams["Port"], "5432"))
                ds.Add("port", connParams["Port"]);
              if (connParams.ContainsKey("Database"))
                ds.Add("dbname", connParams["Database"]);

              if (connParams.ContainsKey("User ID") && !string.IsNullOrEmpty((string)connParams["User ID"]))
                ds.Add("user", connParams["User ID"]);

              if (connParams.ContainsKey("Password") && !string.IsNullOrEmpty((string)connParams["Password"]))
                ds.Add("password", connParams["Password"]);
            }

            if (parameters.TryGetValue("Extent", out paramValue))
              ds.Add("extent", paramValue);

            if (parameters.TryGetValue("Table_Origin", out paramValue))
              ds.Add("table", paramValue);
            if (parameters.TryGetValue("Query", out paramValue))
              ds.Add("table", paramValue);
            else if (parameters.TryGetValue("Table", out paramValue))
              ds.Add("table", paramValue);


            if (parameters.TryGetValue("GeometryField", out paramValue))
              ds.Add("geometry_field", paramValue);
            break;
          case "mssqlspatial":
            ds.Add("type", type.ToLower());

            if (parameters.TryGetValue("Connection", out paramValue))
              ds.Add("connection", paramValue);
            if (parameters.TryGetValue("GeometryField", out paramValue))
              ds.Add("geometry_field", paramValue);
            if (parameters.TryGetValue("Table", out paramValue))
              ds.Add("table", paramValue);
            if (parameters.TryGetValue("Query", out paramValue) && !string.IsNullOrEmpty(paramValue))
              ds.Add("query", paramValue);
            if (parameters.TryGetValue("SpatialIndex", out paramValue) && !string.IsNullOrEmpty(paramValue))
              ds.Add("spatial_index", paramValue);
            if (parameters.TryGetValue("Extent", out paramValue))
              ds.Add("extent", paramValue);
            break;
          case "spatialite":
            ds.Add("type", type.ToLower());

            if (parameters.TryGetValue("Connection", out paramValue))
              ds.Add("connection", paramValue);
            if (parameters.TryGetValue("GeometryField", out paramValue))
              ds.Add("geometry_field", paramValue);
            if (parameters.TryGetValue("Table", out paramValue))
              ds.Add("table", paramValue);
            if (parameters.TryGetValue("Query", out paramValue))
              ds.Add("query", paramValue);
            if (parameters.TryGetValue("Extent", out paramValue))
              ds.Add("extent", paramValue);
            break;
          case "dem":
            ds.Add("type", type.ToLower());

            if (parameters.TryGetValue("Path", out paramValue))
              ds.Add("path", paramValue);
            if (parameters.TryGetValue("DataSourceType", out paramValue))
              ds.Add("datasource_type", paramValue);
            if (parameters.TryGetValue("CacheSize", out paramValue))
              ds.Add("cache_size", paramValue);
            if (parameters.TryGetValue("FileExtension", out paramValue))
              ds.Add("file_extension", paramValue);
            if (parameters.TryGetValue("DataType", out paramValue))
              ds.Add("data_type", paramValue);
            if (parameters.TryGetValue("IsolineInterval", out paramValue))
              ds.Add("isoline_interval", paramValue);
            if (parameters.TryGetValue("MinElevation", out paramValue))
              ds.Add("min_elevation", paramValue);
            if (parameters.TryGetValue("MaxElevation", out paramValue))
              ds.Add("max_elevation", paramValue);
            if (parameters.TryGetValue("RestoreData", out paramValue))
              ds.Add("restore_data", paramValue);
            if (parameters.TryGetValue("ResampleAlgorithm", out paramValue))
              ds.Add("resampling_algorithm", paramValue);
            if (parameters.TryGetValue("AutoResolution", out paramValue))
              ds.Add("auto_resolution", paramValue);
            if (parameters.TryGetValue("DownsampleResolution", out paramValue))
              ds.Add("downsample_resolution", paramValue);
            if (parameters.TryGetValue("CacheID", out paramValue))
              ds.Add("cache_id", paramValue);
            break;
          case "osm":
            ds.Add("type", type.ToLower());
            if (parameters.TryGetValue("File", out paramValue))
              ds.Add("file", paramValue);
            else
              throw new Exception("'File' parameter is required.");

            if (parameters.TryGetValue("Query", out paramValue))
              ds.Add("query", paramValue);
            if (parameters.TryGetValue("TagsFilter", out paramValue))
              ds.Add("tags_filter", paramValue);
            if (parameters.TryGetValue("BuildSpatialIndex", out paramValue) && !string.IsNullOrEmpty(paramValue))
              ds.Add("spatial_index", paramValue.ToLower());
            if (parameters.TryGetValue("FileBasedIndex", out paramValue) && !string.IsNullOrEmpty(paramValue))
              ds.Add("spatial_index_file", paramValue.ToLower());
            break;
          case "esrifilegeodb":
            ds.Add("type", type.ToLower());

            if (parameters.TryGetValue("Path", out paramValue))
              ds.Add("path", paramValue);
            if (parameters.TryGetValue("Table", out paramValue))
              ds.Add("table", paramValue);
            if (parameters.TryGetValue("Query", out paramValue))
              ds.Add("query", paramValue);
            break;
        }
      }

      return ds;
    }

    public override ParameterCollection ToDatasourceParameters(CartoDatasource datasource)
    {
      ParameterCollection parameters = new ParameterCollection();

      if (datasource == null)
        return parameters;

      string dsType = datasource.Type.ToLower();
      string paramValue;
      string providerName = null;

      switch (dsType)
      {
        case "shape":
          providerName = "Shape";
          parameters.Add(new Parameter("File", datasource["file"]));

          if (datasource.TryGetValue("indexed", out paramValue))
            parameters.Add(new Parameter("FileBasedIndex", paramValue));
          else
            parameters.Add(new Parameter("FileBasedIndex", "True"));

          if (datasource.TryGetValue("encoding", out paramValue))
            parameters.Add(new Parameter("Encoding", paramValue));
          else
            parameters.Add(new Parameter("Encoding", "UTF-8"));

          if (datasource.TryGetValue("row_limit", out paramValue))
            parameters.Add(new Parameter("RowLimit", paramValue)); // Is not supported
          break;
        case "postgis":
          providerName = "PostGIS";

          string host = "localhost";
          if (datasource.TryGetValue("host", out paramValue))
            host = paramValue;

          string port = "5432";
          if (datasource.TryGetValue("port", out paramValue))
            port = paramValue;

          string dbname = string.Empty;
          if (datasource.TryGetValue("dbname", out paramValue))
            dbname = paramValue;

          string user = string.Empty;
          if (datasource.TryGetValue("user", out paramValue))
            user = paramValue;
          string password = string.Empty;
          if (datasource.TryGetValue("password", out paramValue))
            password = paramValue;

          string connection = string.Format("Host={0};Port={1};Database={2};User ID={3};Password={4};", host, port, dbname, user, password);
          parameters.Add(new Parameter("Connection", connection));

          if (datasource.TryGetValue("extent", out paramValue))
            parameters.Add(new Parameter("Extent", paramValue));

          if (datasource.TryGetValue("table", out paramValue))
          {
            parameters.Add(new Parameter("Table_Origin", paramValue));
            paramValue = PrepareSqlQuery(paramValue);
            parameters.Add(new Parameter("Query", paramValue));
            parameters.Add(new Parameter("Table", GetSqlTableName(paramValue)));
          }

          if (datasource.TryGetValue("geometry_field", out paramValue))
            parameters.Add(new Parameter("GeometryField", paramValue));
          break;
        case "mssqlspatial":
          providerName = "MsSQLSpatial";

          if (datasource.TryGetValue("connection", out paramValue))
            parameters.Add(new Parameter("Connection", paramValue));
          if (datasource.TryGetValue("geometry_field", out paramValue))
            parameters.Add(new Parameter("GeometryField", paramValue));
          if (datasource.TryGetValue("table", out paramValue))
            parameters.Add(new Parameter("Table", paramValue));
          if (datasource.TryGetValue("query", out paramValue))
            parameters.Add(new Parameter("Query", paramValue));
          if (datasource.TryGetValue("spatial_index", out paramValue))
            parameters.Add(new Parameter("SpatialIndex", paramValue));
          if (datasource.TryGetValue("extent", out paramValue))
            parameters.Add(new Parameter("Extent", paramValue));
          break;
        case "spatialite":
          providerName = "SpatiaLite";

          if (datasource.TryGetValue("connection", out paramValue))
            parameters.Add(new Parameter("Connection", paramValue));
          if (datasource.TryGetValue("geometry_field", out paramValue))
            parameters.Add(new Parameter("GeometryField", paramValue));
          if (datasource.TryGetValue("table", out paramValue))
            parameters.Add(new Parameter("Table", paramValue));
          if (datasource.TryGetValue("query", out paramValue))
            parameters.Add(new Parameter("Query", paramValue));
          if (datasource.TryGetValue("extent", out paramValue))
            parameters.Add(new Parameter("Extent", paramValue));
          break;
        case "geojson":
          providerName = "GeoJson";
          parameters.Add(new Parameter("File", datasource["file"]));

          if (datasource.TryGetValue("indexed", out paramValue))
            parameters.Add(new Parameter("FileBasedIndex", paramValue));
          else
            parameters.Add(new Parameter("FileBasedIndex", "False"));

          if (datasource.TryGetValue("encoding", out paramValue))
            parameters.Add(new Parameter("Encoding", paramValue));
          break;
        case "ogr":
          providerName = "OGR";
          parameters.Add(new Parameter("File", datasource["file"]));
          if (datasource.TryGetValue("layer", out paramValue))
            parameters.Add(new Parameter("LayerName", paramValue));
          else
            parameters.Add(new Parameter("LayerIndex", "0"));
          break;
        case "dem":
          providerName = "DEM";
          if (datasource.TryGetValue("path", out paramValue))
            parameters.Add(new Parameter("Path", paramValue));
          if (datasource.TryGetValue("datasource_type", out paramValue))
            parameters.Add(new Parameter("DataSourceType", paramValue));
          if (datasource.TryGetValue("cache_size", out paramValue))
            parameters.Add(new Parameter("CacheSize", paramValue));
          if (datasource.TryGetValue("file_extension", out paramValue))
            parameters.Add(new Parameter("FileExtension", paramValue));
          if (datasource.TryGetValue("data_type", out paramValue))
            parameters.Add(new Parameter("DataType", paramValue));
          if (datasource.TryGetValue("isoline_interval", out paramValue))
            parameters.Add(new Parameter("IsolineInterval", paramValue));
          if (datasource.TryGetValue("min_elevation", out paramValue))
            parameters.Add(new Parameter("MinElevation", paramValue));
          if (datasource.TryGetValue("max_elevation", out paramValue))
            parameters.Add(new Parameter("MaxElevation", paramValue));
          if (datasource.TryGetValue("restore_data", out paramValue))
            parameters.Add(new Parameter("RestoreData", paramValue));
          if (datasource.TryGetValue("resampling_algorithm", out paramValue))
            parameters.Add(new Parameter("ResampleAlgorithm", paramValue));
          if (datasource.TryGetValue("auto_resolution", out paramValue))
            parameters.Add(new Parameter("AutoResolution", paramValue));
          if (datasource.TryGetValue("downsample_resolution", out paramValue))
            parameters.Add(new Parameter("DownsampleResolution", paramValue));
          if (datasource.TryGetValue("cache_id", out paramValue))
            parameters.Add(new Parameter("CacheID", paramValue));
          break;
        case "gdal":
          providerName = "GDAL";
          parameters.Add(new Parameter("File", datasource["file"]));

          if (datasource.ContainsKey("DownsampleResolution"))
            parameters.Add(new Parameter("DownsampleResolution", datasource["DownsampleResolution"]));
          else
            parameters.Add(new Parameter("DownsampleResolution", "True"));

          if (datasource.ContainsKey("ResampleAlgorithm"))
            parameters.Add(new Parameter("ResampleAlgorithm", datasource["ResampleAlgorithm"]));
          else
            parameters.Add(new Parameter("ResampleAlgorithm", "Cubic"));
          break;
        case "osm":
          providerName = "OSM";
          parameters.Add(new Parameter("File", datasource["file"]));

          if (datasource.ContainsKey("query"))
            parameters.Add(new Parameter("Query", datasource["query"]));
          if (datasource.ContainsKey("tags_filter"))
            parameters.Add(new Parameter("TagsFilter", datasource["tags_filter"]));
          if (datasource.ContainsKey("spatial_index"))
            parameters.Add(new Parameter("BuildSpatialIndex", datasource["spatial_index"]));
          if (datasource.ContainsKey("spatial_index_file"))
            parameters.Add(new Parameter("FileBasedIndex", datasource["spatial_index_file"]));
          break;
        case "esrifilegeodb":
          providerName = "EsriFileGeoDB";
          parameters.Add(new Parameter("Path", datasource["path"]));
          if (datasource.TryGetValue("table", out paramValue))
            parameters.Add(new Parameter("Table", paramValue));
          if (datasource.TryGetValue("query", out paramValue))
            parameters.Add(new Parameter("Query", paramValue));
          break;
        case "vectortiles":
          providerName = "VectorTiles";
          if (datasource.TryGetValue("compression", out paramValue))
            parameters.Add(new Parameter("Compression", datasource["compression"]));
          if (datasource.TryGetValue("datasource", out paramValue))
            parameters.Add(new Parameter("Datasource", datasource["datasource"]));
          if (datasource.TryGetValue("extent", out paramValue))
            parameters.Add(new Parameter("Extent", datasource["extent"]));
          if (datasource.TryGetValue("format", out paramValue))
            parameters.Add(new Parameter("Format", datasource["format"]));
          if (datasource.TryGetValue("layers", out paramValue))
            parameters.Add(new Parameter("Layers", datasource["layers"]));
          if (datasource.TryGetValue("littleendian", out paramValue))
            parameters.Add(new Parameter("LittleEndian", datasource["littleendian"]));
          if (datasource.TryGetValue("minzoom", out paramValue))
            parameters.Add(new Parameter("MinZoom", datasource["minzoom"]));
          if (datasource.TryGetValue("maxzoom", out paramValue))
            parameters.Add(new Parameter("MaxZoom", datasource["maxzoom"]));
          if (datasource.TryGetValue("mode", out paramValue))
            parameters.Add(new Parameter("Mode", datasource["mode"]));
          if (datasource.TryGetValue("properties", out paramValue))
            parameters.Add(new Parameter("Properties", datasource["properties"]));
          if (datasource.TryGetValue("threads_number", out paramValue))
            parameters.Add(new Parameter("ThreadsNumber", datasource["threads_number"]));
          if (datasource.TryGetValue("tile_cache", out paramValue))
            parameters.Add(new Parameter("TileCache", datasource["tile_cache"]));
          break;
        default:

          break;
      }

      parameters.Add(new Parameter("Type", providerName));

      return parameters;
    }

    private string PrepareSqlQuery(string sql)
    {
      if (string.IsNullOrEmpty(sql))
        return sql;

      return sql.Replace("!bbox!", "T_BBOX");
    }

    private string GetSqlTableName(string sql)
    {
      string tableName = sql;
      int pos = sql.LastIndexOf(" FROM ", StringComparison.OrdinalIgnoreCase);

      if (pos > 0)
      {
        pos = FindFirstNotOf(tableName, " ", pos + 5);

        if (pos > 0)
        {
          int pos1 = tableName.IndexOf(" ", pos, StringComparison.OrdinalIgnoreCase);
          tableName = tableName.Substring(pos, pos1 - pos).Replace("\n", string.Empty);
        }

        pos = tableName.IndexOf(", )", StringComparison.OrdinalIgnoreCase);
        if (pos > 0)
        {
          tableName = tableName.Substring(0, pos).Replace("\n", string.Empty);
        }
      }

      return tableName;
    }

    public static int FindFirstNotOf(string source, string chars, int pos)
    {
      if (source == null)
        throw new ArgumentNullException("source");

      if (chars == null)
        throw new ArgumentNullException("chars");

      if (source.Length == 0) return -1;
      if (chars.Length == 0) return 0;

      for (int i = pos; i < source.Length; i++)
      {
        if (chars.IndexOf(source[i]) == -1)
          return i;
      }

      return -1;
    }

    private static bool HasProperty(NodePropertyValue[] properties, string prop)
    {
      for (int i = 0; i < properties.Length; i++)
      {
        NodePropertyValue pv = properties[i];
        if (pv.Name.Equals(prop, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
      }

      return false;
    }

    private static bool HasPropertyValue(NodePropertyValue[] properties, string prop, string value)
    {
      for (int i = 0; i < properties.Length; i++)
      {
        NodePropertyValue pv = properties[i];
        if (pv.Name.Equals(prop, StringComparison.OrdinalIgnoreCase))
        {
          if (pv.Value.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        }
      }

      return false;
    }

    public override bool IsFontSetProperty(string value)
    {
      return value == "text-face-name" || value == "shield-face-name";
    }

    private LineJoin ToLineJoin(string value)
    {
      switch (value.ToLower())
      {
        case "miter":
          return LineJoin.Miter;
        case "round":
          return LineJoin.Round;
        case "bevel":
          return LineJoin.Bevel;
        default:
          throw new Exception("Unknown line-join value.");
      }
    }

    private LineCap ToLineCap(string value)
    {
      switch (value.ToLower())
      {
        case "butt":
          return LineCap.Flat;
        case "round":
          return LineCap.Round;
        case "square":
          return LineCap.Square;
        default:
          throw new Exception("Unknown line-cap value.");
      }
    }

    private TextAlignment ToTextAlignment(string value)
    {
      switch (value.ToLower())
      {
        case "left":
          return TextAlignment.Left;
        case "right":
          return TextAlignment.Right;
        case "center":
          return TextAlignment.Center;
        case "auto":
          // TODO
          break;
      }

      return TextAlignment.Center;
    }

    private LabelAlignment ToLabelAlignment(string value)
    {
      //"N,S,E,W,NE,SE,NW,SW,16,14,12";
      switch (value.ToUpper())
      {
        case "N":
          return LabelAlignment.TopCenter;
        case "S":
          return LabelAlignment.BottomCenter;
        case "E":
          return LabelAlignment.MiddleRight;
        case "W":
          return LabelAlignment.MiddleLeft;
        case "NE":
          return LabelAlignment.TopRight;
        case "SE":
          return LabelAlignment.BottomRight;
        case "NW":
          return LabelAlignment.TopLeft;
        case "SW":
          return LabelAlignment.BottomLeft;
      }

      return LabelAlignment.MiddleCenter;
    }

    private LabelAlignment[] ToLabelAlignments(string value)
    {
      //"N,S,E,W,NE,SE,NW,SW,16,14,12";
      string[] pv = value.Trim(new char[] { '"', '\'' }).Split(',');
      List<LabelAlignment> alignments = new List<LabelAlignment>(pv.Length);
      float size;

      for (int i = 0; i < pv.Length; i++)
      {
        if (float.TryParse(pv[i], out size))
          break;

        alignments.Add(ToLabelAlignment(pv[i]));
      }

      return alignments.ToArray();
    }

    private string ToGeometryExpression(GeometryTransformInfo gt)
    {
      if (!gt.IsEmpty)
      {
        string expr = "GeometryTransformations.ViewTransformation([_geom_], [_ViewTransformation_])";

        if (!string.IsNullOrEmpty(gt.DisplacementX) || !string.IsNullOrEmpty(gt.DisplacementY))
        {
          string dx = string.IsNullOrEmpty(gt.DisplacementX) ? "0" : gt.DisplacementX;
          string dy = string.IsNullOrEmpty(gt.DisplacementY) ? "0" : gt.DisplacementY;

          if (gt.OffsetCurve)
            expr = string.Format("GeometryTransformations.OffsetCurve({0}, {1}, 0)", expr, dy);
          else
            expr = string.Format("GeometryTransformations.Offset({0}, {1}, {2})", expr, dx, dy);
        }

        if (!string.IsNullOrEmpty(gt.Simplify))
          expr = string.Format("GeometryTransformations.Simplify({0}, {1})", expr, gt.Simplify);
        if (!string.IsNullOrEmpty(gt.Offset))
          expr = string.Format("GeometryTransformations.OffsetCurve({0}, {1}, 0)", expr, gt.Offset);
        if (!string.IsNullOrEmpty(gt.Smooth))
          expr = string.Format("GeometryTransformations.Smooth({0}, {1})", expr, gt.Smooth);

        return expr;
      }
       
      return null;
    }

    private string TrimIf(string value, char ch)
    {
      if (string.IsNullOrEmpty(value))
        return value;
      if (value[0] == ch && value[value.Length - 1] == ch)
        return value.Trim(new char[] { ch });

      return value;
    }

    private string ToExpression(string value)
    {
      if (string.IsNullOrEmpty(value))
        return value;

      value = TrimIf(value , '\"');

      if (value.Contains(".replace("))
      {
        Match funcMatch = m_regexFunc.Match(value);

        if (funcMatch.Success)
        {
          MatchCollection argsMatches = m_regexFuncParams.Matches(funcMatch.Groups[2].Value);

          string funcName = funcMatch.Groups["func"].Value;
          string varName = funcName.Substring(0, funcName.IndexOf('.'));

          value = "Regex.Replace(" + varName + ", " + ReplaceQuotes(argsMatches[0].Value) + ", " + ReplaceQuotes(argsMatches[1].Value) + ")";
        }
      }

      if (value == "''")
        return "";
      else
      {
        value = TrimIf(value, '\'');

        if (value.Contains("["))
          return value;
        else if (value.Length != 0)
        {
          if (value[0] == '"' && value[value.Length - 1] == '"')
            return value;
          else
            return "\"" + value + "\"";
        }
      }

      return value;
    }

    public override string ToPath(string url)
    {
      url = url.TrimStart("url(".ToCharArray()).TrimEnd(')');
      Uri uri = null;
      if (Uri.TryCreate(url, UriKind.Relative, out uri))
      {
        return url;
      }
      else
      {
        if (Uri.TryCreate(url, UriKind.Absolute, out uri))
        {
          if (uri.IsFile)
            return uri.LocalPath;
          else
            return uri.AbsolutePath;
        }
        else
        {
          return url;
        }
      }
    }

    public override string ToFilter(string key, string op, string value)
    {
      bool bAddSquareBrackets = true;
      if (key != null)
      {
        float fValue;
        if (float.TryParse(key, out fValue))
        {
          bAddSquareBrackets = false;
        }
        else if (key.Contains("mapnik::geometry_type"))
        {
          // Here, we convert expressions such as ['mapnik::geometry_type'=2]  to
          // [_geom_].OgcGeometryType = OgcGeometryType.LineString

          key = "[_geom_].OgcGeometryType";

          int intValue = -1;
          if (int.TryParse(value, out intValue))
            value = NumberToGeometryType(intValue);
          else
            value = "OgcGeometryType." + value;

          bAddSquareBrackets = false;
        }
        else if (m_regexFunc.IsMatch(key))
        {
          bAddSquareBrackets = false;
        }
      }

      if (op == "%")
      {
        key = "(" + (bAddSquareBrackets ? ("[" + key + "]") : key) + " % " + value + ")";
        value = "0";
        op = "=";
        return key + " " + op + " " + value;
      }
      else
        return (bAddSquareBrackets ? ("[" + key + "]") : key) + " " + op + " " + value;
    }

    public ColorMapMode ToColorMapMode(string value)
    {
      if (!string.IsNullOrEmpty(value))
      {
        switch (value.Trim().ToLower())
        {
          case "linear":
            return ColorMapMode.Linear;
          case "discrete":
            return ColorMapMode.Discrete;
          case "exact":
            return ColorMapMode.Exact;
        }
      }

      return ColorMapMode.Inherit;
    }

    /// <summary>
    /// Converts geometry type constants to their string representation.
    /// see https://github.com/mapnik/mapnik/blob/master/include/mapnik/geometry_types.hpp
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    private string NumberToGeometryType(int value)
    {
      switch (value)
      {
        case 0:
          return "Unknown";
        case 1:
          return "OgcGeometryType.Point";
        case 2:
          return "OgcGeometryType.LineString";
        case 3:
          return "OgcGeometryType.Polygon";
        case 4:
          return "OgcGeometryType.MultiPoint";
        case 5:
          return "OgcGeometryType.MultiLineString";
        case 6:
          return "OgcGeometryType.MultiPolygon";
        case 7:
          return "OgcGeometryType.GeometryCollection";
      }

      return value.ToString();
    }

    private string ReplaceQuotes(string value)
    {
      if (string.IsNullOrEmpty(value))
        return value;

      return value.Replace("'", "\"");
    }

    private string RemoveQuotes(string value)
    {
      if (string.IsNullOrEmpty(value))
        return value;

      return value.Replace("\"", string.Empty).Replace("'", string.Empty);
    }

    private FeatureTypeStyle CopyStyle(FeatureTypeStyle style, int ruleIndex, int length, int lastRuleSymIndex)
    {
      FeatureTypeStyle newStyle = new FeatureTypeStyle();
      newStyle.Name = style.Name;
      newStyle.BlendingOptions = style.BlendingOptions;
      newStyle.Enabled = style.Enabled;

      for (int ri = ruleIndex; ri < ruleIndex + length; ri++)
      {
        Rule rule = style.Rules[ri];

        if (lastRuleSymIndex < 0)
          newStyle.Rules.Add(rule);
        else
        {
          if (ri + 1 == ruleIndex + length)
          {
            Rule newRule = CopyRule(rule, 0, lastRuleSymIndex);
            newStyle.Rules.Add(newRule);
          }
          else
            newStyle.Rules.Add(rule);
        }
      }

      return newStyle;
    }

    private Rule CopyRule(Rule rule, int symIndex = 0, int length = -1)
    {
      Rule newRule = new Rule();
      newRule.Filter = rule.Filter;
      newRule.Name = rule.Name;
      newRule.MaxScale = rule.MaxScale;
      newRule.MinScale = rule.MinScale;

      if (length > 0)
      {
        for (int i = symIndex; i < symIndex + length; i++)
        {
          newRule.Symbolizers.Add(rule.Symbolizers[i]);
        }
      }

      return newRule;
    }

    public override void ProcessStyles(FeatureTypeStyleCollection styles)
    {
      if (styles.Count > 0)
      {
        FeatureTypeStyleCollection newStyles = new FeatureTypeStyleCollection();

        foreach (FeatureTypeStyle style in styles)
        {
          for (int ri = 0; ri < style.Rules.Count; ri++)
          {
            Rule rule = style.Rules[ri];

            for (int si = 0; si < rule.Symbolizers.Count; si++)
            {
              Symbolizer sym = rule.Symbolizers[si];
              Dictionary<string, string> props = sym.Tag as Dictionary<string, string>;

              if (props != null && props.ContainsKey("comp-op"))
              {
                // Create new style and copy all previous rules.
                FeatureTypeStyle newStyle = CopyStyle(style, 0, ri + 1, si);
                newStyles.Add(newStyle);

                FeatureTypeStyle newStyle2 = new FeatureTypeStyle(style.Name + "_comp-op_" + props["comp-op"]);
                newStyle2.BlendingOptions.CompositingMode = ToCompositingMode(props["comp-op"]);
                Rule newRule = CopyRule(rule);
                newRule.Symbolizers.Add(sym);
                newStyle2.Rules.Add(newRule);

                newStyles.Add(newStyle2);

                rule.Symbolizers.RemoveRange(0, si + 1);

                style.Rules.RemoveRange(0, ri + 1);
                ri = -1;

                break;
              }
            }
          }

          if (style.Rules.Count > 0)
            newStyles.Add(style);
        }

        if (newStyles.Count > styles.Count)
        {
          styles.Clear();
          styles.AddRange(newStyles);
        }
      }
    }

    private void UnsupportedProperty(NodePropertyValue prop)
    {
      if (m_logger != null)
      {
        NotSupportedPropertyException npe = new NotSupportedPropertyException(string.Format("Property '{0}' is not supported.", prop.Name), prop.Location.FileName, Zone.GetLineNumber(prop.Location));
        LogFactory.WriteLogEntry(m_logger, npe, LogEntryType.Information);
      }
    }
  }
}
