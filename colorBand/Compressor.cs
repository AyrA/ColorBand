using System.IO;
using System;
using System.IO.Compression;
using System.Text;

namespace AyrA.IO
{
    /// <summary>
    /// Compresses or decompresses files into a blob
    /// </summary>
    public static class Compressor
    {
        /// <summary>
        /// Compress a list of files into a blob
        /// </summary>
        /// <remarks>During compression, the path information is lost</remarks>
        /// <param name="Files">Full file names.</param>
        /// <param name="Destination">Destination stream</param>
        public static void Compress(string[] Files, Stream Destination)
        {
            FileInfo[] FF = Array.ConvertAll(Files, delegate(string f) { return new FileInfo(f); });

            using (GZipStream zip = new GZipStream(Destination, CompressionMode.Compress, true))
            {
                using (BinaryWriter BW = new BinaryWriter(zip))
                {
                    BW.Write(Files.Length);
                    foreach (FileInfo F in FF)
                    {
#if DEBUG
                        Console.WriteLine("Compressing {0}", F.Name);
#endif
                        byte[] Data = File.ReadAllBytes(F.FullName);
                        BW.Write(Encoding.UTF8.GetByteCount(F.Name));
                        BW.Write(Encoding.UTF8.GetBytes(F.Name));
                        BW.Write(Data.Length);
                        BW.Write(Data);
                    }
                }
            }
        }

        /// <summary>
        /// Decompresses files from a stream into a directory
        /// </summary>
        /// <param name="Directory">Destination directory</param>
        /// <param name="Source">Compressed source stream</param>
        public static void Decompress(string Directory, Stream Source)
        {
            using (GZipStream GZ = new GZipStream(Source, CompressionMode.Decompress))
            {
                using (BinaryReader BR = new BinaryReader(GZ))
                {
                    int Count = BR.ReadInt32();
                    for (int i = 0; i < Count; i++)
                    {
                        string FileName = Encoding.UTF8.GetString(BR.ReadBytes(BR.ReadInt32()));
#if DEBUG
                        Console.WriteLine("Decompressing {0}", FileName);
#endif
                        byte[] Content = BR.ReadBytes(BR.ReadInt32());
                        File.WriteAllBytes(Path.Combine(Directory, FileName), Content);
                    }
                }
            }
        }
    }
}
