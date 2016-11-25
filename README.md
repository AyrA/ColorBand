# ColorBand
Creates Color Bands from movies

It is now the second time I see Color Bands on reddit without a source code provided,
as if it was made by some evil magic.

# How to use
Just double click the .exe file and fill in the requested values.

**Hint:** When asked for file names, you can drag the file itself on the console to fill in the full path.

## Questions asked
List of questions you are asked to answer

### Source video file
The path and name of the video file to use

### Destination image file
The path and name of the destination image (the color band)

### Image height
The height of the image. The width is determined by the number of frames in the video.

### Use single color
If you answer `y`, then each frame is reduced to one color.
If you answer `n`, then each frame is reduced to a width of 1.

# TODO
- Make the programm accept command line arguments
- GUI
- Error checking

# FFMPEG

This work contains the compiled ffmpeg binaries inside blob.bin.
