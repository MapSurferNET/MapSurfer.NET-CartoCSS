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
using System.Collections.Generic;

using dotless.Core.Parser.Infrastructure;

namespace MapSurfer.Styling.Formats.CartoCSS.Parser.Tree
{
  internal class CartoStyle
  {
    public string Attachment { get; set; }
    public int Index { get; set; }
    public List<CartoDefinition> Definitions { get; set; }

    public CartoStyle()
    {
      Definitions = new List<CartoDefinition>();
    }

    public CartoStyle(CartoDefinition def)
    {
      Definitions = new List<CartoDefinition>();
      Definitions.Add(def);
    }

    public int Count
    {
      get
      {
        return Definitions.Count;
      }
    }

    public CartoDefinition this[int index]
    {
      get
      {
        return Definitions[index];
      }
      set
      {
        Definitions[index] = value;
      }
    }

    public void Add(CartoDefinition def)
    {
      Definitions.Add(def);
    }
   
    /// <summary>
    /// Removes dead style definitions that can never be reached
    /// when filter-mode="first". The style is modified in-place
    /// and returned. The style must be sorted.
    /// </summary>
    public void Fold(Env env)
    {
        for (var i = 0; i < Definitions.Count; i++) 
        {
           for (var j = Definitions.Count - 1; j > i; j--) 
           {
              if (Definitions[j].Filters.CloneWith(Definitions[i].Filters, env) == null)
                  Definitions.RemoveAt(j);
           }
        }    
    }
  }
}
