# Project Index: Oracle (AvaloniaUI, Motely, Balatro)

## 1. Overview

- **UI Framework:** AvaloniaUI (.NET 9, C# 13)
- **Core Features:** Sprite management, asset-driven UI, SIMD-powered search (Motely), Balatro-inspired design
- **Key Libraries:** Avalonia, Motely, SkiaSharp, DuckDB

---

## 2. Asset & Sprite System

### SpriteService (src\Services\SpriteService.cs)
- **Purpose:** Loads, indexes, and crops sprites for UI elements (Jokers, Tags, Tarots, Spectrals, Vouchers)
- **Data Sources:** JSON files (e.g., `vouchers.json`) define sprite positions; PNG sheets provide graphics
- **Usage:**  
  - `GetJokerImage`, `GetVoucherImage`, etc. return cropped images for UI
  - `GetItemImage` auto-detects type
  - `SpriteExists` checks for sprite presence
- **Extensibility:** Add new assets by updating JSON and PNG files; no code changes needed

### Asset Structure
- `Assets/Jokers/`, `Assets/Tags/`, `Assets/Tarots/`, `Assets/Vouchers/`
  - Each contains a `.json` (positions) and `.png` (sheet)

---

## 3. Motely: SIMD Searching

### What is Motely?
- **Motely** is a C# library for high-performance, vectorized (SIMD) searching and filtering.
- **Use Case:** Fast, parallel filtering of game data (e.g., voucher combinations, seed searches).

### How to Use Motely in AvaloniaUI
- **Integration:**  
  - Use Motely’s SIMD search in your view models or services to process large datasets (e.g., searching for voucher combos).
  - Display results in AvaloniaUI controls (DataGrid, ListBox, etc.).
- **Example Pattern:**
  1. Use Motely to filter/search your data (e.g., seeds, vouchers).
  2. Bind results to AvaloniaUI controls for instant, interactive feedback.
- **Benefits:**  
  - Lightning-fast search, even for large datasets.
  - Smooth UI experience—no blocking or lag.

### Example (Pseudo-code)