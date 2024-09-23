using System.Collections.Generic;
using System.IO;

namespace PngParser.Tests
{
    public class PngMetadataTests
    {
        private const string TestImagesFolder = "TestImages";
        private const string EmptyPngPath = "TestImages/empty_meta.png";
        private const string TextualPngPath = "TestImages/with_meta.png";
        private const string NewPngPath = "TestImages/new_meta.png";

        public PngMetadataTests()
        {
            // Ensure the TestImages folder exists
            if (!Directory.Exists(TestImagesFolder))
            {
                Directory.CreateDirectory(TestImagesFolder);
            }

            // Generate or copy a test PNG file (ensure a valid original.png exists in TestImages)
            if (!File.Exists(EmptyPngPath))
            {
                // Generate a simple PNG (could use an external tool or provide a minimal PNG file)
                // You can also copy an existing valid PNG here.
            }
        }

        [Fact]
        public void InitializePngMetadata_ShouldReadChunks()
        {
            // Arrange
            var pngMetadata = new PngMetadata(TextualPngPath);

            // Act
            var chunks = pngMetadata.ReadTextChunks();

            // Assert
            Assert.NotEmpty(chunks);  // Should contain at least the IHDR, IDAT, and IEND chunks
        }

        [Fact]
        public void UpdateTextChunks_ShouldAddNewMetadata()
        {
            // Arrange
            var pngMetadata = new PngMetadata(EmptyPngPath);
            var newMetadata = new Dictionary<string, string>
            {
                { "Author", "John Doe" },
                { "Description", "Test PNG image with metadata" }
            };

            // Act
            pngMetadata.UpdateTextChunks(newMetadata);
            pngMetadata.Save(NewPngPath);

            // Assert
            var updatedMetadata = new PngMetadata(NewPngPath);
            var textChunks = updatedMetadata.ReadTextChunks();
            Assert.True(textChunks.ContainsKey("Author"));
            Assert.Equal("John Doe", textChunks["Author"]);
            Assert.True(textChunks.ContainsKey("Description"));
            Assert.Equal("Test PNG image with metadata", textChunks["Description"]);
        }

        [Fact]
        public void ReadTextChunks_ShouldReturnCorrectMetadata()
        {
            // Arrange
            var pngMetadata = new PngMetadata(TextualPngPath);

            // Act
            var textChunks = pngMetadata.ReadTextChunks();

            // Assert
            Assert.True(textChunks.ContainsKey("Author"));
            Assert.Equal("John Doe", textChunks["Author"]);
        }

        [Fact]
        public void RemoveChunk_ShouldRemoveSpecifiedChunk()
        {
            // Arrange
            var pngMetadata = new PngMetadata(TextualPngPath);

            // Act
            pngMetadata.RemoveChunk("tEXt");  // Remove all tEXt chunks
            pngMetadata.Save(NewPngPath);

            // Assert
            var updatedMetadata = new PngMetadata(NewPngPath);
            var textChunks = updatedMetadata.ReadTextChunks();
            Assert.Empty(textChunks);  // All tEXt chunks should be removed
        }

        [Fact]
        public void InsertOrReplaceChunk_ShouldReplaceChunkSuccessfully()
        {
            // Arrange
            var pngMetadata = new PngMetadata(EmptyPngPath);
            var physChunk = new Chunk
            {
                Name = "pHYs",
                Data = PngUtilities.PhysEncodeData(2835, 2835, (byte)ResolutionUnits.Meters)
            };

            // Act
            pngMetadata.InsertOrReplaceChunk(physChunk);
            pngMetadata.Save(NewPngPath);

            // Assert
            var updatedMetadata = new PngMetadata(NewPngPath);
            var chunk = updatedMetadata.ReadPhysChunk();
            // Verify that the chunk was inserted correctly (in this case, using pHYs example)
            Assert.Equal((uint)2835, chunk.X);
            Assert.Equal((uint)2835, chunk.Y);
            Assert.True(chunk.Unit == (byte)ResolutionUnits.Meters);
        }
    }
}
