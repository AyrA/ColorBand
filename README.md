# ColorBand
Creates Color Bands from movies

It is now the second time I see Color Bands on reddit without a source code provided,
as if it was made by some evil magic. This time the person creating those is also selling these images.

Well now you can create them on your own. And it's fast too.
For most source files it encodes with at least 20x playback speed.

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
- [X] Adding graphical interface
- [X] Autodetect height from video
- [X] Use command line arguments instead of prompts
- [X] Some error checking
- [ ] Make image joiner async for UI

# License
This application is licensed under the [GNU agpl 3.0](https://www.gnu.org/licenses/agpl-3.0.txt).
FFmpeg is licensed under [this mess](https://www.ffmpeg.org/legal.html).

# FFMPEG
This work contains the compiled ffmpeg binaries inside blob.bin.
