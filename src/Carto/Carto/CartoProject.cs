//==========================================================================================
//
//		MapSurfer.Styling.Formats.CartoCSS
//		Copyright (c) 2008-2015, MapSurfer.NET
//
//    Authors: Maxim Rylov
//
//==========================================================================================
using System.Collections.Generic;

using Newtonsoft.Json;

using YamlDotNet.Serialization;

namespace MapSurfer.Styling.Formats.CartoCSS
{
  internal class CartoProject
  {
    public CartoProject()
    {
      Generator = "mapnik";
    }

    [JsonProperty("generator")] // Tilemill, GeoServer or MapSurfer.NET
    [YamlMember(Alias = "generator")]
    public string Generator { get; set; }

    [JsonProperty("bounds")]
    [YamlMember(Alias = "bounds")]
    public string[] Bounds { get; set; }

    [JsonProperty("center")]
    [YamlMember(Alias = "center")]
    public string[] Center { get; set; }

    [JsonProperty("format")]
    [YamlMember(Alias = "format")]
    public string Format { get; set; }

    [JsonProperty("interactivity")]
    [YamlMember(Alias = "interactivity")]
    public object Interactivity { get; set; }

    [JsonProperty("minzoom")]
    [YamlMember(Alias = "minzoom")]
    public int MinZoom { get; set; }

    [JsonProperty("maxzoom")]
    [YamlMember(Alias = "maxzoom")]
    public int MaxZoom { get; set; }

    [JsonProperty("srs")]
    [YamlMember(Alias = "srs")]
    public string Srs { get; set; }

    [JsonProperty("srs-name")]
    [YamlMember(Alias = "srs-name")]
    public string SrsName { get; set; }

    [JsonProperty("Stylesheet")]
    [YamlMember(Alias = "Stylesheet")]
    public string[] Stylesheet { get; set; }

    [JsonProperty("Layer")]
    [YamlMember(Alias = "Layer")]
    public CartoLayer[] Layers { get; set; }

    [JsonProperty("scale")]
    [YamlMember(Alias = "scale")]
    public float Scale { get; set; }

    [JsonProperty("metatile")]
    [YamlMember(Alias = "metatile")]
    public int Metatile { get; set; }

    [JsonProperty("name")]
    [YamlMember(Alias = "name")]
    public string Name { get; set; }
  
    [JsonProperty("description")]
    [YamlMember(Alias = "description")]
    public string Description { get; set; }

    [JsonProperty("attribution")]
    [YamlMember(Alias = "attribution")]
    public string Attribution { get; set; }

    [JsonProperty("legend")]
    [YamlMember(Alias = "legend")]
    public string Legend { get; set; }

    [YamlMember(Alias = "_parts")]
    [YamlIgnore]
    public CartoParts _Parts { get; set; }

    public Dictionary<string, object> GetInteractivity()
    {
      return Interactivity as Dictionary<string, object>;
    }
  }

  internal class CartoParts : Dictionary<string, Dictionary<string, string>>
  {
  }

  internal class CartoLayer
  {
    public CartoLayer()
    {
      Class = string.Empty;
    }

    [JsonProperty("id")]
    [YamlMember(Alias = "id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    [YamlMember(Alias = "name")]
    public string Name { get; set; }

    [JsonProperty("srs")]
    [YamlMember(Alias = "srs")]
    public string Srs { get; set; }

    [JsonProperty("srs-name")]
    [YamlMember(Alias = "srs-name")]
    public string SrsName { get; set; }

    [JsonProperty("class")]
    [YamlMember(Alias = "class")]
    public string Class { get; set; }

    [JsonProperty("geometry")]
    [YamlMember(Alias = "geometry")]
    public string Geometry { get; set; }

    [JsonProperty("Datasource")]
    [YamlMember(Alias = "Datasource")]
    public CartoDatasource Datasource { get; set; }

    [JsonProperty("properties")]
    [YamlMember(Alias = "properties")]
    public Dictionary<string, string> Properties { get; set; }
    
    [JsonProperty("status")]
    [YamlMember(Alias = "status")]
    public string Status { get; set; }

    [JsonProperty("extent")]
    [YamlMember(Alias = "extent")]
    public string[] Extent { get; set; }

    public override string ToString()
    {
      return string.Format("Id={0};Name={1};Class={2}", Id, Name, Class);
    }
  }

  internal class CartoDatasource : Dictionary<string, string> 
  {
    public CartoDatasource()
    {
    }

    public string Type
    {
      get
      {
        if (this.ContainsKey("type"))
          return this["type"];
        else
        {
          if (ContainsKey("file"))
          {
            string file = this["file"];

            string ext = System.IO.Path.GetExtension(file);
            switch (ext)
            {
              case ".json":
              case ".geojson":
              case ".topojson":
                return "GeoJson";
              case ".shp":
                return "Shape";
              case ".csv":
              case ".kml":
                return "OGR";
              default:
                break;
            }
          }

          return "Shape";
        }
      }
    }
  }

  internal class CartoParameter
  {
    public string Name { get; set; }

    public string Value { get; set; }
  }
}
