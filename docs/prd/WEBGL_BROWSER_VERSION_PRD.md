# WebGL Browser Version - Product Requirements Document

**Date**: 2025-11-16
**Feature**: BalatroSeedOracle in the Browser via WebAssembly
**Status**: FUTURE VISION - RESEARCH PHASE
**Priority**: GAME CHANGER 🚀

---

## Executive Summary

Port **BalatroSeedOracle** to run in web browsers using:
- **Avalonia + WebAssembly** (WASM)
- **Uno Platform** (alternative - better WASM support)
- **Blazor + Avalonia** (hybrid approach)

This enables:
- ✅ No installation required - just visit a URL
- ✅ Cross-platform (Windows, Mac, Linux, Mobile)
- ✅ Easy sharing - send links to seed searches
- ✅ Cloud-based search offloading (optional)

### THE DREAM:
```
https://balatro-seeds.com
  ├─ Filter Builder (visual + JSON)
  ├─ Run Search (in browser!)
  ├─ View Results (sortable grid)
  ├─ Shader Background (WebGL!)
  └─ Share Search (permalink)
```

---

## Why This is INSANE (in a good way)

### Current State:
- Desktop app (Windows/Mac/Linux)
- ~250 MB download
- Installation friction
- Hard to share filters/results

### Web Version:
- **Zero install** - just click a link
- **Instant access** - loads in ~5 seconds
- **Shareable** - `balatro-seeds.com/search/abc123`
- **Mobile-friendly** - works on phones/tablets!

---

## Technical Approaches

### Option 1: Avalonia + Browser-WASM (EXPERIMENTAL)

**Status**: Avalonia has experimental WASM support via `Avalonia.Browser`

```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Avalonia.Browser" Version="11.2.0" />
  </ItemGroup>
</Project>
```

**Pros**:
- Keep existing Avalonia codebase
- Minimal code changes
- Same MVVM architecture

**Cons**:
- **EXPERIMENTAL** - not production-ready yet
- Shader support limited (WebGL vs SkiaSharp)
- Performance unknown
- File system APIs different (IndexedDB vs actual files)

---

### Option 2: Uno Platform (BEST FOR WASM)

**Status**: Uno has mature, production-ready WASM support

Uno Platform is like Avalonia but with better WASM tooling:
- XAML-based (similar to Avalonia)
- Full WebAssembly support
- Works with Skia rendering (SkiaSharp)
- Hot reload in browser!

**Architecture**:
```
BalatroSeedOracle.Shared/
  ├─ ViewModels/ (reuse existing!)
  ├─ Models/ (reuse existing!)
  └─ Services/ (reuse existing!)

BalatroSeedOracle.Wasm/
  ├─ Views/ (Uno Platform XAML)
  ├─ wwwroot/ (web assets)
  └─ Program.cs (WASM entry point)

BalatroSeedOracle.Desktop/
  └─ (existing Avalonia app)
```

**Migration Effort**:
- **ViewModels**: Copy-paste (95% reusable)
- **Services**: Copy-paste (90% reusable, adjust file I/O)
- **XAML Views**: Convert Avalonia → Uno (80% similar syntax)
- **Shaders**: Convert SkiaSharp → WebGL/Skia

**Estimated Time**: 2-3 weeks for MVP

---

### Option 3: Blazor Hybrid (PRAGMATIC)

Build a **simplified web UI** that reuses core logic:

```
BalatroSeedOracle.Core/
  ├─ Services/FilterService.cs
  ├─ Services/SearchEngine.cs
  └─ Models/

BalatroSeedOracle.Blazor/
  ├─ Pages/FilterBuilder.razor
  ├─ Pages/SearchResults.razor
  └─ wwwroot/

BalatroSeedOracle.Desktop/
  └─ (existing Avalonia app)
```

**Pros**:
- Fastest to implement
- Clean separation
- Web-optimized UI

**Cons**:
- Have to rebuild UI in Blazor/Razor
- Less code reuse than Uno
- Two UIs to maintain

---

## Key Technical Challenges

### 1. File System Access

**Desktop**:
```csharp
var sprites = File.ReadAllBytes("Assets/Sprites/Jokers_1.png");
```

**Browser**:
```csharp
// Use IndexedDB for local storage
var db = await IndexedDbManager.GetDbAsync();
var sprites = await db.GetFile("Jokers_1.png");

// OR embed sprites in WASM binary
[assembly: WebAssemblyHostAsset("Assets/Sprites/Jokers_1.png")]
```

### 2. DuckDB (SQL Database)

**Problem**: DuckDB uses native C++ libraries - won't work in WASM!

**Solution Options**:
- **sql.js** - SQLite compiled to WASM (simpler than DuckDB)
- **In-memory search** - Keep results in JavaScript arrays
- **Server-side search** - Offload heavy searches to cloud API

### 3. Shader Background (WebGL)

**Current**: SkiaSharp SKSL shader
**Browser**: Convert to WebGL GLSL shader

```glsl
// WebGL Fragment Shader (GLSL ES 3.0)
#version 300 es
precision highp float;

uniform float u_time;
uniform vec2 u_resolution;
uniform vec3 u_mainColor;
uniform vec3 u_accentColor;

out vec4 fragColor;

void main() {
    vec2 uv = gl_FragCoord.xy / u_resolution;

    // Balatro shader math (same logic, different syntax)
    float spin = sin(u_time * 0.5);
    vec3 color = mix(u_mainColor, u_accentColor, uv.x);

    fragColor = vec4(color, 1.0);
}
```

**Conversion Tool**: SKSL → GLSL transpiler (manual for MVP)

### 4. Performance

**Seed Searching** in browser is SLOW:
- JavaScript/WASM is ~10x slower than native C#
- Can't use all CPU cores effectively

**Solution**: Hybrid approach
- **Simple searches**: Run in browser (good for trying filters)
- **Big searches**: Offload to cloud API (optional paid tier?)

---

## MVP Feature Set (Web Version)

### Phase 1: Filter Builder Only
- Visual filter builder
- JSON editor
- Preview (no actual search)
- Export filter as JSON
- **NO BACKEND** - pure frontend

### Phase 2: Client-Side Search (Limited)
- Search 10,000 seeds locally in browser
- Good for testing filters quickly
- IndexedDB for results storage

### Phase 3: Cloud Search API (Premium)
- POST filter to API
- Server runs full search (millions of seeds)
- WebSocket for progress updates
- Results sent back to browser

---

## Deployment Architecture

```
CloudFlare Pages / Netlify / Vercel
├─ Static WASM App
├─ Sprite Assets (CDN)
└─ API Gateway
    └─ Azure Functions / AWS Lambda
        └─ Search Workers (C# .NET)
```

**Hosting Cost**:
- **Free Tier**: Static hosting (CloudFlare Pages)
- **Paid Tier**: Cloud search API (~$20/month for small scale)

---

## Code Reusability Matrix

| Component | Desktop (Avalonia) | Web (Uno/Blazor) | Reuse % |
|-----------|-------------------|------------------|---------|
| ViewModels | ✅ | ✅ | 95% |
| Models | ✅ | ✅ | 100% |
| FilterService | ✅ | ✅ | 90% |
| SearchEngine | ✅ | ⚠️ (needs WASM tweaks) | 70% |
| SpriteService | ✅ | ⚠️ (IndexedDB) | 60% |
| AudioManager | ✅ | ⚠️ (Web Audio API) | 40% |
| Shaders | ✅ (SKSL) | ⚠️ (GLSL) | 50% |

---

## Implementation Roadmap

### Phase 1: Proof of Concept (1 week)
- [ ] Set up Uno Platform WASM project
- [ ] Port 1 simple view (Filter Builder)
- [ ] Verify XAML conversion process
- [ ] Test WASM build size / load time

### Phase 2: Core Features (2 weeks)
- [ ] Port all ViewModels
- [ ] Convert XAML views
- [ ] Implement IndexedDB storage
- [ ] Add sprite loading (embedded or CDN)

### Phase 3: Search Engine (2 weeks)
- [ ] Port search logic to WASM
- [ ] Replace DuckDB with sql.js
- [ ] Test search performance
- [ ] Optimize for browser constraints

### Phase 4: Cloud API (Optional) (1 week)
- [ ] Build REST API for searches
- [ ] Deploy to Azure/AWS
- [ ] Add WebSocket progress updates
- [ ] Billing/rate limiting

### Phase 5: Polish & Deploy (1 week)
- [ ] WebGL shader conversion
- [ ] Responsive mobile layout
- [ ] PWA support (installable)
- [ ] Deploy to production

**Total Time**: 7-10 weeks (with 1 developer full-time)

---

## Success Metrics

1. ✅ App loads in <5 seconds on 4G connection
2. ✅ WASM binary size <20 MB
3. ✅ Filter builder fully functional
4. ✅ Client-side search works for 10K seeds
5. ✅ Mobile-responsive (works on phones)
6. ✅ ShareURL feature works (permalink to searches)

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| WASM performance too slow | HIGH | Hybrid: light searches in browser, heavy searches via API |
| Shader conversion complex | MEDIUM | Simplify shader for web, keep advanced features desktop-only |
| File size too large | MEDIUM | Lazy-load sprites, use sprite sheets, CDN |
| Browser compatibility | LOW | Target modern browsers only (Chrome 90+, Firefox 88+) |

---

## Alternatives Considered

### Electron Desktop App
- **Pro**: Easier than WASM
- **Con**: Still requires download/install
- **Verdict**: Doesn't solve the "zero-install" goal

### React/Vue Rewrite
- **Pro**: Mature web frameworks
- **Con**: Lose ALL existing C# code
- **Verdict**: Too much work, Uno Platform better

---

## Next Steps

1. **Research Phase** (this week)
   - Try Avalonia.Browser hello world
   - Try Uno Platform WASM sample
   - Compare build sizes & performance

2. **Decision Point** (next week)
   - Choose: Avalonia.Browser vs Uno Platform vs Blazor
   - Create architecture design doc
   - Estimate effort for full port

3. **Prototype** (week 3)
   - Build minimal filter builder in WASM
   - Get feedback from users
   - Measure performance

---

## Resources

- [Avalonia.Browser Docs](https://docs.avaloniaui.net/docs/deployment/browser)
- [Uno Platform WASM Guide](https://platform.uno/docs/articles/get-started-wasm.html)
- [Awesome Avalonia List](https://github.com/AvaloniaCommunity/awesome-avalonia)
- [WebAssembly.org](https://webassembly.org/)

---

**Status**: EXCITING AS FUCK - Let's make it happen! 🚀
**Estimated MVP**: 3 months
**Potential Impact**: 10x user growth (no install barrier)
