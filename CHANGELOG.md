# CHANGE LOG

All notable changes to this project will be documented in this file.

#### Known Issues

- Indirect rendering has a glitch in editor scene view where it sometimes doesn't change even on forced scene repaint and needs user to go over the sceneview

## RELEASES

### Release 0.9.13

#### Fixed

- fixed malformed meta files

### Release 0.9.12

#### Changed

- added additional check for indirect arguments support for gpus/drivers that incorrectly return number of arguments supported 

### Release 0.9.11

#### Changed

- bumped Native Collections dependency to 2.4.3 (preparing for Unity 6)
- fixed ToArray to ToArrayNBC for compatibility with latest Native Collection

### Release 0.9.10

#### Fixed

- fixed tags for Unity 2022
- try catch editor asset creation to crash silently if Unity misbehaves

#### Changed

- allocate and work with visibility buffer only when culling is enabled

### Release 0.9.9

#### Added

- option to tranform whole clusters using handles or direct transform

### Release 0.9.8

#### Added

- settings to specify include raycast layer mask

#### Changed

- changed started refactoring on merging all the different raycast approaches, will continue to merge it into unified raycast pipeline and clean the experiments

#### Changed

### Release 0.9.7

#### Fixed

- fixed skip rendering if all cameras are hidden/inactive for some reason

### Release 0.9.6

#### Changed

- changed disposing approach to eliminate NativeCollection allocation hangups on assembly reloads

### Release 0.9.5

#### Changed

- changed InstancePainterConfig now saves to Assets/Editor/Resources instead of Assets/Resources/Editor to avoid build embedding, migration of old configs to new path is also implemented

### Release 0.9.4

#### Added

- added fallback material property for paint definition so it will create cluster with both material and fallback material in place

### Release 0.9.3

#### Added

- added option to enable/disable inactive gameobject raycasting

### Release 0.9.2

#### Fixed

- fixed added explicit max value of instances per batch for culling shader so it avoids working on data outside of valid buffer in additional GPU threads
- fixed invalidate culling shader instance when exiting play mode

### Release 0.9.1

#### Changed

- changed reworked interface serialization to avoid serialization callbacks and splitting
- changed PaintTool now needs to enable use direction explicitly for directional drawing, for using single click painting without direction

#### Changed

- fixed when using directional drawing Painting with Updates using Control wasn't working

### Release 0.9.0

#### Changed

- changed now merget editor/runtime to single assembly definition
- changed namespaces to match other tooling

#### Fixed

- fixed null clusters are now valid Asset clusters and get serialized as such
- fixed on cluster asset change now correctly force native serialization

### Release 0.8.0

#### Added

- added curve tool [EXPERIMENTAL]
- added enable experimental in settings
- added modifiers volume RECT/SPHERE 

#### Changed

- changed multiple UI changes and updates

#### Fixed

- fixed correct faceculling for depth and shadow pass
- fixed deserialization issues when disabling/enabling whole gameobject in editor
- fixed gpu invalidation on data change before modifiers

### Release 0.7.0 - 18.10.2022 RELEASE CANDIDATE

#### Added

- Added optional raycasting for moving using modify tool
- Added cluster tool as separate tooling
- Added generate game objects from instance cluster
- Added gradient distribution option for color painting

#### Changed

- Changed color and minimum distance now part of paint definition and removed from tool config
- Changed cluster changes moved from modify tool to cluster tool
- Changed modifying instance colors moved from paint to modify tool
- Changed InstanceDefinition made obsolete and migration to PaintDefinition [BREAKING]
- Changed refactoring on cluster methods

#### Fixed

- Fixed serialization issues on tool configurations

### Release 0.6.5 - 12.9.2022 RELEASE CANDIDATE

#### Changed

- Changed scene view rendering now skipped in play mode to avoid multiple issues in various prefab stages and culling options

#### Fixed

- Fixed on accessing incorrect camera in prefab stage
- Fixed edit mode execution for prefab stage exit

### Release 0.6.4 - 12.9.2022 RELEASE CANDIDATE

#### Added

- Added frustum and distance GPU culling for indirect rendering

### Release 0.6.3 - 24.8.2022 RELEASE CANDIDATE

#### Changed

- Changed additional UI visuals and styles
- Changed include layers removed for now till they are working as intended

#### Fixed

- Fixed geometry fetching inside prefab stage for correct paint/raycast in prefab stage

### Release 0.6.2 - 23.8.2022 RELEASE CANDIDATE

#### Added

- Added new UI changes for instance renderer editor

#### Fixed

- Fixed reserialization on internal data when adding clusters happen immediately
- Fixed minor ui response issues

### Release 0.6.1 - 22.8.2022 RELEASE CANDIDATE

#### Fixed

- Fixed tool properties editing

### Release 0.6.0 - 21.8.2022 RELEASE CANDIDATE

#### Added

- Added completely reworked UI editors and inspectors to have better user experience
- Added warnings about no mesh, material, etc states

#### Changed

- Changed every tool has now its own standalone settings
- Changed disabled clusters are uneffected by painting

#### Fixed

- Fixed a lot of lock states
- Fixed not possible to add same cluster twice to renderer
- Fixed not possible to add same paint definiton twice to painter

### Release 0.5.4 - 12.8.2022

#### Fixed

- Fixed set dirty on renderer properties change

### Release 0.5.3 - 2.7.2022

#### Fixed

- Fixed editor namespaces for prefab stage

### Release 0.5.2 - 1.7.2022

#### Added

- Added when adding clusters specify asset/bound type

#### Fixed

- Fixed undo/redo on cluster asset operations
- Fixed dispose Unity crash on render disable/enable in editor
- Fixed memory leaks on ScriptableObject dealocation on assembly reload

### Release 0.5.0 - 22.6.2022

#### Added

- Completely refactored version if Instance Painter
- Different renderer, clusters, handling...

### Release 0.4.11 - 14.6.2022

#### Added

- Added instance renderers window to show all active renderers in the scene and their stats

#### Changed

- Changed when instanced rendering is now awailable even for fallback it will now fail silently

### Release

### Release 0.4.10 - 7.6.2022

#### Fixed

- fixed modified native containers initialized even for scenarios before renderer is enabled

### Release 0.4.9 - 30.5.2022

#### Changed

- fixed updating of modfied data and buffers between fallback/nonfallback rendering

### Release 0.4.8 - 30.5.2022

#### Changed

- changed fallback rendering now doesn't allocate any memory for batches

### Release 0.4.7 - 27.5.2022

#### Fixed

- fixed OnDisable is disposing only when we are not in playmode
- fixed when in fallback initialization now correctly still copies modified data

### Release 0.4.6 - 24.5.2022

#### Added

- added IsInitialized for IPRenderer
- added renderbounds for instanced rendering

#### Changed

- changed cleanup of invalidation order

### Release 0.4.6 - 23.5.2022

#### Fixed

- fixed rounding issue

### Release 0.4.4 - 23.5.2022

#### Fixed

- fixed modified data length used instead of original for batches

### Release 0.4.3 - 23.5.2022

#### Added

- added fallback rendering now uses DrawMeshInstanced

#### Fixed

- fixed fallback shaders cleaned up and fixed matrix transformations

### Release 0.4.2 - 16.5.2022

#### Changed

- changed how buffers are handled for modifiers, no buffer change same instance count just buffer update
- changed removed visibility property for modifiers
- changed removed IPVisibilityModifier

### Release 0.4.1 - 11.5.2022

#### Fixed

- fixed Instance Painter enabled state now gets correctly saved

### Release 0.4.0 - 9.5.2022

#### Added

- added binning support for modifier space checks
- added IPUnityEnderer that enumerates meshfilters in parent and puts them into instances
- added support for prefab stage editing

#### Changed

- changed colliders are now removed modifiers have rect area for binning
- changed various refactors in codebase
- changed Unity Collections dependency to 1.2.3

#### Fixed

- fixed OnEnable/OnDisable invalidation now removed
- fixed UNITY 2020 prefab stage namespace vs 2021

### Release

### Release 0.3.1 - 11.4.2022

#### Changed

- changed shader categorization (fallbacks under category)

#### Fixed

- fixed undo/redo which wasn't invalidating correctly since NativeCollection rework

### Release 0.3.0 - 7.4.2022

#### Added

- all fallback shaders now have feature parity
- forceFallback property to explicitly use fallback rendering 

#### Changed

- fallback property was removed once you have fallbackMaterial it will automatically fallback to it if HW doesn't meet instancing requirements

#### Fixed

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
