//==========================================================================================
//
//		MapSurfer.Styling.Formats.CartoCSS
//		Copyright (c) 2008-2016, MapSurfer.NET
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

    bool IsSymbolizerPropertyValid(string symbolizer, NodePropertyValue property);

    bool HasRequiredProperties(string symbolizer, NodePropertyValue[] properties, ref string missingProperty);

    void Prepare();
  }
}
