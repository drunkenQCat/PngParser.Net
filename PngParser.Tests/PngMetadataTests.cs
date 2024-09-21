namespace PngParser.Tests;

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
        // Generate an all-white PNG image
        string whiteImagePath = Path.Combine(TestImagesFolder, "empty_meta.png");
        if (!File.Exists(whiteImagePath))
        {
            //ImageGenerator.GenerateAllWhitePng(whiteImagePath, 100, 100);
        }

        // Generate an all-black PNG image with metadata
        string blackImagePath = Path.Combine(TestImagesFolder, "with_meta.png");
        if (!File.Exists(blackImagePath))
        {
            var metadata = new Dictionary<string, string>
            {
                { "Author", "John Doe" },
                { "Description", "Test Author" }
            };
            //ImageGenerator.GenerateAllBlackPngWithMetadata(blackImagePath, 100, 100, metadata);
        }
    }

    [Fact]
    public void ReadMetadata_ShouldExtractTextChunks()
    {
        // Arrange
        var pngData = File.ReadAllBytes(Path.Combine(TestImagesFolder, "with_meta.png"));

        // Act
        var (textData, _) = PngMetadata.ReadMetadata(pngData);

        // Assert
        Assert.NotEmpty(textData);
        Assert.True(textData.ContainsKey("Author"));
        Assert.Equal("John Doe", textData["Author"]);
    }

    [Fact]
    public void WriteMetadata_ShouldAddTextChunks()
    {
        // Arrange
        var pngData = File.ReadAllBytes(Path.Combine(TestImagesFolder, "empty_meta.png"));
        var newTextData = new Dictionary<string, string>
        {
            { "Title", "Unit Test Image" },
            { "Description", "This image is used for unit testing." }
        };

        // Act
        var newPngData = PngMetadata.WriteMetadata(pngData, newTextData);
        var (textData, _) = PngMetadata.ReadMetadata(newPngData);

        // Assert
        Assert.True(textData.ContainsKey("Title"));
        Assert.Equal("Unit Test Image", textData["Title"]);
        Assert.True(textData.ContainsKey("Description"));
        Assert.Equal("This image is used for unit testing.", textData["Description"]);
    }

    [Fact]
    public void InsertMetadata_ShouldUpdatePhysChunk()
    {
        // Arrange
        var pngData = File.ReadAllBytes(Path.Combine(TestImagesFolder, "empty_meta.png"));
        var physData = new PhysChunkData
        {
            X = 2835,
            Y = 2835,
            Unit = (byte)ResolutionUnits.Meters
        };

        // Act
        var newPngData = PngMetadata.WriteMetadata(pngData, physData: physData);
        var (_, extractedPhysData) = PngMetadata.ReadMetadata(newPngData);

        // Assert
        Assert.NotNull(extractedPhysData);
        Assert.Equal(2835u, extractedPhysData!.X);
        Assert.Equal(2835u, extractedPhysData.Y);
        Assert.Equal((byte)ResolutionUnits.Meters, extractedPhysData.Unit);
    }

    [Fact]
    public void ExtractChunks_InvalidHeader_ShouldThrowException()
    {
        // Arrange
        var invalidData = new byte[] { 0x00, 0x01, 0x02 };

        // Act & Assert
        Assert.Throws<Exception>(() => PngMetadata.ExtractChunks(invalidData));
    }
}
