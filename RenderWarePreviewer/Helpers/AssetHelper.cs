﻿using BCnEncoder.Decoder;
using BCnEncoder.ImageSharp;
using RenderWareIo;
using RenderWareIo.Structs.Dff;
using RenderWareIo.Structs.Ide;
using RenderWareIo.Structs.Img;
using RenderWareIo.Structs.Txd;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RenderWarePreviewer.Helpers
{
    public class AssetHelper
    {
        private string? gtaPath;
        private ImgFile? img;

        public AssetHelper()
        {

        }

        public void SetGtaPath(string path)
        {
            var imgPath = Path.Join(path, "models", "gta3.img");
            if (!File.Exists(imgPath))
                throw new FileNotFoundException($"No img file found at {path}");

            if (this.img != null)
                this.img.Dispose();

            this.gtaPath = path;
            this.img = new ImgFile(imgPath);
        }

        public Dff GetDff(string name)
        {
            if (this.img == null)
                throw new FileNotFoundException("No img file found");

            var key = SanitizeName(name) + ".dff";
            if (!this.img.Img.DataEntries.ContainsKey(key))
                throw new FileNotFoundException($"No {name} file found in txd");

            var data = this.img.Img.DataEntries[key];
            var stream = GetReadStream(data);

            var dff = new Dff();
            dff.Read(stream);

            return dff;
        }

        public Txd GetTxd(string name)
        {
            if (this.img == null)
                throw new FileNotFoundException("No img file found");

            var key = SanitizeName(name) + ".txd";
            if (!this.img.Img.DataEntries.ContainsKey(key))
                throw new FileNotFoundException($"No {name} file found in txd");

            var data = this.img.Img.DataEntries[key];
            var stream = GetReadStream(data);

            var txd = new Txd();
            txd.Read(stream);

            return txd;
        }

        public Dictionary<string, Image<Rgba32>> GetImages(Txd txd)
        {
            var images = new Dictionary<string, Image<Rgba32>>();

            foreach (var texture in txd.TextureContainer.Textures)
            {
                string sanitizedName = SanitizeName(texture.Data.TextureName);
                string format = texture.Data.TextureFormatString;

                Image<Rgba32> rgbaImage = new(texture.Data.Width, texture.Data.Height);

                switch (format)
                {
                    case "RGB32":
                        {
                            var image = Image.LoadPixelData<Bgra32>(texture.Data.Data, texture.Data.Width, texture.Data.Height);
                            rgbaImage = image.CloneAs<Rgba32>();
                            image.Dispose();
                            break;
                        }
                    case "RGBA32":
                        {
                            var image = Image.LoadPixelData<Rgba32>(texture.Data.Data, texture.Data.Width, texture.Data.Height);
                            rgbaImage = image;
                            break;
                        }
                    case "DXT1":
                        {
                            BcDecoder decoder = new();
                            Image<Rgba32> image = decoder.DecodeRawToImageRgba32(texture.Data.Data, texture.Data.Width, texture.Data.Height, BCnEncoder.Shared.CompressionFormat.Bc1);
                            rgbaImage = image;
                            break;
                        }
                }

                images[sanitizedName] = rgbaImage;
            }

            return images;
        }

        public IEnumerable<Ped> GetSkins()
        {
            var ide = new IdeFile(Path.Join(this.gtaPath, "data", "peds.ide"));
            return ide.Ide.Peds.Where(x => x.Id > 0);
        }

        public string SanitizeName(string name)
        {
            return name.Replace("_", "").Trim('\0').ToLower();
        }

        private Stream GetReadStream(DataEntry data)
        {
            var stream = new MemoryStream();
            stream.Write(data.Data);
            stream.Position = 0;
            return stream;
        }
    }
}