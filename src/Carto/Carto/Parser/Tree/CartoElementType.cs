//==========================================================================================
//
//		MapSurfer.Styling.Formats.CartoCSS.Parser.Tree
//		Copyright (c) 2008-2015, MapSurfer.NET
//
//    Authors: Maxim Rylov
// 
//    A C# port of the carto library written by Mapbox (https://github.com/mapbox/carto/)
//    and released under the Apache License Version 2.0.
//
//==========================================================================================
using System;

namespace MapSurfer.Styling.Formats.CartoCSS.Parser.Tree
{
  public enum ElementType : byte
  {
    Unknown,
    Class,
    Id,
    Wildchar
  }
}
