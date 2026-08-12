# QuartzDistribution Flat Hotspot Annotations

## Runtime setup

1. Instantiate `Prefabs/FlatAnnotationGroup.prefab` under a screen-space Canvas.
2. Keep `MarKActions` on the always-active prefab root and set its `mObjectID`.
3. The DLL's `objectCenterPosition` is interpreted as screen pixels. `MarKActions` converts it into Canvas coordinates, moves the scan-ring anchor, and all lines rebuild from that moving anchor every frame.
4. `Start` and `Move` entries in `ObjectDetect.mObjectDic` show the whole group. Missing IDs, `End`, and `Undetect` hide it.

`FlatHotspotDemo.unity` contains two simulated tracking IDs, each connected to three fixed information cards. Use **Tools > Quartz Distribution > Rebuild Flat Hotspot Demo** to regenerate it.

## Development simulation

- Enable `simulateTrackingData` and set `simulatedPosition`/`simulatedAngle` in the Inspector. Positions use `simulationReferenceResolution` coordinates and are scaled to the current screen.
- Enable `simulateMotion` to make the DLL sample move continuously and verify that completed lines keep following.
- Runtime development code can call `SetSimulatedTrackingData(position, angle, detected)`, `PushDllSimulationSample(screenPosition, angle, state)`, or `StopSimulation()`.
- Simulation writes a real `DetectObjectDetails` value into `ObjectDetect.mObjectDic`; `MarKActions` then reads it through the same path as production detection.

## MarkerDetect integration

`Assets/Plugins/MarkerDetect.dll` and its `StreamingAssets` configuration are copied from QuartzMarkerDetect. The supplied DLL's sample `ObjectDetect` component has a registration-helper runtime error, so the flat development scene uses `MarKActions`' DLL simulation entry instead of attaching that component. Production detection can populate the same static dictionary normally.
