# MASTER TODO - ALL INCOMPLETE PRD WORK

**Date:** 2025-11-05
**Reality Check:** I HAVE NOT FINISHED THESE PRDS!
**Status:** Consolidated master list of ALL incomplete work

---

## 🔥 CRITICAL Priority (Must Complete Before Release)

### 1. ❌ Filter & Score UI Ultimate Refactor
**File:** FILTER_SCORE_UI_ULTIMATE_REFACTOR_PRD.md
**Status:** READY FOR IMPLEMENTATION
**Priority:** 🔥 CRITICAL

**What Needs To Happen:**
- OR/AND clause workflow (PARTIALLY DONE - needs testing!)
- Edition/Sticker/Seal buttons (DONE)
- Grouped clause display (DONE)
- BUT: Architecture refactor NOT done (FilterBuilderItemViewModel not integrated)
- BUT: Save/load NOT tested
- BUT: Happy path NOT working

**Current Status:** ~50% complete

---

### 2. ❌ SearchInstance.cs Refactor
**File:** SEARCHINSTANCE_REFACTOR_PRD.md
**Status:** NOT STARTED
**Priority:** CRITICAL - Performance & Correctness

**What Needs To Happen:**
- Thread-safety issues
- Race conditions in search state
- Proper cancellation handling
- Memory leak fixes

**Current Status:** 0% complete

---

### 3. ❌ Live Results Query Optimization
**File:** LIVE_RESULTS_OPTIMIZATION_PRD.md
**Status:** READY FOR IMPLEMENTATION
**Priority:** HIGH - Performance

**What Needs To Happen:**
- Optimize live results queries during search
- Reduce UI lag when results stream in
- Performance improvements for large result sets

**Current Status:** 0% complete

---

## 🎨 HIGH Priority (Polish & UX)

### 4. ❌ Music Visualizer Refactor
**File:** MUSIC_VISUALIZER_REFACTOR_PRD.md
**Status:** READY FOR IMPLEMENTATION
**Priority:** HIGH - Required for release

**What Needs To Happen:**
- Split into 3 independent components:
  1. FFT Window (audio analysis)
  2. Audio Mixer Widget
  3. Shader Scene Builder
- Each saves its own config independently
- Modular architecture

**Current Status:** 0% complete

---

### 5. ⚠️ Music Visualizer Settings Cleanup
**File:** MUSIC_VISUALIZER_SETTINGS_CLEANUP.md
**Status:** PARTIAL IMPLEMENTATION
**Priority:** HIGH - UX improvement

**What Needs To Happen:**
- Remove disconnected settings
- Remove "ADVANCED SETTINGS" expander
- Add test buttons
- Clean up clutter

**Current Status:** ~20% complete (widget works but needs cleanup)

---

### 6. ❌ Standard Playing Card UX
**File:** STANDARD_PLAYING_CARD_UX.md
**Status:** Unknown
**Priority:** Medium

**Current Status:** Unknown % complete

---

### 7. ❌ Visual Builder Card Display
**File:** VISUAL_BUILDER_CARD_DISPLAY_PRD.md
**Status:** Unknown
**Priority:** Medium

**Current Status:** Unknown % complete

---

## 📊 Completion Summary

| PRD | Status | Priority | % Complete |
|-----|--------|----------|------------|
| Filter & Score UI Refactor | PARTIAL | 🔥 CRITICAL | 50% |
| SearchInstance Refactor | NOT STARTED | 🔥 CRITICAL | 0% |
| Live Results Optimization | NOT STARTED | HIGH | 0% |
| Music Visualizer Refactor | NOT STARTED | HIGH | 0% |
| Music Visualizer Cleanup | PARTIAL | HIGH | 20% |
| Standard Playing Card UX | UNKNOWN | MEDIUM | ? |
| Visual Builder Card Display | UNKNOWN | MEDIUM | ? |

**Overall Completion:** ~10% of ALL PRD work! 😱

---

## 🎯 Recommended Order of Attack

### Phase 1: Fix Filter Builder (NOW)
1. **Test OR/AND workflow** (user testing)
2. **Fix bugs found** during testing
3. **Complete save/load** for grouped clauses
4. **Get happy path working** for first time ever!

**Time:** 2-4 hours

---

### Phase 2: Critical Performance Fixes
1. **SearchInstance refactor** (thread-safety, race conditions)
2. **Live Results optimization** (reduce UI lag)

**Time:** 4-6 hours

---

### Phase 3: Music Visualizer Cleanup
1. **Settings cleanup** (remove disconnected stuff)
2. **Add test buttons**
3. **Optional:** Full refactor into 3 components (or defer post-MVP)

**Time:** 2-3 hours (cleanup only) OR 6-8 hours (full refactor)

---

### Phase 4: Sprite Pre-Loading
1. **Implement pre-load all sprites**
2. **Balatro loading screen** (research LUA files)
3. **Eliminate UI lag**

**Time:** 3-4 hours

---

### Phase 5: Final Cleanup
**Use FINAL_CLEANUP_TODO.md:**
- Magic colors
- Embarrassing comments
- MVVM violations
- Anti-patterns

**Time:** 3-5 hours

---

## 💀 BRUTAL HONESTY

**What I've Actually Completed:**
- ✅ OR/AND clause UI (footer buttons, visibility)
- ✅ Grouped clause display template
- ✅ FilterBuilderItemViewModel wrapper (NOT integrated!)
- ✅ Documentation (this file, FINAL_CLEANUP_TODO.md, etc.)

**What I Have NOT Done:**
- ❌ Tested OR/AND workflow
- ❌ Integrated FilterBuilderItemViewModel
- ❌ SearchInstance refactor
- ❌ Live Results optimization
- ❌ Music Visualizer refactor
- ❌ Music Visualizer cleanup
- ❌ Sprite pre-loading
- ❌ Most of FINAL_CLEANUP_TODO.md

**Estimated Time to MVP-Ready:** 15-20 hours of focused work

---

## 🚀 NEXT IMMEDIATE STEPS

1. **User tests OR/AND workflow** → Report bugs
2. **I fix bugs found**
3. **Get happy path working**
4. **THEN** tackle other PRDs in priority order

---

## 🎭 THE TRUTH

I got excited about the architecture refactor discussion and created documentation, but **I haven't implemented most of the PRD work!**

The user is RIGHT to call this out!

**Let's finish the fucking PRDs before release!** 🔥

