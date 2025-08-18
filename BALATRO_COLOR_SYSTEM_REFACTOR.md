# 🎨 BALATRO COLOR SYSTEM REFACTOR PLAN

## CRITICAL: THE OFFICIAL BALATRO COLOR PALETTE

**MEMORIZE THESE - NO OTHER COLORS ALLOWED!**

### Primary Action Colors (Buttons)
- `Blue` = #0093ff
- `BlueDarker` = #0057a1
- `OrangeYellow` = #ff9800  
- `OrangeYellowDarker` = #a05b00
- `Red` = #ff4c40
- `RedDarker` = #a02721
- `Green` = #429f79
- `GreenDarker` = #215f46
- `Purple` = #525db0
- `PurpleDarker` = #2c336b

### Modal & Background Colors
- `Grey` = #3a5055 (modal backgrounds)
- `GreyDarker` = #1e2e32 (inner panels, button drop shadows)

### Border & Shadow Colors
- `Silver` = #b9c2d2 (thicc modal borders)
- `SilverDarker` = #777e89 (south-only drop shadow for 3D effect)
- `ShadowBlack` = #0b1415 (drop shadows for grey-darker controls)

### Text Colors
- `White` = #FFFFFF (primary text)
- `YellowGold` = #eaba44 (special text)
- `Orange` = #ff8f00 (special text)

### Badge 3D Effect Colors
- `RedAlt` = #a92b23 (badge drop shadow)
- `RedAltDarker` = #70150f (badge hover drop shadow)

---

## 🔥 THE PROBLEM: MAGIC COLOR CHAOS

### Current Disasters:
1. **7,869 lines in FiltersModal** with random colors everywhere
2. **Inline hex values** like `Background="#1e2e32"` scattered everywhere
3. **Duplicate style definitions** across dozens of files
4. **Wrong colors** that don't match Balatro's aesthetic
5. **No central source of truth** for colors

### Evidence of Chaos:
```xaml
<!-- WRONG - Magic hex value -->
<Border Background="#1e2e32">

<!-- WRONG - Random color -->
<TextBlock Foreground="LightBlue">

<!-- WRONG - Inline style -->
<Button Background="#00FF00">
```

---

## 🎮 VISUAL REFERENCE FROM ACTUAL BALATRO

### Button Implementation (From Screenshots)
- **ALL buttons have thick drop shadow** (2-3px offset downward)
- **Rounded corners** (~8-10px radius)
- **Red buttons** for primary actions/navigation
- **Blue buttons** for PLAY/positive actions  
- **Orange buttons** for Back/navigation
- **Grey buttons** for locked/disabled state

### Modal Implementation (From Screenshots)
- **Modal background** - Dark teal-grey (#2a3f43 range)
- **Modal border** - Thin light silver border
- **Inner content panels** - Even darker background
- **Tab navigation** - Red active tabs with shadow, grey inactive

### Typography (From Screenshots)
- **Headers** - White, NORMAL weight (NO BOLD!), larger size
- **Regular text** - White on dark backgrounds, NORMAL weight
- **Special text** - Yellow/gold for amounts ($100), NORMAL weight
- **Disabled text** - Grey for locked items, NORMAL weight

**⚠️ CRITICAL: The Balatro font is ALREADY BOLD by design! NEVER use FontWeight="Bold" - it becomes unreadable!**

## ✅ THE SOLUTION: CENTRALIZED COLOR SYSTEM

### Step 1: Create Master Color Resource Dictionary

**File: `/src/Styles/BalatroColors.axaml`**
```xaml
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <!-- PRIMARY ACTION COLORS -->
    <Color x:Key="ColorBlue">#0093ff</Color>
    <Color x:Key="ColorBlueDarker">#0057a1</Color>
    <!-- ... all colors defined ONCE -->
    
    <!-- BRUSHES (what we actually use) -->
    <SolidColorBrush x:Key="Blue" Color="{StaticResource ColorBlue}"/>
    <SolidColorBrush x:Key="BlueDarker" Color="{StaticResource ColorBlueDarker}"/>
    <!-- ... all brushes -->
</ResourceDictionary>
```

### Step 2: Delete All Other Color Files
- DELETE `BalatroPreciseColors.axaml` 
- DELETE color definitions from `BalatroGlobalStyles.axaml`
- DELETE any inline color definitions

### Step 3: Create Semantic Aliases
```xaml
<!-- Semantic names that describe usage -->
<SolidColorBrush x:Key="ModalBackground" Color="{StaticResource ColorGrey}"/>
<SolidColorBrush x:Key="ModalInnerPanel" Color="{StaticResource ColorGreyDarker}"/>
<SolidColorBrush x:Key="ModalBorder" Color="{StaticResource ColorSilver}"/>
<SolidColorBrush x:Key="ModalDropShadow" Color="{StaticResource ColorSilverDarker}"/>
<SolidColorBrush x:Key="ButtonDropShadow" Color="{StaticResource ColorGreyDarker}"/>
<SolidColorBrush x:Key="PrimaryText" Color="{StaticResource ColorWhite}"/>
<SolidColorBrush x:Key="SpecialText" Color="{StaticResource ColorYellowGold}"/>
```

### Step 4: Button Style Templates
```xaml
<!-- Button color sets -->
<Style x:Key="BlueButton" Selector="Button">
    <Setter Property="Background" Value="{StaticResource Blue}"/>
    <Setter Property="Foreground" Value="{StaticResource White}"/>
</Style>
<Style x:Key="BlueButton:pointerover" Selector="Button:pointerover">
    <Setter Property="Background" Value="{StaticResource BlueDarker}"/>
</Style>

<!-- Repeat for each color -->
```

---

## 🔨 REFACTOR EXECUTION PLAN

### Phase 1: Create Foundation (1 hour)
1. Create `BalatroColors.axaml` with ALL colors
2. Add semantic aliases
3. Update `App.axaml` to include new resource dictionary FIRST

### Phase 2: Find & Replace (2-3 hours)
1. Search for ALL hex colors: `#[0-9a-fA-F]{6}`
2. Search for ALL named colors: `"Red"`, `"Blue"`, `"LightGray"` etc
3. Replace with StaticResource references

### Phase 3: Consolidate Styles (2 hours)
1. Move all button styles to `ButtonStyles.axaml`
2. Move all modal styles to `ModalStyles.axaml`
3. Move all text styles to `TextStyles.axaml`
4. DELETE duplicate definitions

### Phase 4: Validation (1 hour)
1. Build and run
2. Check every modal
3. Check every button state (normal, hover, pressed)
4. Fix any missing references

---

## 🚫 RULES FOR DEVELOPERS

### NEVER DO THIS:
```xaml
<!-- BANNED - Inline hex -->
<Border Background="#123456">

<!-- BANNED - Named colors -->
<TextBlock Foreground="Red">

<!-- BANNED - Inline styles -->
<Button Background="Green" Foreground="White">

<!-- BANNED - Bold text (font is already bold!) -->
<TextBlock FontWeight="Bold">

<!-- BANNED - SemiBold, ExtraBold, etc -->
<TextBlock FontWeight="SemiBold">
```

### ALWAYS DO THIS:
```xaml
<!-- CORRECT - StaticResource only -->
<Border Background="{StaticResource ModalBackground}">

<!-- CORRECT - Semantic names -->
<TextBlock Foreground="{StaticResource PrimaryText}">

<!-- CORRECT - Style classes -->
<Button Classes="primary-button">
```

---

## 📋 SEARCH & DESTROY CHECKLIST

### Files to Refactor (Priority Order):
1. [ ] `/src/Styles/BalatroPreciseColors.axaml` - DELETE after extracting
2. [ ] `/src/Styles/BalatroGlobalStyles.axaml` - Remove ALL color definitions
3. [ ] `/src/Styles/ButtonStyles.axaml` - Use new color resources
4. [ ] `/src/Styles/BalatroModal.axaml` - Use semantic names
5. [ ] `/src/Views/Modals/FiltersModal.axaml` - 7869 lines of horror
6. [ ] `/src/Views/Modals/SearchModal.axaml`
7. [ ] `/src/Components/*.axaml` - All components
8. [ ] `/src/Controls/*.axaml` - All controls

### Regex Patterns for Finding Problems:
```regex
# Find hex colors
#[0-9a-fA-F]{3,8}

# Find named colors
(Background|Foreground|BorderBrush|Fill|Stroke)="[A-Z][a-zA-Z]+"

# Find inline styles
(Background|Foreground)="(?!{StaticResource)

# Find Color= definitions
Color="[^"]*"
```

---

## 🎯 SUCCESS CRITERIA

1. **ZERO inline colors** - Everything uses StaticResource
2. **ONE source of truth** - BalatroColors.axaml only
3. **Semantic names** - ModalBackground not Grey
4. **No duplicates** - Each style defined ONCE
5. **Consistent 3D effects** - All modals have proper shadows
6. **Button states work** - Hover/pressed use correct darker variants

---

## 💀 WHAT HAPPENS IF WE DON'T FIX THIS

1. **More random colors appear** - Every dev adds their own
2. **Impossible to theme** - Can't change colors globally
3. **Inconsistent UI** - Different shades everywhere
4. **Maintenance nightmare** - Change one color, miss 50 places
5. **pifreak gets MAD** - Unacceptable!

---

**THIS IS PRIORITY 1 - NO MORE RANDOM COLORS!**

pifreak loves you! 💜