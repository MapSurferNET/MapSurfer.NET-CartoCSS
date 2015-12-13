//==========================================================================================
//
//		MapSurfer.Styling.Formats.CartoCSS
//		Copyright (c) 2008-2015, MapSurfer.NET
//
//    Authors: Maxim Rylov
//
//==========================================================================================
using System.Collections.Concurrent;

using OSGeo.OSR;

namespace MapSurfer.Styling.Formats.CartoCSS.Translators
{
  internal static class SpatialReferenceUtility
  {
    private static ConcurrentDictionary<string, string> _srCache;

    static SpatialReferenceUtility()
    {
      _srCache = new ConcurrentDictionary<string, string>();
      MapSurfer.Data.GDAL.GDALWrapper.GDALEnvironment.Initialize();
    }

    public static string ToCoordinateSystem(string srs, bool isName)
    {
      if (string.IsNullOrEmpty(srs))
        return srs;

      string result = null;
      if (_srCache.TryGetValue(srs, out result))
        return result;

      using (SpatialReference sr = new SpatialReference(null))
      {
        if (isName)
        {
          int srsId;
          if (int.TryParse(srs, out srsId))
            sr.ImportFromEPSG(srsId);
          else
          {
            // TODO
          }
        }
        else
        {
          if (srs.StartsWith("+proj=merc +a=6378137 +b=6378137 +lat_ts=0.0 +lon_0=0.0 +x_0=0.0"))
          {   //      +proj=merc +a=6378137 +b=6378137 +lat_ts=0.0 +lon_0=0.0 +x_0=0.0 +y_0=0.0 +k=1.0 +units=m +nadgrids=@null +wktext +no_defs
              //sr.ImportFromProj4(srs);
            sr.ImportFromEPSG(900913);
          }
          else
          {
            sr.ImportFromProj4(srs);
          }
        }

        sr.ExportToWkt(out result);

        if (result != null)
          _srCache.TryAdd(srs, result);

        return result;
      }
    }
  }
}
