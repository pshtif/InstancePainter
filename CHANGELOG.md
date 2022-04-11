# CHANGE LOG

All notable changes to this project will be documented in this file.

## RELEASES

### Release

### Release 0.3.1 - 11.4.2022

#### Changed

- changed shader categorization (fallbacks under category)

#### Fixes

- fixed undo/redo which wasn't invalidating correctly since NativeCollection rework

### Release 0.3.0 - 7.4.2022

#### Added

- all fallback shaders now have feature parity
- forceFallback property to explicitly use fallback rendering 

#### Changed

- fallback property was removed once you have fallbackMaterial it will automatically fallback to it if HW doesn't meet instancing requirements

#### Fixes

- fixed now avoiding normal calculation and direction in billboarding mode for all shaders

### Release 0.2.1 - 1.4.2022

#### Added

- more HW compatibility checks on SSBO access
- experimental compute buffers/shaders, still disabled by default

#### Changed

- changed raycasting implementation to avoid using some internal Unity calls that are buggy on MacOS

### Release 0.2.0 - 17.3.2022

#### Added

- added gizmos for colliders

#### Changed

- huge refactor to native collections accross the whole codebase
- optimizations accross the whole codebase

#### Fixed

## RELEASES

### Release 0.1.2 - 16.3.2022

#### Added

- added instance colliders
- added instance modifiers to modify instances within a collider
- added Color, Scale, Visibility modifiers
- added Bounds, Sphere colliders

#### Fixed

- fixed glitches in rendering on same frame as invalidation
- fixed compute buffer offset on modifier invalidation

### Release 0.1.1 - 16.3.2022

#### Added

- new shader for receiving pixel perfect shadows instead of just vertex
- new wind properties for time scaling and tiling
- new shader property for ambient lighting
- added receive shadows toggle for all shaders
- added support for billboarding with shadows

### Release 0.1.0 - 15.3.2022

#### Added
- Added initial version :)
