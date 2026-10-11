<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-modeling/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-modeling/) · [Source](https://github.com/Novolis-Platform/novolis-modeling)
<!-- novolis-pkg-brand:end -->

# Novolis.Modeling.Scene

Mesh-first 3D scene graph for editing and rendering pipelines. Typed nodes, staged evaluation (generators/modifiers → triangles), runtime mesh-edit state, and `.nov3djson` serialization. No Avalonia UI and no LLM transports.

## Install

```bash
dotnet add package Novolis.Modeling.Scene
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`). References `Novolis.Math.Geometry`.

## Quick start

```csharp
using Novolis.Modeling;

var doc = SceneDocument.CreatePrimitiveStage("Demo");
SceneSerializer.Save(doc, @"out.nov3djson");
var evaluator = new SceneEvaluator();
evaluator.Bind(doc);
```

## Evaluated mesh data

`SceneEvaluator.Cache.EvaluatedMeshes` returns the existing indexed
position/index/world values plus optional renderer-neutral mesh attributes.
Default evaluation deterministically supplies normals, tangents, planar UVs,
topology/adjacency, bounds, quality, readiness, and semantic/cache metadata.
Curvature, cavity, material slots, and LOD are opt-in through
`MeshQualityProfile` and the `EvaluatedMesh.FromEditable` overloads.
Scene evaluation can use the same policy through `SceneEvaluator.MeshOptions`.

Geometry and appearance readiness are reported separately. Use
`MeshContractValidator.ValidateAppearanceOnly` before applying an
appearance-only contract; the validator derives available attributes from the
actual evaluated data and rejects claims about omitted geometry.

## Related

| Package / app | Notes |
|---------------|-------|
| [`Novolis.Modeling.Import.Assimp`](../Novolis.Modeling.Import.Assimp/README.md) | Assimp import into editable meshes |
| [`Novolis.Avalonia.Modeling`](https://github.com/Novolis-Platform/novolis-avalonia/tree/main/src/Novolis.Avalonia.Modeling) | Scene editor UI |
| [`Novolis.Cad.SceneBridge`](https://github.com/Novolis-Platform/novolis-cad) | CadDocument → SceneDocument |
