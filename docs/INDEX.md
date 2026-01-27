# Documentation Index

Welcome to the Balatro Seed Oracle documentation. This index provides quick access to all project documentation.

## 📚 Core Documentation

### Getting Started
- **[README.md](../README.md)** - Main project overview, installation, and quick start guide
- **[GIT_SUBMODULE_SETUP.md](../GIT_SUBMODULE_SETUP.md)** - Setting up the Motely submodule

### Architecture & Design
- **[ARCHITECTURE.md](ARCHITECTURE.md)** - Platform abstraction patterns, MVVM architecture, AOT compilation
- **[PLATFORM_GUIDE.md](PLATFORM_GUIDE.md)** - Cross-platform development guide
- **[AVALONIA_BEST_PRACTICES.md](AVALONIA_BEST_PRACTICES.md)** - Avalonia UI framework patterns
- **[AVALONIA_LICENSE_SETUP.md](AVALONIA_LICENSE_SETUP.md)** - Avalonia license configuration

### Development
- **[AI_CODING_GUIDELINES.md](../AI_CODING_GUIDELINES.md)** - Guidelines for AI coding agents
- **[TECH_DEBT_TODO.md](../TECH_DEBT_TODO.md)** - Technical debt and improvement tasks
- **[BALATRO_UI_STYLE_GUIDE.md](../BALATRO_UI_STYLE_GUIDE.md)** - UI styling and theme guidelines

## 🔧 Features & Specifications

### JAML Filter System
- **[JAML_EDITOR_ENHANCEMENTS.md](../JAML_EDITOR_ENHANCEMENTS.md)** - JAML editor feature specifications
- **[YAML_ANCHORS_IMPLEMENTATION_PLAN.md](../YAML_ANCHORS_IMPLEMENTATION_PLAN.md)** - YAML anchors & aliases support
- **[YAML_BEST_PRACTICES.md](../YAML_BEST_PRACTICES.md)** - YAML/JAML best practices guide
- **[ITEM_CONFIG_UX_PRD.md](../ITEM_CONFIG_UX_PRD.md)** - Item configuration UX design

### API & Integration
- **[MCP_STATUS_AND_ACTION_PLAN.md](../MCP_STATUS_AND_ACTION_PLAN.md)** - Model Context Protocol server status
- **[MOTELY_API_ROUTES.md](../MOTELY_API_ROUTES.md)** - Motely API endpoint documentation

## 🎯 External Projects

### Motely Search Engine
- **[external/Motely/README.md](../external/Motely/README.md)** - Motely CLI and search engine documentation

### JamlGenie
- **[external/JamlGenie/README.md](../external/JamlGenie/README.md)** - JamlGenie natural language filter generator

## 📝 Recent Changes

### January 2026 - AOT Compilation Refactoring
- ✅ Full AOT compilation support for Desktop and Browser platforms
- ✅ Removed all reflection-based code from JAML parsing
- ✅ Implemented platform abstraction for Excel export (`IExcelExporter`)
- ✅ Browser WASM build with full feature parity
- ✅ System.Text.Json and YamlDotNet source generation

See [TECH_DEBT_TODO.md](../TECH_DEBT_TODO.md) for completed AOT tasks.

## 🔍 Quick Reference

### For New Contributors
1. Start with [README.md](../README.md) for project overview
2. Read [ARCHITECTURE.md](ARCHITECTURE.md) for design patterns
3. Review [AI_CODING_GUIDELINES.md](../AI_CODING_GUIDELINES.md) for coding standards
4. Check [TECH_DEBT_TODO.md](../TECH_DEBT_TODO.md) for available tasks

### For AI Agents
- **Must Read**: [AI_CODING_GUIDELINES.md](../AI_CODING_GUIDELINES.md)
- **Task List**: [TECH_DEBT_TODO.md](../TECH_DEBT_TODO.md)
- **Architecture**: [ARCHITECTURE.md](ARCHITECTURE.md)

### For UI Development
- **Style Guide**: [BALATRO_UI_STYLE_GUIDE.md](../BALATRO_UI_STYLE_GUIDE.md)
- **Avalonia Patterns**: [AVALONIA_BEST_PRACTICES.md](AVALONIA_BEST_PRACTICES.md)
- **Platform Guide**: [PLATFORM_GUIDE.md](PLATFORM_GUIDE.md)

---

*Last Updated: January 26, 2026*
