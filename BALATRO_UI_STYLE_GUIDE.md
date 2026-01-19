# Balatro UI Style Guide

This document serves as the **Single Source of Truth** for the Balatro UI aesthetic. All future apps and components must adhere to these definitions to maintain the authentic "CRT/Pixel-Art" look and feel.

## 1. Color Palette

### Base Colors (The "Authentic" Set)
| Name | Hex | CSS Variable | Notes |
|------|-----|--------------|-------|
| **Red** | `#ff4c40` | `--color-red` | Primary action / Mult |
| **Dark Red** | `#a02721` | `--color-dark-red` | Red button shadow |
| **Blue** | `#0093ff` | `--color-blue` | Chips / Secondary action |
| **Dark Blue** | `#0057a1` | `--color-dark-blue` | Blue button shadow |
| **Orange** | `#ff9800` | `--color-orange` | Attention / Back buttons |
| **Dark Orange** | `#a05b00` | `--color-dark-orange` | Orange button shadow |
| **Gold** | `#eaba44` | `--color-bright-gold` | Money / High value text |
| **Dark Gold** | `#b8883a` | `--color-dark-gold` | Gold button shadow |
| **Green** | `#429f79` | `--color-green` | Success / Money backing |
| **Dark Green** | `#215f46` | `--color-dark-green` | Green button shadow |
| **Purple** | `#7d60e0` | `--color-purple` | Tarot / Magic |
| **Dark Purple** | `#292189` | `--color-dark-purple` | Purple shadow |
| **Black** | `#000000` | `--color-black` | Pure black |
| **White** | `#FFFFFF` | `--color-white` | Pure white |

### Modal & Panel Colors (The "Teal Twinge")
The UI is not pure grey; it has a distinct teal/slate tint.

| Name | Hex | Usage |
|------|-----|-------|
| **Modal BG** | `#3a5055` | Main background for cards/panels |
| **Modal Inner** | `#1e2b2d` | Inner inset areas / dark backgrounds |
| **Border** | `#b9c2d2` | "Bright Silver" borders |
| **Shadow** | `#0b1415` | Drop shadows |

## 2. Typography

*   **Header Font**: `m6x11plusplus` (or equivalent pixel font). Used for buttons, headers, and titles.
    *   *Letter Spacing*: `0.1em` (Generous spacing is key).
    *   *Text Shadow*: Almost always uses a hard drop shadow (`2px 2px 0 rgba(0,0,0,0.8)`).
*   **Body Font**: `m6x11plusplus`. Used for descriptions.
*   **Number Font**: `m6x11plusplus` (Monospaced if possible for alignment).

## 3. Component Styles

### A. The "Tactical Sinking" Button
Buttons in Balatro are physical. They have a "side" (shadow) that disappears when pressed.

*   **Default State**:
    *   `border-radius`: `0.5rem`
    *   `box-shadow`: `0 4px 0 [dark-variant]`
    *   `transform`: `translateY(0)`
    *   `transition`: `none` (Instant response)
*   **Hover State**:
    *   `transform`: `translateY(-1px)`
    *   `filter`: `brightness(1.1)`
    *   `box-shadow`: `0 5px 0 [dark-variant-darker]`
*   **Active (Pressed) State**:
    *   `transform`: `translateY(4px)`
    *   `box-shadow`: `none`
    *   `filter`: `brightness(0.9)`

### B. Panels & Cards
*   **Background**: `#3a5055` (Modal BG)
*   **Border**: `2px solid rgba(0,0,0,0.2)` or `#b9c2d2` for high contrast.
*   **Corner Radius**: `0.75rem`
*   **Shadow**: `0 4px 0 rgba(0,0,0,0.4)`

### C. Inputs
*   **Background**: `rgba(0, 0, 0, 0.4)`
*   **Text**: White, Pixel Font.
*   **Border**: `2px solid transparent` (Changes to Blue `#0093ff` on focus).
*   **Inner Shadow**: `inset 0 2px 4px rgba(0, 0, 0, 0.5)`

## 4. Visual Effects (The "Juice")

### A. CRT Overlay
A subtle scanline effect overlaid on the entire screen.
```css
pointer-events: none;
background: repeating-linear-gradient(
    to bottom,
    rgba(0, 0, 0, 0.1),
    rgba(0, 0, 0, 0.1) 2px,
    transparent 2px,
    transparent 4px
);
opacity: 0.15;
```

### B. Swirl Background
The iconic shifting gradient background.
*   **Colors**: `#2c3e50`, `#0099DB`, `#c0392b`, `#8E44AD`
*   **Animation**: `swirl 20s ease infinite` (Pan background position).

### C. "Juice" Pop
Used when items are created or clicked.
*   **Keyframes**: Rapid scale up (1.15) -> scale down (0.95) -> settle (1.0).
*   **Rotation**: Slight tilt (`-5deg` to `4deg`) adds character.

## 5. CSS Reference Implementation

```css
:root {
    /* Semantic Aliases */
    --balatro-red: #ff4c40;
    --balatro-blue: #0093ff;
    --balatro-orange: #ff9800;
    --balatro-green: #429f79;
    --balatro-gold: #eaba44;
    
    --balatro-modal-bg: #3a5055;
    --balatro-modal-shadow: #0b1415;
}

.balatro-button {
    background-color: var(--balatro-red);
    box-shadow: 0 4px 0 var(--color-dark-red);
    color: white;
    font-family: 'm6x11plusplus';
    padding: 0.6rem 1.25rem;
    border-radius: 0.5rem;
    text-transform: uppercase;
    text-shadow: 2px 2px 0 rgba(0,0,0,0.5);
}

.balatro-button:active {
    transform: translateY(4px);
    box-shadow: none;
}
```
