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
using System.Linq;

using dotless.Core.Parser.Tree;
using dotless.Core.Parser.Infrastructure.Nodes;

namespace MapSurfer.Styling.Formats.CartoCSS.Parser.Tree
{
  internal class CartoElement : Element
  {
    private ElementType m_type = ElementType.Unknown;

    public CartoElement(Combinator combinator, Node value)
      : base(combinator, value)
    {
      string strValue = value.ToString().Trim();

      if (string.IsNullOrEmpty(strValue))
      {
        m_type = ElementType.Unknown;
        if (strValue != null)
          Value = "\"\"";
      }
      else if (strValue[0] == '#')
      {
        Value = strValue.Remove(0, 1);
        m_type = ElementType.Id;
      }
      else if (strValue[0] == '.')
      {
        Value = strValue.Remove(0, 1);
        m_type = ElementType.Class;
      }
      else if (strValue.Contains('*'))
      {
        m_type = ElementType.Wildchar;
      }
    }

    public ElementType Type
    {
      get
      {
        return m_type;
      }
    }

    public int[] Specificity()
    { 
          return new int[] {   (m_type == ElementType.Id) ? 1 : 0, // a
                               (m_type == ElementType.Class) ? 1 : 0  // b
                           };
    }
  }
}
