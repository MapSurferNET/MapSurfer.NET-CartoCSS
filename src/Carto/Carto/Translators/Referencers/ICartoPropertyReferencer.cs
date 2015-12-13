//==========================================================================================
//
//		MapSurfer.Styling.Formats.CartoCSS
//		Copyright (c) 2008-2015, MapSurfer.NET
//
//    Authors: Maxim Rylov
//
//==========================================================================================
using System;

namespace MapSurfer.Styling.Formats.CartoCSS.Translators.Referencers
{
  public interface ICartoPropertyReferencer
  {
    string GetSymbolizerName(string property);

    bool HasRequiredProperties(string symbolizer, string[] properties, ref string missingProperty);

    void Prepare();
  }
}
