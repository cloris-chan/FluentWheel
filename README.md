# Fluent Wheel

Smooth scrolling and zooming extension for Visual Studio 2022 (`17.14+`) and Visual Studio 2026.

## Features

- Smooth vertical scrolling
- Smooth horizontal scrolling with `Shift + Wheel`
- Smooth zooming with `Ctrl + Wheel`
- Fixed-step zooming with `Ctrl + Alt + Wheel`

## Settings

You can find the extension settings in `Tools > Options > All Settings > FluentWheel`.

Available settings:

- Scroll duration
- Scroll easing mode
- Vertical scroll rate
- Horizontal scroll rate
- Zoom duration
- Zoom easing mode
- Enable low-level mouse hook

### Scroll rate

- `100` follows your system scroll setting
- Negative values reverse the scroll direction

### Low-level mouse hook

- If scrolling or zooming feels laggy, try turning it on
- It can help in editors with more complex layouts or when `Sticky Scroll` is enabled
- When the editor enters `InLayout`, standard input messages may be delayed or blocked

### Easing modes

- `Linear`
- `EaseIn`
- `EaseOut`
- `EaseInOut`