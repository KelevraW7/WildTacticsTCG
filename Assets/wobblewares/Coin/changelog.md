# Changelog
All notable changes to this package will be documented in this file.

## [1.0.5] - 2024-09-16
- Updated: Package distribution has been changed to simplify usage and installation. Ensure you delete the previous version of wobblecoin if updating from an older version.
  - wobblecoin now imports into the `Assets/wobblewares/Coin` folder instead of the Packages folder to simplify maintenance and render pipeline compatibility.
  - wobblekit (a free wobbly vfx kit) is now included as a dependency inside the `Assets/wobblewares/Kit` folder and used exclusively for wobblewares sample scenes.
  - URP assets are now separated into a URP.unitypackage that can be imported if required. You must install URP assets for both wobblecoin and wobblekit for the sample scenes.
- Fixed: Fixed bug where angular velocity was being incorrectly calculated on descent
- Fixed: NaN bug affecting SwipeToToss sample scene

## [1.0.4] - 2023-06-20
- Fixed: package now appears in Package folder instead of Assets folder
- Fixed: samples are not included by default and installed via package manager

## [1.0.3] - 2023-06-20
- Added: extra sample scene
- Fixed: restructured package for maintainability

## [1.0.2] - 2023-06-20
- Added: universal render pipeline support
- Added: 6 new coin designs

## [1.0.1] - 2023-06-20
- Initial release with prefab, mesh, scripts and samples