# MapSurfer.NET-CartoCSS
MapSurfer.NET-CartoCSS is a module for reading map styles written in a CartoCSS-like language. This library is a C# port of [carto](https://github.com/mapbox/carto) library written by MapBox.

## Requirements
- [MapSurfer.NET](https://www.nuget.org/packages?q=mapsurfer.net) v2.5 or greater
- [dotless](https://github.com/dotless/dotless)
- [YamlDotNet](https://github.com/aaubry/YamlDotNet)

## Installation
This library can be used as a plugin for MapSurfer.NET. In order MapSurfer.NET could detect this library, it must be installed/copied into the following folder:
**MapSurfer.NET Installation Folder**\Core\Plugins\StylingFormats\CartoCSS 

## Styling Features

### Not Supported

#### Tilemill/Mapnik syntax

- raster-filter-factor
- raster-mesh-size
- shield-text-dx
- shield-text-dy
- shield-horizontal-alignment
- shield-vertical-alignment
- shield-justify-alignment
- shield-transform
- shield-min-padding
- shield-comp-op
- shield-unlock-image
- line-pattern-comp-op
- text-halo-rasterizer
- text-ratio
- text-horizontal-alignment
- text-vertical-alignment
- text-min-padding
- text-comp-op
- text-simplify-algorithm
- point-transform
- point-comp-op
- marker-multi-policy
- marker-max-error
- marker-geometry-transform
- marker-comp-op
- polygon-gamma
- polygon-gamma-method
- polygon-comp-op
- polygon-simplify-algorithm
- line-comp-op
- line-rasterizer
- line-simplify-algorithm
