//==========================================================================================
//
//		MapSurfer.Styling.Formats.CartoCSS.Parser.Tree
//		Copyright (c) 2008-2016, MapSurfer.NET
//
//    Authors: Maxim Rylov
// 
//    A C# port of the carto library written by Mapbox (https://github.com/mapbox/carto/)
//    and released under the Apache License Version 2.0.
//
//==========================================================================================
using dotless.Core.Parser.Tree;

namespace MapSurfer.Styling.Formats.CartoCSS.Parser.Tree
{
  internal class CartoInvalidElement : Element
  {
    public CartoInvalidElement(Combinator combinator, string text)
      : base(combinator, "Invalid property or value: '" + text +"'")
    {
    }
  }
}
