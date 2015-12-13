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
using dotless.Core.Parser.Infrastructure.Nodes;
using dotless.Core.Parser.Infrastructure;
using dotless.Core.Parser.Tree;
using System;

namespace MapSurfer.Styling.Formats.CartoCSS.Parser.Tree
{
  internal class CartoFieldNode : TextNode, IOperable
  {
    public CartoFieldNode(string content)
      : base(content)
    {
    }

    public Node Operate(Operation op, Node other)
    {
      var otherField = other as CartoFieldNode;

      if (otherField == null)
      {
        Quoted otherQuoted = other as Quoted;

        return new Quoted(this.Value + op.Operator + ConvertUtility.QuoteValue(otherQuoted.Value), otherQuoted.Escaped);
      }
      else
      {
        return new CartoFieldNode(this.Value + otherField.Value);
      }      
    }

    public Color ToColor()
    {
      throw new NotImplementedException();
    }
  }
}
