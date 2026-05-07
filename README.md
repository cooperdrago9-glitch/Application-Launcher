# Spotlight (WPF Launcher)

A lightweight, macOS Spotlight / Raycast-style launcher built for Windows using WPF.

It provides fast app launching, system search, and a minimal glass UI.

---

## ✨ Features

- ⚡ Global hotkey (Ctrl + Space)
- 🔍 Instant app search
- 📌 Pinned apps (Terminal, VS Code, Explorer, Firefox, VirtualBox)
- 🌐 Web search fallback
- ⌨️ Keyboard navigation (↑ ↓ Enter Esc)
- 🪟 Clean glass UI (Spotlight-inspired)
- 📂 Start Menu app scanning

---

## 📸 Screenshots

(Add screenshots here later)

---

## ⬇️ Download (No build required)

Go to **Releases** and download the latest version:

👉 https://github.com/YOUR_USERNAME/Spotlight/releases

Download the `.zip`, extract it, and run:

---

## 🧱 Build from source

### Requirements
- .NET 8 SDK or later
- Windows 10/11 (ARM64 or x64)

---

## 🧪 Build commands

### 🟣 ARM64

```bash
dotnet publish -c Release -r win-arm64 --self-contained true /p:PublishSingleFile=true

```
### x64
```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
