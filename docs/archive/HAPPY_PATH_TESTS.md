# Balatro Seed Oracle - Happy Path Test Plan

**Version:** 1.1.4
**Date:** 2025-10-20
**Status:** Ready for Testing

## 🎯 Core User Workflows

### Test 1: Create a Simple Filter
**Goal:** User can create a filter using the visual designer

**Steps:**
1. Launch app
2. Click "🎨 CREATE FILTER" button
3. In Visual Builder tab:
   - Drag a "Voucher" card to the "MUST HAVE" section
   - Select "Telescope" from dropdown
   - Set antes to [1, 2]
4. Switch to "Save Filter" tab
5. Enter name: "Test-Telescope"
6. Click "💾 SAVE FILTER"
7. Verify filter appears in filter browser

**Expected Result:**
- ✅ Filter saved to `JsonItemFilters/Test-Telescope.json`
- ✅ Filter appears in filter selector
- ✅ No errors in console

---

### Test 2: Start a Search (Basic)
**Goal:** User can start a simple search

**Steps:**
1. Click "🎰 START SEARCH" button
2. Select filter: "SimpleTest" (or the one you just created)
3. Select Deck: "Red Deck"
4. Select Stake: "White Stake"
5. Click "START SEARCH" button
6. Watch progress indicator

**Expected Result:**
- ✅ Search starts (progress bar animates)
- ✅ Result count updates
- ✅ Status shows "Running"
- ✅ No crashes

---

### Test 3: Minimize Search to Desktop
**Goal:** User can minimize a running search

**Steps:**
1. While search is running (from Test 2)
2. Click "📌 MINIMIZE TO DESKTOP" button
3. Observe desktop canvas

**Expected Result:**
- ✅ SearchDesktopIcon appears on main desktop
- ✅ Icon shows filter name
- ✅ Icon shows progress
- ✅ Modal closes
- ✅ Search continues in background

---

### Test 4: Interact with Desktop Widget
**Goal:** User can control search from desktop widget

**Steps:**
1. Right-click on SearchDesktopIcon
2. Click "⏸️ Pause"
3. Verify status changes to "Paused"
4. Right-click again
5. Click "▶️ Resume"
6. Verify status changes back to "Running"

**Expected Result:**
- ✅ Pause works
- ✅ Resume works
- ✅ Context menu appears
- ✅ Icons update correctly

---

### Test 5: View Search Results
**Goal:** User can see search results

**Steps:**
1. Wait for search to find at least 1 result (or let it run for 10 seconds)
2. Right-click SearchDesktopIcon
3. Click "📊 View Results"
4. Results window should open

**Expected Result:**
- ✅ DataGrid shows results
- ✅ Columns: Seed, Score, Ante Info
- ✅ Can sort by clicking column headers
- ✅ Can scroll through results

---

### Test 6: Export Results
**Goal:** User can export results to Excel

**Steps:**
1. In Results window, click "Export" button
2. Choose "Excel (.xlsx)"
3. Choose save location
4. Open exported file

**Expected Result:**
- ✅ Excel file created
- ✅ Contains all result columns
- ✅ Data is formatted correctly
- ✅ Can open in Excel/LibreOffice

---

### Test 7: Audio Visualization (VibeOut Mode)
**Goal:** Audio visualizer works

**Steps:**
1. Close any open modals
2. Click the 🎵 music button (volume slider pops up)
3. Adjust volume slider
4. Observe background shader reacts
5. Click 🎵 again to toggle music
6. Click "VIBE OUT" button (if visible)

**Expected Result:**
- ✅ Music plays
- ✅ Volume slider works
- ✅ Background shader reacts to music
- ✅ VibeOut mode fullscreen works
- ✅ Press ESC to exit VibeOut mode

---

### Test 8: Settings Persistence
**Goal:** User settings are saved

**Steps:**
1. Click "⚙️ SETTINGS" button
2. Change theme/colors
3. Adjust visualizer settings
4. Close app
5. Reopen app

**Expected Result:**
- ✅ Settings persisted
- ✅ Theme/colors same as before
- ✅ Visualizer settings saved

---

### Test 9: Analyze a Specific Seed
**Goal:** User can analyze a known seed

**Steps:**
1. Click "🔍 ANALYZE" button
2. Enter seed: "XTTO2111"
3. Select Deck: "Red Deck"
4. Select Stake: "White Stake"
5. Click "ANALYZE"

**Expected Result:**
- ✅ Analysis completes
- ✅ Shows ante-by-ante breakdown
- ✅ Lists all items found
- ✅ Matches expected data

---

### Test 10: Multiple Concurrent Searches
**Goal:** User can run multiple searches at once

**Steps:**
1. Start Search 1 (SimpleTest filter)
2. Minimize to desktop
3. Start Search 2 (different filter)
4. Minimize to desktop
5. Start Search 3 (another filter)
6. Minimize to desktop
7. Observe all 3 widgets updating independently

**Expected Result:**
- ✅ All 3 searches run concurrently
- ✅ Each widget shows independent progress
- ✅ No interference between searches
- ✅ App remains responsive

---

## 🐛 Known Issues to Watch For

### Critical Bugs (Would prevent release):
- [ ] App crashes on startup
- [ ] Search never starts
- [ ] Results window empty/broken
- [ ] Cannot create filters
- [ ] Cannot save filters
- [ ] Desktop widgets don't appear

### Major Bugs (Should fix before release):
- [ ] Settings don't persist
- [ ] Audio doesn't work
- [ ] Export fails
- [ ] Search gets stuck
- [ ] Memory leaks during long searches
- [ ] UI freezes during search

### Minor Bugs (Can fix later):
- [ ] Tooltips missing
- [ ] Visual glitches
- [ ] Animations stuttering
- [ ] DebugLogger spam in console (already known)

---

## 🎮 Performance Tests

### Search Performance
**Test:** Search speed with SimpleTest filter
- **Expected:** 10-50 million seeds/second (depends on CPU)
- **Minimum Acceptable:** 1 million seeds/second

### UI Responsiveness
**Test:** Can interact with UI while search running?
- **Expected:** Smooth 60 FPS animations
- **Minimum Acceptable:** No freezing, clickable buttons

### Memory Usage
**Test:** Memory after 10 minutes of searching
- **Expected:** < 500MB RAM
- **Minimum Acceptable:** < 1GB RAM, no memory leaks

---

## ✅ Automated Tests (Motely Engine)

Run: `dotnet test X:/BalatroSeedOracle/external/Motely/Motely.Tests/Motely.Tests.csproj`

**Expected:** All tests pass

---

## 📝 Test Results Template

```
Date: __________
Tester: __________
Platform: Windows / Mac / Linux
.NET Version: __________

| Test | Pass | Fail | Notes |
|------|------|------|-------|
| Test 1: Create Filter | ☐ | ☐ | |
| Test 2: Start Search | ☐ | ☐ | |
| Test 3: Minimize Search | ☐ | ☐ | |
| Test 4: Widget Controls | ☐ | ☐ | |
| Test 5: View Results | ☐ | ☐ | |
| Test 6: Export Results | ☐ | ☐ | |
| Test 7: Audio/VibeOut | ☐ | ☐ | |
| Test 8: Settings Persist | ☐ | ☐ | |
| Test 9: Analyze Seed | ☐ | ☐ | |
| Test 10: Multiple Searches | ☐ | ☐ | |

**Overall Status:** PASS / FAIL
**Ready for Release:** YES / NO
**Critical Issues Found:** ___________
```

---

## 🚀 Next Steps After Testing

1. If all tests pass → **Ready for release packaging**
2. If critical bugs found → **Fix before release**
3. If minor bugs found → **Document, fix later**
4. Run performance tests → **Verify acceptable speeds**
5. Test on all platforms → **Windows, Mac, Linux**

---

**Note:** This is a MANUAL test plan. Ideally we'd automate these with integration tests, but for a GUI app, manual testing is often necessary for the happy path.
