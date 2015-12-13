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

using MapSurfer.Drawing;
using MapSurfer.Configuration;
using MapSurfer.Drawing.Drawing2D;
using MapSurfer.Styling;

namespace MapSurfer.Styling.Formats.CartoCSS.Translators
{
  internal interface ICartoTranslator
  {
    Dictionary<string, FontSet> FontSets { get; set; }

    string GetSymbolizerName(string property);

    bool HasRequiredProperties(string symbolizer, string[] properties, ref string missingProperty);

    Symbolizer ToSymbolizer(string symbolizer, string[] properties, string[] values);

    CompositingMode ToCompositingMode(string comp);

    string ToCoordinateSystem(string srs, bool name = false);

    ParameterCollection ToDatasourceParameters(CartoLayer layer);

    string ToImageFilter(string filter);

    string ToPath(string url);
    
    void ProcessStyles(FeatureTypeStyleCollection styles);
  }
}
