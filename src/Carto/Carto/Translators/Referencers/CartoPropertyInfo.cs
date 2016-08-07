//==========================================================================================
//
//		MapSurfer.Styling.Formats.CartoCSS
//		Copyright (c) 2008-2016, MapSurfer.NET
//
//    Authors: Maxim Rylov
//
//==========================================================================================

namespace MapSurfer.Styling.Formats.CartoCSS.Translators.Referencers
{
  internal class CartoPropertyInfo
  {
    public string CssName { get; set; }
    public bool Required { get; set; }
    public int Priority;

    public CartoPropertyInfo(string name):this(name, false)
    { }

    public CartoPropertyInfo(string name, bool required)
      : this(name, required, 0)
    {
    }

    public CartoPropertyInfo(string name,bool required = false, int priority = 0)
    {
      CssName = name;
      Required = required;
      Priority = priority;
    }
  }
}
