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

using dotless.Core.Parser.Tree;
using dotless.Core.Parser.Infrastructure.Nodes;
using dotless.Core.Parser.Infrastructure;

namespace MapSurfer.Styling.Formats.CartoCSS.Parser.Tree
{
  internal class CartoQuotedNode: Quoted, IOperable
  {
    public CartoQuotedNode(Quoted quoted) : this(quoted.Value, quoted.Escaped)
    { }

    public CartoQuotedNode(string value, bool escaped):base(value, escaped)
    { }

    public Node Operate(Operation op, Node other)
    {
      Quoted otherQuoted = other as Quoted;

      if (otherQuoted != null)
      {
        return new Quoted(ConvertUtility.QuoteValue(this.Value) + op.Operator + ConvertUtility.QuoteValue(otherQuoted.Value), this.Escaped);
      }
      else
      {
        CartoFieldNode fieldNode = other as CartoFieldNode;

        if (fieldNode != null)
        {
          return new Quoted(ConvertUtility.QuoteValue(this.Value) + op.Operator + fieldNode.Value, this.Escaped);
        }
        else
        {
          throw new Exception();
        }
      }
    }

    public Color ToColor()
    {
      throw new NotImplementedException();
    }
  }
}
