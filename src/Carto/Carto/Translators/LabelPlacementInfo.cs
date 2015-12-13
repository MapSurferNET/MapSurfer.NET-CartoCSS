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

namespace MapSurfer.Styling.Formats.CartoCSS.Translators
{
  internal class LabelPlacementInfo
  {
    public string Placement { get; set; }
    public List<KeyValuePair<string, string>> Properties { get; set; }
    public int Symbolizer { get; set; }

    public LabelPlacementInfo()
    {
      Placement = "point";
      Properties = new List<KeyValuePair<string, string>>();
    }

    public string GetPropertyValue(string key)
    {
      foreach (KeyValuePair<string, string> kv in Properties)
        if (kv.Key == key)
          return kv.Value;

      return null;
    }
  }

}
