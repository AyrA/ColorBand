# ColorBand
Creates Color Bands from movies

It is now the second time I see Color Bands on reddit without a source code provided,
as if it was made by some evil magic. This time the person creating those is also selling these images.

Well now you can create them on your own. And it's fast too.
For most HD source files it encodes with at least 20x playback speed.
For source files with less FPS and/or less resolution it can go up to 200 or more.

## Colorband vs Frameband

A Colorband is a band where the average color of a frame is taken for each column.
A Frameband is a band where each frame is reduced to one column instead of one pixel,
this way each column in the band has multiple colors.

# Features

- Creates color bands or frame bands from video input.
- Works as console and as UI application, depending if command line arguments are given.
- Accepts Drag&Drop for Video files or text strings containing the full file path.
- Shows the progress and makes rather exact estimate about the runtime.
- Self contained. No dependencies needed.
- Autodetects ideal height of the band.
- If the conversion process is aborted, the band can still be created from the already extracted frames.
- Thanks to ffmpeg, works for about every possible video input.
- Simple UI
- Made by me

# How to use
This application can be used in Windowed and Console mode.

## Windowed mode
Just double click the .exe file to get the graphical user interface.

## Console mode
You can use it in command line mode (untested as fo now).
Type `ColorBand /?` for help.

# Single color mode
Single color mode reduces a frame to a single pixel instead of a vertical line.
This causes the frame band to be only one color per frame.
The height value still works. This is what most people generate, but it looks less awesome.
Multi color mode examples can be found on [imgur](http://imgur.com/a/M9oIx).

# TODO
- [ ] Show the band as progress in the UI mode instead of a progress bar.

# License
This application is licensed under the [GNU agpl 3.0](https://www.gnu.org/licenses/agpl-3.0.txt).
FFmpeg is licensed under [this mess](https://www.ffmpeg.org/legal.html).

# FFMPEG
This work contains the compiled ffmpeg binaries inside blob.bin.
