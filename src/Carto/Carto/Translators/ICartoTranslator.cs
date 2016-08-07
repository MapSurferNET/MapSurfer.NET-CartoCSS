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
using MapSurfer.Logging;

namespace MapSurfer.Styling.Formats.CartoCSS.Translators
{
  internal interface ICartoTranslator
  {
    Dictionary<string, FontSet> FontSets { get; set; }

    string GetSymbolizerName(string property);

    bool IsSymbolizerPropertyValid(string symbolizer, NodePropertyValue property);

    bool HasRequiredProperties(string symbolizer, NodePropertyValue[] properties, ref string missingProperty);

    bool IsFontSetProperty(string value);

    Symbolizer ToSymbolizer(string symbolizer, NodePropertyValue[] properties);

    CompositingMode ToCompositingMode(string comp);

    string ToCompositingOperation(CompositingMode mode);

    string ToCoordinateSystem(string srs, bool name = false);

    ParameterCollection ToDatasourceParameters(CartoDatasource datasource);

    CartoDatasource ToDatasource(ParameterCollection parameters);

    string ToImageFilter(string filter);

    string ToPath(string url);
    
    string ToFilter(string key, string op, string value);

    ImageResamplingMode ToImageResamplingMode(string mode);
    
    void ProcessStyles(FeatureTypeStyleCollection styles);

    void SetLogger(Logger logger);
  }
}
