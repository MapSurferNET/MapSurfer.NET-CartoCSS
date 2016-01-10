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
using System.Linq;

using MapSurfer.Configuration;
using MapSurfer.Drawing;
using MapSurfer.Drawing.Text;
using MapSurfer.Labeling;

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

    public MapnikTranslator()
    {
      m_referencer = new MapnikPropertyReferencer();
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

    private ExtrudedPolygonSymbolizer CreateExtrudedPolygonSymbolizer(string[] properties, string[] values)
    {
      ExtrudedPolygonSymbolizer symExtPoly = new ExtrudedPolygonSymbolizer();
      symExtPoly.Clip = false;
      int nProps = properties.Length;

      for (int i = 0; i < nProps; i++)
      {
        string prop = properties[i];
        switch (prop)
        {
          case "building-fill":
            Color clr = ColorTranslator.FromHtml(values[i]);
            symExtPoly.FacesColor = clr;
            symExtPoly.TopColor = clr;
            break;
          case "building-fill-opacity":
            symExtPoly.FillOpacity = Convert.ToSingle(values[i]);
            break;
          case "building-height":
            symExtPoly.HeightExpression = ToExpression(values[i]);
            break;
        }
      }

      return symExtPoly;
    }

    private RasterSymbolizer CreateRasterSymbolizer(string[] properties, string[] values)
    {
      throw new NotImplementedException();
    }

    private GraphicTextSymbolizer CreateGraphicTextSymbolizer(string[] properties, string[] values)
    {
      GraphicTextSymbolizer symText = new GraphicTextSymbolizer();
      symText.Clip = true;
      ExternalGraphicSymbol gsImage = new ExternalGraphicSymbol();
      symText.Graphic.GraphicSymbols.Add(gsImage);

      GeometryTransformInfo geomTrans = new GeometryTransformInfo();

      LabelPlacementInfo lpi = new LabelPlacementInfo();
      lpi.Placement = "point";
      lpi.Symbolizer = (int)SymbolizerType.Shield;

      int blockIndex = -1;
      TextLayoutBlock textBlock = new TextLayoutBlock();
      int nProps = properties.Length;

      for (int i = 0; i < nProps; i++)
      {
        string prop = properties[i];
        switch (prop)
        {
          case "shield-name":
            if (values[i] != null)
            {
              textBlock.TextExpression = ToExpression(values[i]);

              blockIndex++;
              if (blockIndex > 0)
                textBlock = new TextLayoutBlock();

              symText.TextLayout.Blocks.Add(textBlock);
            }
            break;
          case "shield-face-name":
            if (values[i] != null)
            {
              string strFace = RemoveQuotes(values[i]);
              if (this.FontSets != null)
              {
                FontSet fontSet = null;
                if (this.FontSets.TryGetValue(strFace, out fontSet))
                  strFace = fontSet.Name;
              }

              textBlock.FontSetName = strFace;
            }
            break;
          case "shield-file":
            gsImage.Path = ToPath(values[i]);
            break;
          case "shield-text-transform":
            textBlock.TextExpression = ApplyTextTransform(textBlock.TextExpression, values[i]);
            break;
          case "shield-fill":
            textBlock.TextFormat.TextStyle.Color = ColorTranslator.FromHtml(values[i]);
            break;
          case "shield-text-opacity":
            textBlock.TextFormat.TextStyle.Opacity = Convert.ToSingle(values[i]);
            break;
          case "shield-opacity":
            gsImage.Opacity = Convert.ToSingle(values[i]);
            break;
          case "shield-size":
            //textBlock.TextFormat.TextStyle.Font.Size = Convert.ToSingle(values[i]);
            float sz = Convert.ToSingle(values[i]);
            symText.Graphic.Size = new SizeF(sz, sz);
            break;
          case "shield-halo-radius":
            textBlock.TextFormat.TextStyle.Halo.Radius = Convert.ToSingle(values[i]);
            break;
          case "shield-halo-fill":
            textBlock.TextFormat.TextStyle.Halo.Color = ColorTranslator.FromHtml(values[i]);
            break;
          case "shield-halo-opacity":
            textBlock.TextFormat.TextStyle.Halo.Opacity = Convert.ToSingle(values[i]);
            break;
          case "shield-clip":
            symText.Clip = Convert.ToBoolean(values[i]);
            break;
          case "shield-horizontal-alignment":
            // TODO
            break;
          case "shield-vertical-alignment":
            // TODO
            break;
          case "shield-justify-alignment":
            // TODO
            break;
          case "shield-transform":
            //TODO
            break;
          case "shield-wrap-width":
            textBlock.TextFormat.TextWrapping.MaxWidth = Convert.ToUInt32(values[i]);
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

            tw2.Characters[0].Character = values[i];
            break;
          case "shield-character-spacing":
            textBlock.TextFormat.TextSpacing.CharacterSpacing = Convert.ToSingle(values[i]);
            break;
          case "shield-line-spacing":
            textBlock.TextFormat.TextSpacing.Leading = Convert.ToSingle(values[i]);
            break;
          case "shield-allow-overlap":
            symText.LabelBehaviour.AllowOverlap = Convert.ToBoolean(values[i]);
            break;
          case "shield-min-distance":
            symText.LabelBehaviour.CollisionMeasures.Add(string.Format("MinimumDistance({0})", values[i]));
            break;
          case "shield-avoid-edges":
            symText.LabelBehaviour.AvoidEdges = Convert.ToBoolean(values[i]);
            break;
          case "shield-spacing":
            lpi.Properties.Add(new KeyValuePair<string, string>(prop, values[i]));
            break;
          case "shield-min-padding":
            // not supported
            break;
          case "shield-placement-type":
            lpi.Properties.Add(new KeyValuePair<string, string>(prop, values[i]));
            break;
          case "shield-placements":
            lpi.Properties.Add(new KeyValuePair<string, string>(prop, values[i]));
            break;
          case "shield-text-dx":
            lpi.Properties.Add(new KeyValuePair<string, string>(prop, values[i]));
            break;
          case "shield-text-dy":
            lpi.Properties.Add(new KeyValuePair<string, string>(prop, values[i]));
            break;
          case "shield-dx":
            lpi.Properties.Add(new KeyValuePair<string, string>(prop, values[i]));
            break;
          case "shield-dy":
            lpi.Properties.Add(new KeyValuePair<string, string>(prop, values[i]));
            break;
          case "shield-comp-op":
            // not supported property
            break;
          default:
            break;
        }
      }

      symText.LabelPlacement = CreateLabelPlacement(lpi);

      symText.GeometryExpression = ToGeometryExpression(geomTrans);

      return symText;
    }

    private LinePatternSymbolizer CreateLinePatternSymbolizer(string[] properties, string[] values)
    {
      LinePatternSymbolizer symLinePattern = new LinePatternSymbolizer();
      symLinePattern.Clip = true;
      symLinePattern.LabelBehaviour.AllowOverlap = true;
      symLinePattern.LabelBehaviour.CollisionDetectable = false;

      GeometryTransformInfo geomTrans = new GeometryTransformInfo();
      int nProps = properties.Length;

      for (int i = 0; i < nProps; i++)
      {
        switch (properties[i])
        {
          case "line-pattern-file":
            symLinePattern.FileName = ToPath(values[i]);
            break;
          case "line-pattern-clip":
            symLinePattern.Clip = Convert.ToBoolean(values[i]);
            break;
          case "line-pattern-simplify":
            geomTrans.Simplify = values[i];
            break;
          case "line-pattern-simplify-algorithm":
            geomTrans.SimplifyAlgorithm = values[i];
            break;
          case "line-pattern-smooth":
            geomTrans.Smooth = values[i];
            break;
          case "line-pattern-offset":
            geomTrans.Offset = values[i];
            break;
          case "line-pattern-geometry-transform":
            geomTrans.GeometryTransform = values[i];
            break;
          case "line-pattern-comp-op":
            // not supported
            AddProperty(symLinePattern, "comp-op", values[i]);
            break;
        }
      }

      symLinePattern.GeometryExpression = ToGeometryExpression(geomTrans);

      return symLinePattern;
    }

    private TextSymbolizer CreateTextSymbolizer(string[] properties, string[] values)
    {
      TextSymbolizer symText = new TextSymbolizer();
      symText.Clip = true;

      GeometryTransformInfo geomTrans = new GeometryTransformInfo();
      LabelPlacementInfo lpi = new LabelPlacementInfo();
      lpi.Symbolizer = (int)SymbolizerType.Text;
      TextLayoutBlock textBlock = new TextLayoutBlock();
      int blockIndex = -1;
      int nProps = properties.Length;

      for (int i = 0; i < nProps; i++)
      {
        string prop = properties[i];
        switch (prop)
        {
          case "text-name":
            if (values[i] != null)
            {
              textBlock.TextExpression = ToExpression(values[i]);

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
            if (values[i] != null)
            {
              string strFace = RemoveQuotes(values[i]);
              if (this.FontSets != null)
              {
                FontSet fontSet = null;
                if (this.FontSets.TryGetValue(strFace, out fontSet))
                  strFace = fontSet.Name;
              }

              textBlock.FontSetName = strFace;
            }
            break;
          case "text-transform":
            textBlock.TextExpression = ApplyTextTransform(textBlock.TextExpression, values[i]);
            break;
          case "text-fill":
            textBlock.TextFormat.TextStyle.Color = ColorTranslator.FromHtml(values[i]);
            break;
          case "text-opacity":
            textBlock.TextFormat.TextStyle.Opacity = Convert.ToSingle(values[i]);
            break;
          case "text-size":
            textBlock.TextFormat.TextStyle.Font.Size = Convert.ToSingle(values[i]);
            break;
          case "text-halo-radius":
            textBlock.TextFormat.TextStyle.Halo.Radius = Convert.ToSingle(values[i]);
            break;
          case "text-halo-fill":
            Color clr = ColorTranslator.FromHtml(values[i]);
            textBlock.TextFormat.TextStyle.Halo.Color = clr;
            textBlock.TextFormat.TextStyle.Halo.Opacity = clr.A / 255F;
            break;
          case "text-halo-opacity": // This property does not exist in the specification.
            textBlock.TextFormat.TextStyle.Halo.Opacity = Convert.ToSingle(values[i]);
            break;
          case "text-halo-rasterizer":
            // TODO
            break;
          case "text-ratio":
            //TODO
            break;
          case "text-clip":
            symText.Clip = Convert.ToBoolean(values[i]);
            break;
          case "text-align":
            textBlock.TextFormat.TextAlignment = ToTextAlignment(values[i]);
            break;
          case "text-horizontal-alignment":
            // TODO
            break;
          case "text-vertical-alignment":
            // TODO
            break;
          case "text-wrap-width":
            textBlock.TextFormat.TextWrapping.MaxWidth = Convert.ToUInt32(values[i]);
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
            if (tw2.Characters == null)
              tw2.Characters = new WrapCharacter[] { new WrapCharacter() };
            tw2.Characters[0].Character = values[i];
            break;
          case "text-character-spacing":
            textBlock.TextFormat.TextSpacing.CharacterSpacing = Convert.ToSingle(values[i]);
            break;
          case "text-line-spacing":
            textBlock.TextFormat.TextSpacing.Leading = Convert.ToSingle(values[i]);
            break;
          case "text-allow-overlap":
            symText.LabelBehaviour.AllowOverlap = Convert.ToBoolean(values[i]);
            break;
          case "text-min-distance":
            symText.LabelBehaviour.CollisionMeasures.Add(string.Format("MinimumDistance({0})", values[i]));
            break;
          case "text-avoid-edges":
            symText.LabelBehaviour.AvoidEdges = Convert.ToBoolean(values[i]);
            break;
          case "text-spacing":
            lpi.Properties.Add(new KeyValuePair<string, string>(prop, values[i]));
            break;
          case "text-max-char-angle-delta":
            lpi.Properties.Add(new KeyValuePair<string, string>(prop, values[i]));
            break;
          case "text-label-position-tolerance":
            lpi.Properties.Add(new KeyValuePair<string, string>(prop, values[i]));
            break;
          case "text-min-padding":
            // not supported
            break;
          case "text-min-path-length":
            lpi.Properties.Add(new KeyValuePair<string, string>(prop, values[i]));
            break;
          case "text-orientation":
            lpi.Properties.Add(new KeyValuePair<string, string>(prop, values[i]));
            break;
          case "text-placement":
            lpi.Placement = values[i];
            break;
          case "text-placement-type":
            lpi.Properties.Add(new KeyValuePair<string, string>(prop, values[i]));
            break;
          case "text-placements":
            lpi.Properties.Add(new KeyValuePair<string, string>(prop, values[i]));
            break;
          case "text-dx":
            geomTrans.DisplacementX = values[i];
            lpi.Properties.Add(new KeyValuePair<string, string>(prop, values[i]));
            break;
          case "text-dy":
            geomTrans.DisplacementY = values[i];
            lpi.Properties.Add(new KeyValuePair<string, string>(prop, values[i]));
            break;
          case "text-comp-op":
            // not supported
            break;
          default:
            break;
        }
      }

      symText.LabelPlacement = CreateLabelPlacement(lpi);
      symText.GeometryExpression = ToGeometryExpression(geomTrans);

      return symText;
    }

    private PointSymbolizer CreatePointSymbolizer(string[] properties, string[] values)
    {
      PointSymbolizer symPoint = new PointSymbolizer();
      
      bool isPoint = properties[0].StartsWith("point-");

      ExternalGraphicSymbol pointSymbol = null;
      MarkGraphicSymbol markSymbol = null;

      LabelPlacementInfo lpi = new LabelPlacementInfo();

      if (isPoint || properties.Contains("marker-file"))
      {
        pointSymbol = new ExternalGraphicSymbol();
        symPoint.Graphic.GraphicSymbols.Add(pointSymbol);
        lpi.Placement = "centroid";
        lpi.Symbolizer = (int)SymbolizerType.Point;
      }
      else
      {
        markSymbol = new MarkGraphicSymbol();
        symPoint.Graphic.GraphicSymbols.Add(markSymbol);
        lpi.Symbolizer = (int)SymbolizerType.Marker;
      }

      int nProps = properties.Length;
      float width = 10F, height = 0F;

      for (int i = 0; i < nProps; i++)
      {
        string prop = properties[i];

        if (isPoint)
        {
          switch (prop)
          {
            case "point-file":
              pointSymbol.Path = ToPath(values[i]);
              break;
            case "point-allow-overlap":
              symPoint.LabelBehaviour.AllowOverlap = Convert.ToBoolean(values[i]);
              break;
            case "point-ignore-placement":
              symPoint.LabelBehaviour.CollisionDetectable = Convert.ToBoolean(values[i]);
              break;
            case "point-opacity":
              pointSymbol.Opacity = Convert.ToSingle(values[i]);
              break;
            case "point-placement":
              lpi.Placement = values[i];
              break;
            case "point-transform":
              // not supported
              break;
            case "point-comp-op":
              // not supported
              AddProperty(symPoint, "comp-op", values[i]);
              break;
          }
        }
        else
        {
          switch (prop)
          {
            case "marker-file":
              if (pointSymbol != null)
                pointSymbol.Path = ToPath(values[i]);
              break;
            case "marker-opacity":
              float value = Convert.ToSingle(values[i]);
              if (pointSymbol != null)
                pointSymbol.Opacity = value;
              else if (markSymbol != null)
                symPoint.Graphic.Opacity /*markSymbol.Opacity*/ = value;

              if (value == 0.0F)
                symPoint.Enabled = false;
              break;
            case "marker-line-color":
              if (markSymbol != null)
                markSymbol.Stroke.Color = ColorTranslator.FromHtml(values[i]);
              break;
            case "marker-line-width":
              if (markSymbol != null)
                markSymbol.Stroke.Width = Convert.ToSingle(values[i]);
              break;
            case "marker-line-opacity":
              if (markSymbol != null)
                markSymbol.Stroke.Opacity = Convert.ToSingle(values[i]);
              break;
            case "marker-placement":
              lpi.Placement = values[i];
              break;
            case "marker-multi-policy":
              // TODO
              break;
            case "marker-type":
              markSymbol.WellKnownName = values[i];
              break;
            case "marker-width":
              width = Convert.ToSingle(values[i]);
              break;
            case "marker-height":
              height = Convert.ToSingle(values[i]);
              break;
            case "marker-fill-opacity":
              if (markSymbol != null)
                markSymbol.Fill.Opacity = Convert.ToSingle(values[i]);
              break;
            case "marker-fill":
              if (markSymbol != null)
                markSymbol.Fill.Color = ColorTranslator.FromHtml(values[i]);
              break;
            case "marker-allow-overlap":
              symPoint.LabelBehaviour.AllowOverlap = Convert.ToBoolean(values[i]);
              break;
            case "marker-ignore-placement":
              symPoint.LabelBehaviour.CollisionDetectable = Convert.ToBoolean(values[i]);
              break;
            case "marker-spacing":
              // default value 100
              // TODO
              break;
            case "marker-max-error":
              // TODO
              break;
            case "marker-transform":
              // TODO
              break;
            case "marker-clip":
              symPoint.Clip = Convert.ToBoolean(values[i]);
              break;
            case "marker-smooth":
              symPoint.Clip = Convert.ToBoolean(values[i]);
              break;
            case "marker-geometry-transform":
              // TODO
              break;
            case "marker-comp-op":
              // not supported
              AddProperty(symPoint, "comp-op", values[i]);
              break;
          }
        }
      }

      if (markSymbol != null)
      {
        if (height == 0F)
          height = width;

        markSymbol.Size = new SizeF(width, height);
      }

      symPoint.LabelPlacement = CreateLabelPlacement(lpi);

      return symPoint;
    }

    private PolygonSymbolizer CreatePolygonSymbolizer(string[] properties, string[] values)
    {
      PolygonSymbolizer symPolygon = new PolygonSymbolizer();
      symPolygon.Fill.Color = Color.Gray;
      symPolygon.Fill.Outlined = false;
      symPolygon.Clip = true;

      GraphicFill gFill = properties[0].StartsWith("polygon-pattern-") ? new GraphicFill() : null;
      if (gFill != null)
      {
        symPolygon.Fill.GraphicFill = gFill;
        symPolygon.Fill.Opacity = 0.0F;
      }

      GeometryTransformInfo geomTrans = new GeometryTransformInfo();
      int nProps = properties.Length;

      for (int i = 0; i < nProps; i++)
      {
        if (gFill != null)
        {
          switch (properties[i])
          {
            case "polygon-pattern-file":
              ExternalGraphicSymbol egs = new ExternalGraphicSymbol();
              egs.Path = ToPath(values[i]);
              gFill.GraphicSymbols.Add(egs);
              break;
            case "polygon-pattern-opacity":
              gFill.Opacity = Convert.ToSingle(values[i]);
              break;
            case "polygon-pattern-comp-op":
              AddProperty(symPolygon, "comp-op", values[i]);
              break;
          }
        }
        else
        {
          switch (properties[i])
          {
            case "polygon-fill":
              symPolygon.Fill.Color = ColorTranslator.FromHtml(values[i]);
              break;
            case "polygon-opacity":
              symPolygon.Fill.Opacity = Convert.ToSingle(values[i]);
              break;
            case "polygon-gamma":
              // not supported property
              break;
            case "polygon-gamma-method":
              // not supported property
              break;
            case "polygon-clip":
              symPolygon.Clip = Convert.ToBoolean(values[i]);
              break;
            case "polygon-comp-op":
              // not supported property
              AddProperty(symPolygon, "comp-op", values[i]);
              break;
            case "polygon-simplify":
              geomTrans.Simplify = values[i];
              break;
            case "polygon-simplify-algorithm":
              geomTrans.SimplifyAlgorithm = values[i];
              break;
            case "polygon-smooth":
              geomTrans.Smooth = values[i];
              break;
            case "polygon-offset":
              geomTrans.Offset = values[i];
              break;
            case "polygon-geometry-transform":
              geomTrans.GeometryTransform = values[i];
              break;
          }
        }
      }

      symPolygon.GeometryExpression = ToGeometryExpression(geomTrans);

      return symPolygon;
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

    private LineSymbolizer CreateLineSymbolizer(string[] properties, string[] values)
    {
      LineSymbolizer symLine = new LineSymbolizer();
      symLine.Stroke.LineCap = System.Drawing.Drawing2D.LineCap.Flat;
      symLine.Stroke.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
      symLine.Clip = true;

      GeometryTransformInfo geomTrans = new GeometryTransformInfo();
      int nProps = properties.Length;

      for (int i = 0; i < nProps; i++)
      {
        switch (properties[i])
        {
          case "line-width":
            symLine.Stroke.Width = Convert.ToSingle(values[i]);
            break;
          case "line-color":
            symLine.Stroke.Color = ColorTranslator.FromHtml(values[i]);
            break;
          case "line-opacity":
            symLine.Stroke.Opacity = Convert.ToSingle(values[i]);
            break;
          case "line-join":
            symLine.Stroke.LineJoin = ToLineJoin(values[i]);
            break;
          case "line-cap":
            symLine.Stroke.LineCap = ToLineCap(values[i]);
            break;
          case "line-dasharray":
            symLine.Stroke.DashArray = ConvertUtility.ToFloatArray(values[i]);
            break;
          case "line-miterlimit":
            symLine.Stroke.MiterLimit = Convert.ToSingle(values[i]);
            break;
          case "line-dash-offset":
            symLine.Stroke.DashOffset = Convert.ToSingle(values[i]);
            break;
          case "line-comp-op":
            // not supported property
            AddProperty(symLine, "comp-op", values[i]);
            break;
          case "line-rasterizer":
            // not supported property
            break;
          case "line-simplify":
            geomTrans.Simplify = values[i];
            break;
          case "line-simplify-algorithm":
            geomTrans.SimplifyAlgorithm = values[i];
            break;
          case "line-smooth":
            geomTrans.Smooth = values[i];
            break;
          case "line-offset":
            geomTrans.Offset = values[i];
            break;
          case "line-geometry-transform":
            geomTrans.GeometryTransform = values[i];
            break;
        }
      }

      symLine.GeometryExpression = ToGeometryExpression(geomTrans);

      return symLine;
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
                    pp.Alignments = new LabelAlignment[] { LabelAlignment.MiddleCenter };
                  break;
                case "text-placement-type":
                  break;
              }
            }
          }
          else if (lpi.Symbolizer == (int)SymbolizerType.Shield)
          {
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
                    pp.Alignments = new LabelAlignment[] { LabelAlignment.MiddleCenter };
                  break;
                case "shield-placement-type":
                  break;
                case "shield-text-dx":
                  text_dx = Convert.ToSingle(kv.Value);
                  break;
                case "shield-text-dy":
                  text_dy = Convert.ToSingle(kv.Value);
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
        case "vertex":
          throw new NotImplementedException();
        default:
          throw new Exception("Unknown placement type " + lpi.Placement);
      }

      return result;
    }

    private string ApplyTextTransform(string text, string transform)
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

    public override ParameterCollection ToDatasourceParameters(CartoLayer layer)
    {
      CartoDatasource datasource = layer.Datasource;
      ParameterCollection parameters = new ParameterCollection();

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
            paramValue = PrepareSqlQuery(paramValue);
            parameters.Add(new Parameter("Query", paramValue));
            parameters.Add(new Parameter("Table", GetSqlTableName(paramValue)));
          }

          if (datasource.TryGetValue("geometry_field", out paramValue))
            parameters.Add(new Parameter("GeometryField", paramValue));
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
          else
            parameters.Add(new Parameter("Encoding", "UTF-8"));

          layer.Srs = "+proj=longlat +ellps=WGS84 +datum=WGS84 +no_defs";
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
          tableName = tableName.Substring(pos, pos1 - pos);
        }

        pos = tableName.IndexOf(", )", StringComparison.OrdinalIgnoreCase);
        if (pos > 0)
        {
          tableName = tableName.Substring(0, pos);
        }
      }

      return tableName;
    }

    public static int FindFirstNotOf(string source, string chars, int pos)
    {
      if (source == null)
        throw new ArgumentNullException("source");
      if (chars == null) throw new ArgumentNullException("chars");
      if (source.Length == 0) return -1;
      if (chars.Length == 0) return 0;

      for (int i = pos; i < source.Length; i++)
      {
        if (chars.IndexOf(source[i]) == -1) return i;
      }
      return -1;
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
          return LineJoin.Miter;
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
          return LineCap.Flat;
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
      List<LabelAlignment> alignments = new List<LabelAlignment>();
      for (int i = 0; i < pv.Length; i++)
      {
        float size;
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

    private string ToExpression(string value)
    {
      if (string.IsNullOrEmpty(value))
        return value;

      if (value == "''")
        return "";
      // value = RemoveQuotes(value.Replace("\"", string.Empty).Replace("'", string.Empty));
      //TODO
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
      if (key != null && key.Contains("mapnik::geometry_type"))
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

      return (bAddSquareBrackets ? ("[" + key + "]") : key) + " " + op + " " + value;
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
  }
}
