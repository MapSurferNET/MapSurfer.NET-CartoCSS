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
using System.Collections.Generic;

namespace MapSurfer.Styling.Formats.CartoCSS.Parser.Tree
{
  internal interface ISpecificity
  {
    int[] Specificity();
  }

  internal class SpecificitySorter : IComparer<ISpecificity>
  {
    int IComparer<ISpecificity>.Compare(ISpecificity a, ISpecificity b)
    {
      var asv = a.Specificity();
      var bsv = b.Specificity();

      if (asv[0] != bsv[0]) return bsv[0] - asv[0];
      if (asv[1] != bsv[1]) return bsv[1] - asv[1];
      if (asv[2] != bsv[2]) return bsv[2] - asv[2];

      return (bsv[3] - asv[3]);
    }
  }
}
