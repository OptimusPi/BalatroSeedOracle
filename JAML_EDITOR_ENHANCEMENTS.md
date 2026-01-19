# JAML Editor Enhancement Plan
## Making It REAL...SOLID...GREAT...COZY 🎯

Based on research into modern code editor best practices and YAML editor features, here's a comprehensive enhancement plan to make the JAML editor world-class.

---

## 🎨 Visual Enhancements

### 1. **Error Squiggles & Underlines** ⚠️
**Priority: HIGH**

- Red squiggly underlines for syntax errors
- Yellow warnings for schema violations
- Blue info for suggestions
- Click error → jump to problem

**Implementation:**
```csharp
// Use AvaloniaEdit's TextMarkerService
var markerService = new TextMarkerService(_jamlEditor.Document);
_jamlEditor.TextArea.TextView.BackgroundRenderers.Add(markerService);

// Mark errors
var marker = markerService.Create(startOffset, length);
marker.MarkerColor = Colors.Red;
marker.MarkerTypes = TextMarkerTypes.SquigglyUnderline;
```

**Benefits:**
- Immediate visual feedback
- No need to check status bar
- Professional editor feel

---

### 2. **Hover Tooltips** 💡
**Priority: HIGH**

- Hover over property → show description from schema
- Hover over joker → show joker info (rarity, description)
- Hover over anchor → show anchor definition
- Hover over error → show error details

**Implementation:**
```csharp
_jamlEditor.TextArea.TextView.MouseHover += (s, e) => {
    var position = _jamlEditor.GetPositionFromPoint(e.GetPosition(_jamlEditor));
    if (position.HasValue) {
        var word = GetWordAtPosition(position.Value);
        var tooltip = GetTooltipForWord(word);
        ShowTooltip(tooltip, e.GetPosition(_jamlEditor));
    }
};
```

**Tooltip Content:**
- **Property**: Description from `jaml.schema.json`
- **Joker**: Name, rarity, description from `BalatroData`
- **Anchor**: Full definition preview
- **Error**: Error message + fix suggestion

---

### 3. **Current Line Highlighting** ✨
**Priority: MEDIUM**

- Subtle background highlight on current line
- Makes it easy to see where you are

**Implementation:**
```csharp
_jamlEditor.Options.HighlightCurrentLine = true;
_jamlEditor.TextArea.IndentationSize = 2;
```

---

### 4. **Bracket Matching** 🔗
**Priority: MEDIUM**

- Highlight matching brackets `[]`, `{}`
- Highlight when cursor is on bracket
- Visual indicator for nested structures

**Implementation:**
```csharp
_jamlEditor.TextArea.TextView.BracketMatchingBrush = Brushes.Yellow;
_jamlEditor.Options.EnableBracketMatching = true;
```

---

### 5. **Minimap** 🗺️
**Priority: LOW**

- Small overview of entire file on right side
- Click to jump to section
- Shows structure at a glance

**Note**: AvaloniaEdit may need custom implementation or third-party control.

---

## 🧭 Navigation Features

### 6. **Go to Definition** 🎯
**Priority: HIGH**

- Ctrl+Click on anchor reference (`*anchor_name`) → jump to definition
- Ctrl+Click on property → jump to schema definition
- Right-click → "Go to Definition"

**Implementation:**
```csharp
_jamlEditor.TextArea.TextView.MouseDown += (s, e) => {
    if (e.KeyModifiers.HasFlag(KeyModifiers.Control)) {
        var position = GetPositionFromPoint(e.GetPosition(_jamlEditor));
        var word = GetWordAtPosition(position);
        if (IsAnchorReference(word)) {
            var definition = FindAnchorDefinition(word);
            JumpToPosition(definition);
        }
    }
};
```

---

### 7. **Find All References** 🔍
**Priority: MEDIUM**

- Right-click anchor definition → "Find All References"
- Shows all places where anchor is used
- Click result → jump to reference

**Implementation:**
- Parse document for all `*anchor_name` references
- Show in popup/panel
- Highlight in editor

---

### 8. **Anchor Navigation Panel** 📋
**Priority: MEDIUM**

- Sidebar showing all anchors defined in file
- Click anchor → jump to definition
- Shows anchor name + preview
- Color-coded: defined vs referenced

---

## 🛠️ Editing Features

### 9. **Format on Save** 🎨
**Priority: MEDIUM**

- Auto-format YAML on save (Ctrl+S)
- Consistent indentation (2 spaces)
- Sort properties (optional)
- Clean up whitespace

**Implementation:**
```csharp
private void FormatJaml()
{
    var deserializer = new DeserializerBuilder().Build();
    var serializer = new SerializerBuilder()
        .WithIndentedSequences()
        .WithIndentation(2, 2)
        .Build();
    
    try {
        var obj = deserializer.Deserialize(JamlContent);
        JamlContent = serializer.Serialize(obj);
    } catch { /* Invalid YAML */ }
}
```

---

### 10. **Smart Indentation** 📏
**Priority: MEDIUM**

- Auto-indent on Enter
- Maintain indentation level
- Smart dedent for `-` list items
- Tab/Shift+Tab for indent/dedent

**Implementation:**
```csharp
_jamlEditor.Options.IndentationSize = 2;
_jamlEditor.Options.ConvertTabsToSpaces = true;
_jamlEditor.TextArea.IndentationStrategy = new DefaultIndentationStrategy();
```

---

### 11. **Code Snippets** 📝
**Priority: HIGH**

- Type `joker` → Tab → expands to full joker clause
- Type `anchor` → Tab → expands to anchor definition
- Type `and` → Tab → expands to And clause template

**Snippets:**
- `joker` → `joker: Blueprint\nantes: [1, 2]\nscore: 10`
- `anchor` → `name: &name value`
- `and` → `And:\n  Antes: [1, 2]\n  Mode: Max\n  clauses:`
- `or` → `Or:\n  - joker: Blueprint`
- `cluster` → Full cluster pattern with anchors

**Implementation:**
```csharp
_jamlEditor.TextArea.TextEntered += (s, e) => {
    if (e.Text == "\t" || e.Text == " ") {
        var word = GetCurrentWord();
        var snippet = GetSnippetForWord(word);
        if (snippet != null) {
            InsertSnippet(snippet);
        }
    }
};
```

---

### 12. **Multi-Cursor Editing** 👆
**Priority: LOW**

- Alt+Click → add cursor
- Ctrl+D → select next occurrence
- Edit multiple places at once

**Note**: May require custom implementation or AvaloniaEdit extension.

---

### 13. **Word Wrap Toggle** 📄
**Priority: LOW**

- Toggle word wrap on/off
- Useful for long lines
- Preserves indentation

**Implementation:**
```csharp
_jamlEditor.WordWrap = true; // Toggle button
```

---

## ✅ Validation & Errors

### 14. **Error Panel** 📊
**Priority: HIGH**

- Panel at bottom showing all errors
- Click error → jump to line
- Group by severity (Error/Warning/Info)
- Count badges

**Implementation:**
```csharp
public class ErrorPanel : UserControl
{
    public ObservableCollection<ErrorItem> Errors { get; }
    
    // Update on validation
    private void OnValidationComplete(List<ValidationError> errors)
    {
        Errors.Clear();
        foreach (var error in errors)
        {
            Errors.Add(new ErrorItem {
                Line = error.Line,
                Message = error.Message,
                Severity = error.Severity
            });
        }
    }
}
```

---

### 15. **Real-Time Schema Validation** ✅
**Priority: HIGH**

- Validate against `jaml.schema.json` in real-time
- Check property names, types, enums
- Validate antes ranges (1-8)
- Validate slot ranges (0-5)

**Implementation:**
```csharp
private void ValidateAgainstSchema(string jaml)
{
    var schema = LoadSchema("jaml.schema.json");
    var validator = new JsonSchemaValidator(schema);
    var errors = validator.Validate(jaml);
    
    foreach (var error in errors)
    {
        MarkError(error.Line, error.Column, error.Message);
    }
}
```

---

### 16. **Quick Fixes** 🔧
**Priority: MEDIUM**

- Lightbulb icon on errors
- Click → show suggested fixes
- "Add missing property"
- "Fix indentation"
- "Quote string"

**Implementation:**
```csharp
private void ShowQuickFixes(int line, int column)
{
    var fixes = GetQuickFixesForError(line, column);
    var menu = new ContextMenu();
    foreach (var fix in fixes)
    {
        menu.Items.Add(new MenuItem {
            Header = fix.Description,
            Command = new RelayCommand(() => ApplyFix(fix))
        });
    }
    menu.Open(_jamlEditor);
}
```

---

## 🎯 Anchor-Specific Features

### 17. **Anchor Visual Indicators** 🎨
**Priority: MEDIUM**

- Highlight anchor definitions with subtle background
- Highlight anchor references with different color
- Show connection line (optional, advanced)

**Implementation:**
```csharp
private void HighlightAnchors()
{
    var anchors = FindAllAnchors(_jamlEditor.Text);
    foreach (var anchor in anchors)
    {
        if (anchor.IsDefinition)
        {
            MarkWithColor(anchor.Range, Colors.LightBlue);
        }
        else
        {
            MarkWithColor(anchor.Range, Colors.LightGreen);
        }
    }
}
```

---

### 18. **Anchor Rename** ✏️
**Priority: MEDIUM**

- Rename anchor definition → updates all references
- F2 to rename
- Preview changes before applying

**Implementation:**
```csharp
private void RenameAnchor(string oldName, string newName)
{
    // Find all references
    var references = FindAllReferences(oldName);
    
    // Replace in document
    foreach (var ref in references)
    {
        _jamlEditor.Document.Replace(ref.Offset, ref.Length, newName);
    }
}
```

---

### 19. **Anchor Validation** ✅
**Priority: MEDIUM**

- Warn if anchor is defined but never used
- Error if anchor is referenced but not defined
- Suggest similar anchor names (typo detection)

---

## 🔍 Search & Navigation

### 20. **Advanced Search** 🔎
**Priority: MEDIUM**

- Ctrl+F → Find
- Ctrl+H → Replace
- Regex support
- Match case
- Whole word
- Search in selection

**Note**: AvaloniaEdit has built-in search, but can be enhanced.

---

### 21. **Go to Line** 📍
**Priority: LOW**

- Ctrl+G → Go to line number
- Quick navigation in large files

**Implementation:**
```csharp
private void GoToLine(int lineNumber)
{
    var line = _jamlEditor.Document.GetLineByNumber(lineNumber);
    _jamlEditor.CaretOffset = line.Offset;
    _jamlEditor.TextArea.Caret.BringCaretToView();
}
```

---

## 📚 Documentation & Help

### 22. **Inline Documentation** 📖
**Priority: MEDIUM**

- Show property descriptions inline (optional)
- Toggle documentation panel
- Link to full schema docs

---

### 23. **Context-Sensitive Help** ❓
**Priority: LOW**

- F1 on property → open help
- Link to YAML best practices
- Link to JAML examples

---

## 🎨 Polish & UX

### 24. **Undo/Redo Stack** ↩️
**Priority: MEDIUM**

- Better undo/redo (AvaloniaEdit has this, but can be enhanced)
- Show undo stack in menu
- Clear undo on save (optional)

---

### 25. **Copy with Syntax** 📋
**Priority: LOW**

- Copy as formatted YAML
- Copy as JSON (converted)
- Copy as code block (for markdown)

---

### 26. **Line Numbers with Errors** 🔢
**Priority: MEDIUM**

- Highlight line numbers with errors
- Click line number → select line
- Show error count per line

---

### 27. **Status Bar Enhancements** 📊
**Priority: LOW**

- Show cursor position (line:column)
- Show selection length
- Show file encoding
- Show indentation mode (spaces/tabs)

---

## 🚀 Performance

### 28. **Lazy Validation** ⚡
**Priority: MEDIUM**

- Only validate visible portion (for large files)
- Debounce validation (already done!)
- Cache validation results

---

### 29. **Incremental Parsing** 📈
**Priority: LOW**

- Only re-parse changed sections
- Faster for large files

---

## 🎯 Priority Summary

### **Phase 1: Core Polish** (HIGH Priority)
1. ✅ Error Squiggles & Underlines
2. ✅ Hover Tooltips
3. ✅ Go to Definition
4. ✅ Code Snippets
5. ✅ Error Panel
6. ✅ Real-Time Schema Validation

### **Phase 2: Navigation** (MEDIUM Priority)
7. Find All References
8. Anchor Navigation Panel
9. Format on Save
10. Smart Indentation
11. Quick Fixes
12. Anchor Visual Indicators

### **Phase 3: Advanced Features** (LOW Priority)
13. Minimap
14. Multi-Cursor Editing
15. Word Wrap Toggle
16. Advanced Search
17. Anchor Rename
18. Inline Documentation

---

## 📝 Implementation Notes

### AvaloniaEdit Capabilities
- ✅ Syntax highlighting (already done)
- ✅ Code folding (already done)
- ✅ Autocomplete (just added!)
- ✅ Line numbers (already done)
- ✅ Bracket matching (needs enabling)
- ✅ Current line highlight (needs enabling)
- ⚠️ Error markers (needs TextMarkerService)
- ⚠️ Hover tooltips (needs custom implementation)
- ⚠️ Go to definition (needs custom implementation)

### Dependencies Needed
- **JsonSchema.Net** - For schema validation
- **YamlDotNet** - Already have it!
- **Custom Controls** - For error panel, anchor panel

---

## 🎉 Expected Impact

After implementing Phase 1:
- **Professional feel** - Error squiggles, tooltips, go-to-definition
- **Faster editing** - Code snippets, smart indentation
- **Fewer errors** - Real-time validation, error panel
- **Better navigation** - Go to definition, find references

**Result**: A **cozy**, **productive**, **professional** JAML editor that rivals VS Code! 🚀

---

**Last Updated**: 2025-01-XX  
**Status**: Research Complete - Ready for Implementation
