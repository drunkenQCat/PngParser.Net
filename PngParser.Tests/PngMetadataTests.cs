using System;
using System.Collections.Generic;
using System.IO;

namespace PngParser.Tests
{
    public class PngMetadataTests
    {
        private const string TestImagesFolder = "TestImages";

        public PngMetadataTests()
        {
            // Ensure the TestImages folder exists
            if (!Directory.Exists(TestImagesFolder))
            {
                Directory.CreateDirectory(TestImagesFolder);
            }

            // Generate test images
            GenerateTestImages();
        }

        private void GenerateTestImages()
        {
            // Generate an empty PNG image without metadata
            string emptyMetaImagePath = Path.Combine(TestImagesFolder, "empty_meta.png");
            if (!File.Exists(emptyMetaImagePath))
            {
                //ImageGenerator.GenerateEmptyPng(emptyMetaImagePath, 100, 100);
            }

            // Generate a PNG image with tEXt metadata
            string withMetaImagePath = Path.Combine(TestImagesFolder, "with_meta.png");
            if (!File.Exists(withMetaImagePath))
            {
                var chunks = new List<Chunk>();

                // Create IHDR chunk (we can copy from the empty image)
                var emptyImageData = File.ReadAllBytes(emptyMetaImagePath);
                var emptyChunks = PngMetadata.ReadChunks(emptyImageData);
                var ihdrChunk = emptyChunks.Find(c => c.Name == "IHDR");
                chunks.Add(ihdrChunk);

                // Add IDAT chunk (we can copy from the empty image)
                var idatChunk = emptyChunks.Find(c => c.Name == "IDAT");
                chunks.Add(idatChunk);

                // Add tEXt chunks
                var textChunk1 = new Chunk
                {
                    Name = "tEXt",
                    Data = PngUtilities.TextEncodeData("Author", "John Doe")
                };
                chunks.Add(textChunk1);

                var textChunk2 = new Chunk
                {
                    Name = "tEXt",
                    Data = PngUtilities.TextEncodeData("Description", "Test Author")
                };
                chunks.Add(textChunk2);

                // Add IEND chunk
                var iendChunk = new Chunk { Name = "IEND", Data = Array.Empty<byte>() };
                chunks.Add(iendChunk);

                // Write the chunks to create the image
                var pngData = PngMetadata.WriteChunks(chunks);
                File.WriteAllBytes(withMetaImagePath, pngData);
            }
        }

        [Fact]
        public void ReadMetadata_ShouldExtractTextChunks()
        {
            // Arrange
            var pngData = File.ReadAllBytes(Path.Combine(TestImagesFolder, "with_meta.png"));

            // Act
            var chunks = PngMetadata.ReadChunks(pngData);

            // Extract tEXt chunks
            var textChunks = chunks.FindAll(c => c.Name == "tEXt");
            var textData = new Dictionary<string, string>();
            foreach (var chunk in textChunks)
            {
                var (keyword, text) = PngUtilities.TextDecode(chunk);
                textData[keyword] = text;
            }

            // Assert
            Assert.NotEmpty(textData);
            Assert.True(textData.ContainsKey("Author"));
            Assert.Equal("John Doe", textData["Author"]);
        }

        
        [Fact]
        public void WriteMetadata_ShouldAddOrUpdateTextualChunks()
        {
            // Arrange
            var pngData = File.ReadAllBytes(Path.Combine(TestImagesFolder, "with_meta.png"));
            var chunks = PngMetadata.ReadChunks(pngData);

            // Create new textual chunks
            var newTextChunks = new List<Chunk>
            {
                // Update existing tEXt chunk
                new Chunk
                {
                    Name = "tEXt",
                    Data = PngUtilities.TextEncodeData("Author", "Jane Smith")
                },
                // Add new iTXt chunk
                new Chunk
                {
                    Name = "iTXt",
                    Data = PngUtilities.ITXtEncodeData("Title", "Unit Test Image")
                },
                // Add new zTXt chunk
                new Chunk
                {
                    Name = "zTXt",
                    Data = PngUtilities.ZTxtEncodeData("Description", "This image is used for unit testing.")
                }
            };

            // Act
            foreach (var textChunk in newTextChunks)
            {
                PngMetadata.AddOrUpdateTextChunk(chunks, textChunk);
            }

            var newPngData = PngMetadata.WriteChunks(chunks);

            // Read back the textual data
            var newChunks = PngMetadata.ReadChunks(newPngData);
            var textData = new Dictionary<string, string>();

            foreach (var chunk in newChunks)
            {
                if (IsTextChunk(chunk.Name))
                {
                    string keyword;
                    string text;

                    switch (chunk.Name)
                    {
                        case "tEXt":
                            (keyword, text) = PngUtilities.TextDecode(chunk);
                            break;
                        case "iTXt":
                            (keyword, _, _, _, text) = PngUtilities.ITXtDecode(chunk);
                            break;
                        case "zTXt":
                            (keyword, text) = PngUtilities.ZTxtDecode(chunk);
                            break;
                        default:
                            continue;
                    }

                    textData[keyword] = text;
                }
            }

            // Assert
            Assert.True(textData.ContainsKey("Author"));
            Assert.Equal("Jane Smith", textData["Author"]); // Updated value
            Assert.True(textData.ContainsKey("Title"));
            Assert.Equal("Unit Test Image", textData["Title"]); // New value from iTXt chunk
            Assert.True(textData.ContainsKey("Description"));
            Assert.Equal("This image is used for unit testing.", textData["Description"]); // New value from zTXt chunk
        }

        private static bool IsTextChunk(string chunkName)
        {
            return chunkName is "tEXt" or "iTXt" or "zTXt";
        }

        [Fact]
        public void InsertMetadata_ShouldUpdatePhysChunk()
        {
            // Arrange
            var pngData = File.ReadAllBytes(Path.Combine(TestImagesFolder, "empty_meta.png"));
            var chunks = PngMetadata.ReadChunks(pngData);

            var physChunk = new Chunk
            {
                Name = "pHYs",
                Data = PngUtilities.PhysEncodeData(2835, 2835, (byte)ResolutionUnits.Meters)
            };

            // Act
            PngMetadata.InsertOrReplaceChunk(chunks, physChunk);

            var newPngData = PngMetadata.WriteChunks(chunks);

            // Read back the phys data
            var newChunks = PngMetadata.ReadChunks(newPngData);
            var newPhysChunk = newChunks.Find(c => c.Name == "pHYs");
            var physData = PngUtilities.PhysDecodeData(newPhysChunk);

            // Assert
            Assert.NotNull(newPhysChunk);
            Assert.Equal(2835u, physData.X);
            Assert.Equal(2835u, physData.Y);
            Assert.Equal((byte)ResolutionUnits.Meters, physData.Unit);
        }

        [Fact]
        public void ExtractChunks_InvalidHeader_ShouldThrowException()
        {
            // Arrange
            var invalidData = new byte[] { 0x00, 0x01, 0x02 };

            // Act & Assert
            Assert.Throws<Exception>(() => PngMetadata.ReadChunks(invalidData));
        }
    }
}

