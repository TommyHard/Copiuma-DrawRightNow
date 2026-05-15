# DrawRightNow

> A high-performance desktop application designed for real-time on-screen drawing and annotation.

**DrawRightNow** operates as a transparent overlay, enabling users to create graphical annotations over any active applications, video playback, or the desktop environment without disrupting their underlying functionality (referred to as "live background" mode).

---

## Core Features

### Drawing and Vector Graphics
* **Primary Tools:** Includes a fixed-width pencil (no aliasing), a soft-edged brush (with optional pressure sensitivity), and a semi-transparent marker utilizing a Multiply/Alpha Blend overlay mode.
* **Shapes and Area Fill:** Supports standard primitives (lines, arrows, circles, rectangles) and closed-contour area filling algorithms.
* **Text Rendering:** Provides on-screen text input with post-placement editing capabilities.
* **Advanced Blurring:** Features local vector element blurring and real-time screen blurring using a Gaussian Blur filter applied to the captured live background.
* **Eyedropper Tool:** Captures color hex/RGB values from any point on the screen, including the underlying background.

### Workspace Control and Editing
* **Vector Manipulation:** Allows selection, repositioning, and transformation of objects via bounding box hit-testing. Includes a vector eraser and a "knife" tool for immediate object deletion.
* **State Management:** Fully supports an undo/redo action history stack.
* **Export Capabilities:** Enables copying selections to the clipboard or saving the canvas (with or without background capture) in PNG/JPG formats.
* **Grid Snapping:** Features a customizable grid with variable graduation steps and snapping functionality.

### User Interface (UI)
* **Dynamic Toolbars:** Utilizes horizontal floating panels that appear upon mouse hover and can be permanently locked using a "Pin" toggle.
* **Global Hotkeys:** Configurable global keyboard hooks for tool switching, UI toggling, and enabling click-through transparency.
* **Localization:** Base support for English and Russian (EN/RU).

---

## Technology Stack

* **Platform:** C# (.NET 8 or .NET 9).
* **UI Framework:** WPF.
* **Rendering Engine:** Hardware-accelerated vector rendering utilizing SkiaSharp combined with OpenGL/Vulkan (or Direct2D via Win2D).
* **OS Interoperability:** Win32 API integration (`user32.dll`, `dwmapi.dll`) and Windows Graphics Capture API (WGC).

---

## Architecture and Performance Optimization

The application is engineered to operate under strict system resource constraints:
* **Design Patterns:** Adheres to the MVVM (Model-View-ViewModel) pattern for absolute separation of UI, shape storage, and the rendering engine. The Undo/Redo system utilizes the Command pattern.
* **Zero Allocation Rendering:** Creation of new objects is strictly minimized within the main render loop and during `MouseMove` events to prevent Garbage Collector spikes. Leverages `struct`, `Span<T>`, and Object Pools.
* **Dirty Rectangles:** Implements optimized repainting by selectively rendering only the modified areas of the canvas.
* **Window Management:** Uses `WS_EX_LAYERED` and `WS_EX_TOPMOST` flags for overlay persistence. The `WS_EX_TRANSPARENT` flag is dynamically toggled to allow user interactions to pass through to underlying windows when the drawing mode is inactive.