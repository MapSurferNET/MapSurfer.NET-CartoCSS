//==========================================================================================
//
//		MapSurfer.Styling.Formats.CartoCSS
//		Copyright (c) 2008-2015, MapSurfer.NET
//
//    Authors: Maxim Rylov
//
//==========================================================================================
using System;

namespace MapSurfer.Styling.Formats.CartoCSS.Translators
{
  internal class GeometryTransformInfo
  {
    public string DisplacementX { get; set; }
    public string DisplacementY { get; set; }   
    public string Smooth { get; set; }
    public string Simplify { get; set; }
    public string SimplifyAlgorithm { get; set; }
    public string Offset { get; set; }
    public string GeometryTransform { get; set; }
    
    public bool IsEmpty
    {
    	get
    	{
    		return !(!string.IsNullOrEmpty(DisplacementX) || !string.IsNullOrEmpty(DisplacementY) || !string.IsNullOrEmpty(Offset) || !string.IsNullOrEmpty(GeometryTransform) || !string.IsNullOrEmpty(Smooth) || !string.IsNullOrEmpty(Simplify) || !string.IsNullOrEmpty(SimplifyAlgorithm));
    	}
    }
  }
}
