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
using dotless.Core.Parser.Infrastructure.Nodes;

namespace MapSurfer.Styling.Formats.CartoCSS.Parser.Tree
{
  internal class CartoAttachmentElement : Element
  {
    private string m_value;
    public CartoAttachmentElement(Combinator combinator, Node value, string content)
      : base(combinator, value)
    {
      m_value = content;
    }

    public new string Value
    {
      get
      {
        return m_value;
      }
    }
  }
}
