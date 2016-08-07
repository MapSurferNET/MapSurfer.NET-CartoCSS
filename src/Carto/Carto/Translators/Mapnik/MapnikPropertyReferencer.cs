//==========================================================================================
//
//		MapSurfer.Styling.Formats.CartoCSS
//		Copyright (c) 2008-2016, MapSurfer.NET
//
//    Authors: Maxim Rylov
//
//==========================================================================================
using System;

using MapSurfer.Styling.Formats.CartoCSS.Translators.Referencers;

namespace MapSurfer.Styling.Formats.CartoCSS.Translators.Mapnik
{
  internal class MapnikPropertyReferencer : CartoPropertyReferencer
  {
    public override void Prepare()
    {
      // PolygonSymbolizer
      AddTypeProperty(typeof(PolygonSymbolizer), "polygon", new CartoPropertyInfo[] {
        new CartoPropertyInfo("polygon-fill"),
        new CartoPropertyInfo("polygon-opacity"),
        new CartoPropertyInfo("polygon-gamma"),
        new CartoPropertyInfo("polygon-gamma-method"),
        new CartoPropertyInfo("polygon-clip"),
        new CartoPropertyInfo("polygon-smooth"),
        new CartoPropertyInfo("polygon-offset"),
        new CartoPropertyInfo("polygon-geometry-transform"),
        new CartoPropertyInfo("polygon-comp-op")
      });

      // LineSymbolizer
      AddTypeProperty(typeof(LineSymbolizer), "line", new CartoPropertyInfo[] {
        new CartoPropertyInfo("line-color"),
        new CartoPropertyInfo("line-width"), 
        new CartoPropertyInfo("line-opacity"), 
        new CartoPropertyInfo("line-join"),
        new CartoPropertyInfo("line-cap"),
        new CartoPropertyInfo("line-gamma"),
        new CartoPropertyInfo("line-gamma-method"),
        new CartoPropertyInfo("line-dasharray"),
        new CartoPropertyInfo("line-miterlimit"),
        new CartoPropertyInfo("line-dash-offset"),
        new CartoPropertyInfo("line-clip"),
        new CartoPropertyInfo("line-smooth"),
        new CartoPropertyInfo("line-offset"),
        new CartoPropertyInfo("line-geometry-transform"),
        new CartoPropertyInfo("line-comp-op"),
        new CartoPropertyInfo("line-rasterizer"),
      });

      // PointSymbolizer
      AddTypeProperty(typeof(PointSymbolizer), "marker", new CartoPropertyInfo[] {
        new CartoPropertyInfo("marker-file"),
        new CartoPropertyInfo("marker-opacity"),
        new CartoPropertyInfo("marker-fill-opacity"),
        new CartoPropertyInfo("marker-line-color"),
        new CartoPropertyInfo("marker-line-width"),
        new CartoPropertyInfo("marker-line-opacity"),
        new CartoPropertyInfo("marker-placement"),
        new CartoPropertyInfo("marker-multi-policy"),
        new CartoPropertyInfo("marker-type"),
        new CartoPropertyInfo("marker-width"),
        new CartoPropertyInfo("marker-height"),
        new CartoPropertyInfo("marker-fill"),
        new CartoPropertyInfo("marker-allow-overlap"),
        new CartoPropertyInfo("marker-ignore-placement"),
        new CartoPropertyInfo("marker-spacing"),
        new CartoPropertyInfo("marker-max-error"),
        new CartoPropertyInfo("marker-transform"),
        new CartoPropertyInfo("marker-clip"),
        new CartoPropertyInfo("marker-smooth"),
        new CartoPropertyInfo("marker-simplify"),
        new CartoPropertyInfo("marker-simplify-algorithm"),
        new CartoPropertyInfo("marker-geometry-transform"),
        new CartoPropertyInfo("marker-comp-op"),
      });

      AddTypeProperty(typeof(GraphicTextSymbolizer), "shield", new CartoPropertyInfo[] {
        new CartoPropertyInfo("shield-name"),
        new CartoPropertyInfo("shield-file", true),
        new CartoPropertyInfo("shield-face-name", true),
        new CartoPropertyInfo("shield-fill"),
        new CartoPropertyInfo("shield-size"),
        new CartoPropertyInfo("shield-placement"),
        new CartoPropertyInfo("shield-placements"),
        new CartoPropertyInfo("shield-placement-type"),
        new CartoPropertyInfo("shield-avoid-edges"),
        new CartoPropertyInfo("shield-allow-overlap"),
        new CartoPropertyInfo("shield-min-distance"),
        new CartoPropertyInfo("shield-spacing"),
        new CartoPropertyInfo("shield-min-padding"),
        new CartoPropertyInfo("shield-wrap-width"),
        new CartoPropertyInfo("shield-wrap-before"),
        new CartoPropertyInfo("shield-wrap-character"),
        new CartoPropertyInfo("shield-halo-fill"),
        new CartoPropertyInfo("shield-halo-opacity"),
        new CartoPropertyInfo("shield-halo-radius"),
        new CartoPropertyInfo("shield-character-spacing"),
        new CartoPropertyInfo("shield-line-spacing"),
        new CartoPropertyInfo("shield-text-dx"),
        new CartoPropertyInfo("shield-text-dy"),
        new CartoPropertyInfo("shield-dx"),
        new CartoPropertyInfo("shield-dy"),
        new CartoPropertyInfo("shield-opacity"),
        new CartoPropertyInfo("shield-text-opacity"),
        new CartoPropertyInfo("shield-horizontal-alignment"),
        new CartoPropertyInfo("shield-vertical-alignment"),
        new CartoPropertyInfo("shield-text-transform"),
        new CartoPropertyInfo("shield-justify-alignment"),
        new CartoPropertyInfo("shield-transform"),
        new CartoPropertyInfo("shield-clip"),
        new CartoPropertyInfo("shield-comp-op"),
        new CartoPropertyInfo("shield-unlock-image"),
        new CartoPropertyInfo("shield-smooth"),
        new CartoPropertyInfo("shield-simplify"),
        new CartoPropertyInfo("shield-simplify-simplify")
      });

      // LinePatternSymbolizer
      AddTypeProperty(typeof(LinePatternSymbolizer), "line-pattern", new CartoPropertyInfo[] {
        new CartoPropertyInfo("line-pattern-file", true),
        new CartoPropertyInfo("line-pattern-clip"),
        new CartoPropertyInfo("line-pattern-simplify"),
        new CartoPropertyInfo("line-pattern-simplify-algorithm"),
        new CartoPropertyInfo("line-pattern-smooth"),
        new CartoPropertyInfo("line-pattern-offset"),
        new CartoPropertyInfo("line-pattern-geometry-transform"),
        new CartoPropertyInfo("line-pattern-comp-op"),
      });

      // PolygonSymbolizer with GraphicsFill
      AddTypeProperty(typeof(PolygonSymbolizer), "polygon-pattern", new CartoPropertyInfo[] {
        new CartoPropertyInfo("polygon-pattern-file", true),
        new CartoPropertyInfo("polygon-pattern-alignment"),
        new CartoPropertyInfo("polygon-pattern-gamma"),
        new CartoPropertyInfo("polygon-pattern-opacity"),
        new CartoPropertyInfo("polygon-pattern-clip"),
        new CartoPropertyInfo("polygon-pattern-smooth"),
        new CartoPropertyInfo("polygon-pattern-comp-op"),
      });

      // RasterSymbolizer
      AddTypeProperty(typeof(RasterSymbolizer), "raster", new CartoPropertyInfo[] {
        new CartoPropertyInfo("raster-opacity"),
        new CartoPropertyInfo("raster-filter-factor"),
        new CartoPropertyInfo("raster-scaling"),
        new CartoPropertyInfo("raster-mesh-size"),
        new CartoPropertyInfo("raster-comp-op"),
        new CartoPropertyInfo("raster-colorizer-default-mode"),
        new CartoPropertyInfo("raster-colorizer-default-color"),
        new CartoPropertyInfo("raster-colorizer-epsilon"),
        new CartoPropertyInfo("raster-colorizer-stops")
      });

      AddTypeProperty(typeof(PointSymbolizer), "point", new CartoPropertyInfo[] {
        new CartoPropertyInfo("point-file"),
        new CartoPropertyInfo("point-allow-overlap"),
        new CartoPropertyInfo("point-ignore-placement"),
        new CartoPropertyInfo("point-opacity"),
        new CartoPropertyInfo("point-placement"),
        new CartoPropertyInfo("point-transform"),
        new CartoPropertyInfo("point-comp-op"),
      });

      // TextSymbolizer
      AddTypeProperty(typeof(TextSymbolizer), "text", new CartoPropertyInfo[] {
        new CartoPropertyInfo("text-name", true),
        new CartoPropertyInfo("text-face-name", true),
        new CartoPropertyInfo("text-size"),
        new CartoPropertyInfo("text-ratio"),
        new CartoPropertyInfo("text-wrap-width"),
        new CartoPropertyInfo("text-wrap-before"),
        new CartoPropertyInfo("text-wrap-character"),
        new CartoPropertyInfo("text-spacing"),
        new CartoPropertyInfo("text-character-spacing"),
        new CartoPropertyInfo("text-line-spacing"),
        new CartoPropertyInfo("text-label-position-tolerance"),
        new CartoPropertyInfo("text-max-char-angle-delta"),
        new CartoPropertyInfo("text-fill"),
        new CartoPropertyInfo("text-opacity"),
        new CartoPropertyInfo("text-halo-fill"),
        new CartoPropertyInfo("text-halo-opacity"),
        new CartoPropertyInfo("text-halo-radius"),
        new CartoPropertyInfo("text-halo-rasterizer"),
        new CartoPropertyInfo("text-dx"),
        new CartoPropertyInfo("text-dy"),
        new CartoPropertyInfo("text-align"),
        new CartoPropertyInfo("text-vertical-alignment"),
        new CartoPropertyInfo("text-avoid-edges"),
        new CartoPropertyInfo("text-min-distance"),
        new CartoPropertyInfo("text-min-padding"),
        new CartoPropertyInfo("text-min-path-length"),
        new CartoPropertyInfo("text-allow-overlap"),
        new CartoPropertyInfo("text-orientation"),
        new CartoPropertyInfo("text-placement"),
        new CartoPropertyInfo("text-placement-type"),
        new CartoPropertyInfo("text-placements"),
        new CartoPropertyInfo("text-transform"),
        new CartoPropertyInfo("text-horizontal-alignment"),
        new CartoPropertyInfo("text-align"),
        new CartoPropertyInfo("text-clip"),
        new CartoPropertyInfo("text-comp-op"),
        new CartoPropertyInfo("text-smooth"),
        new CartoPropertyInfo("text-simplify"),
        new CartoPropertyInfo("text-simplify-algorithm")
      });

      // ExtrudedPolygonSymbolizer
      AddTypeProperty(typeof(ExtrudedPolygonSymbolizer), "building", new CartoPropertyInfo[] {
        new CartoPropertyInfo("building-fill"),
        new CartoPropertyInfo("building-fill-opacity"),
        new CartoPropertyInfo("building-height")
      });
    }
  }
}
