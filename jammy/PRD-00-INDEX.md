# Balatro Seed Oracle — Avalonia UI Rewrite PRDs

## Overview

This folder contains **Product Requirement Documents (PRDs)** for a complete rewrite of the Avalonia UI layer of Balatro Seed Oracle. Each PRD covers one self-contained subsystem, scoped for independent implementation.

The existing codebase has ~200+ UI C# files, ~77 AXAML views, ~48 ViewModels, 28 services, 17 converters, 9 behaviors, 11 modals, and 13 widgets. These PRDs capture every feature at a high level so the rewrite can be done cleanly without referencing the old code.

---

## PRD Index

| PRD | Subsystem | Priority | Dependencies |
|-----|-----------|----------|--------------|
| [PRD-01](PRD-01-APP-SHELL.md) | App Shell & Main Window | P0 | None (foundation) |
| [PRD-02](PRD-02-SHADER-BACKGROUND.md) | GPU Shader Background | P0 | PRD-01 |
| [PRD-03](PRD-03-AUDIO-SYSTEM.md) | Audio & Music System | P1 | PRD-01 |
| [PRD-04](PRD-04-SHADER-REACTIVITY.md) | Shader Reactivity & EventFX | P1 | PRD-02, PRD-03 |
| [PRD-05](PRD-05-TRANSITION-ENGINE.md) | Transition Engine | P1 | PRD-02 |
| [PRD-06](PRD-06-SEARCH-SYSTEM.md) | Search System | P0 | PRD-01, PRD-11 |
| [PRD-07](PRD-07-FILTER-BUILDER.md) | Filter Builder | P0 | PRD-01, PRD-11 |
| [PRD-08](PRD-08-SEED-ANALYZER.md) | Seed Analyzer | P1 | PRD-01, PRD-11 |
| [PRD-09](PRD-09-WIDGET-SYSTEM.md) | Widget System | P1 | PRD-01 |
| [PRD-10](PRD-10-UI-KIT.md) | Balatro UI Kit (Custom Controls) | P0 | None (shared) |
| [PRD-11](PRD-11-MODAL-SYSTEM.md) | Modal System | P0 | PRD-01, PRD-10 |
| [PRD-12](PRD-12-SERVICES-PLATFORM.md) | Services & Platform Layer | P0 | None (foundation) |

---

## Recommended Build Order

```
Phase 1 — Foundation
  PRD-12  Services & Platform Layer
  PRD-10  Balatro UI Kit
  PRD-01  App Shell & Main Window
  PRD-11  Modal System

Phase 2 — Core Features
  PRD-02  GPU Shader Background
  PRD-06  Search System
  PRD-07  Filter Builder

Phase 3 — Polish & Delight
  PRD-03  Audio & Music System
  PRD-05  Transition Engine
  PRD-04  Shader Reactivity & EventFX
  PRD-08  Seed Analyzer
  PRD-09  Widget System
```

---

## Tech Stack (Rewrite)

- **Framework:** Avalonia 11+
- **Pattern:** MVVM with CommunityToolkit.Mvvm
- **Rendering:** SkiaSharp via Avalonia Composition API
- **DI:** Microsoft.Extensions.DependencyInjection
- **Platforms:** Desktop (Windows/Mac/Linux), Browser (WASM), iOS, Android
- **Icons:** IconPacks.Avalonia (Material + Feather)
- **Fonts:** Custom BalatroFont

---

## Legacy File Counts (for reference)

| Category | Count |
|----------|-------|
| AXAML files | 77 |
| ViewModels | 48 |
| Services | 28+ |
| Converters | 17 |
| Behaviors | 9 |
| Modals | 11 |
| Widgets | 13 |
| Custom Controls | 20+ |
