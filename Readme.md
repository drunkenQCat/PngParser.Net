# PngParser.Net

PngParser.Net is a lightweight, pure C# library for reading and writing metadata in PNG images, translated from [hometlt/png-metadata](https://github.com/hometlt/png-metadata/tree/master). In current stage, as the original repo, it allows you to extract and modify tEXt and pHYs chunks within PNG files without relying on any external dependencies.



## Installation

Install the package via NuGet:

```bash
Install-Package PngParser.Net
```

Or via the .NET CLI:

```bash
dotnet add package PngParser.Net
```

## Usage

### Reading Metadata

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using PngParser;

class Program
{
    static void Main()
    {
        byte[] pngData = File.ReadAllBytes("input.png");

        // Extract all chunks
        List<Chunk> chunks = PngMetadata.ReadChunks(pngData);

        // Extract textual metadata
        var textData = new Dictionary<string, string>();
        foreach (var chunk in chunks)
        {
            if (PngUtilities.IsTextChunk(chunk.Name))
            {
                string keyword;
                string text;

                switch (chunk.Name)
                {
                    case "tEXt":
                        (keyword, text) = PngUtilities.TextDecode(chunk);
                        break;
                    case "iTXt":
                        var (iKeyword, _, _, _, iText) = PngUtilities.ITXtDecode(chunk);
                        keyword = iKeyword;
                        text = iText;
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

        // Extract physical dimension metadata
        var physChunk = chunks.Find(c => c.Name == "pHYs");
        PhysChunkData physData = null;
        if (physChunk != null)
        {
            physData = PngUtilities.PhysDecodeData(physChunk);
        }

        // Display tEXt metadata
        foreach (var kvp in textData)
            Console.WriteLine($"Keyword: {kvp.Key}, Text: {kvp.Value}");

        // Display pHYs metadata
        if (physData != null)
            Console.WriteLine($"pHYs Data - X: {physData.X}, Y: {physData.Y}, Unit: {physData.Unit}");
    }
}

```

### Writing Metadata

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using PngParser;

class Program
{
    static void Main()
    {
        byte[] pngData = File.ReadAllBytes("input.png");

        // Extract all chunks
        List<Chunk> chunks = PngMetadata.ReadChunks(pngData);

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

        // Insert or update textual chunks
        foreach (var textChunk in newTextChunks)
        {
            PngMetadata.AddOrUpdateTextChunk(chunks, textChunk);
        }

        // Create or update pHYs chunk
        var physChunk = new Chunk
        {
            Name = "pHYs",
            Data = PngUtilities.PhysEncodeData(2835, 2835, (byte)ResolutionUnits.Meters)
        };
        PngMetadata.InsertOrReplaceChunk(chunks, physChunk);

        // Write the modified chunks back to PNG data
        byte[] newPngData = PngMetadata.WriteChunks(chunks);

        // Save the modified PNG file
        File.WriteAllBytes("output.png", newPngData);
    }
}

```

## API Reference

- - ### Classes and Methods
  
    #### **PngMetadata**: Static class providing methods to read and write PNG metadata.
  
    - **`List<Chunk> ReadChunks(byte[] buffer)`**:
      Reads all chunks from a PNG byte array.
  
    - **`byte[] WriteChunks(List<Chunk> chunks)`**:
      Encodes a list of chunks back into a PNG byte array.
  
    - **`void InsertOrReplaceChunk(List<Chunk> chunks, Chunk newChunk)`**:
      Inserts or replaces a single-instance chunk (e.g., `pHYs`).
  
      - Parameters
  
        :
  
        - `chunks`: List of existing chunks.
        - `newChunk`: The chunk to insert or replace.
  
    - **`void AddOrUpdateTextChunk(List<Chunk> chunks, Chunk newChunk)`**:
      Adds a new textual chunk (`tEXt`, `iTXt`, `zTXt`) or updates an existing one based on the keyword.
  
      - Parameters
  
        :
  
        - `chunks`: List of existing chunks.
        - `newChunk`: The textual chunk to add or update.
  
    - **`void AddChunk(List<Chunk> chunks, Chunk newChunk)`**:
      Adds a multi-instance chunk (`iTXt`, `zTXt`) to the list without replacing existing ones.
  
      - Parameters
  
        :
  
        - `chunks`: List of existing chunks.
        - `newChunk`: The chunk to add.
  
    - **`void RemoveChunk(List<Chunk> chunks, string chunkName)`**:
      Removes all chunks with the specified name.
  
      - Parameters
  
        :
  
        - `chunks`: List of existing chunks.
        - `chunkName`: The name of the chunk to remove.
  
    #### **Chunk**: Represents a PNG chunk.
  
    - Properties
  
      :
  
      - `string Name`: The name of the chunk (e.g., "IHDR", "tEXt").
      - `byte[] Data`: The data contained in the chunk.
      - `bool IsCritical`: Determines whether the chunk is critical (true) or ancillary (false).
  
    #### **PhysChunkData**: Represents the data in a `pHYs` chunk.
  
    - Properties
  
      :
  
      - `uint X`: Pixels per unit in the X direction.
      - `uint Y`: Pixels per unit in the Y direction.
      - `byte Unit`: Unit specifier (`0` for unknown, `1` for meters).
  
    #### **ResolutionUnits**: Enum representing resolution units.
  
    - `Undefined = 0`
    - `Meters = 1`
    - `Inches = 2`
  
    #### **PngUtilities**: Static class providing helper methods for encoding and decoding chunks.
  
    - **Textual Chunk Methods**:
      - `byte[] TextEncodeData(string keyword, string text)`: Encodes a `tEXt` chunk.
      - `(string Keyword, string Text) TextDecode(Chunk chunk)`: Decodes a `tEXt` chunk.
      - `byte[] ITXtEncodeData(string keyword, string text, bool compressed = false, string languageTag = "", string translatedKeyword = "")`: Encodes an `iTXt` chunk.
      - `(string Keyword, bool Compressed, string LanguageTag, string TranslatedKeyword, string Text) ITXtDecode(Chunk chunk)`: Decodes an `iTXt` chunk.
      - `byte[] ZTxtEncodeData(string keyword, string text)`: Encodes a `zTXt` chunk.
      - `(string Keyword, string Text) ZTxtDecode(Chunk chunk)`: Decodes a `zTXt` chunk.
      - `bool IsTextChunk(string chunkName)`: Determines if a chunk is a textual chunk.
    - **Physical Chunk Methods**:
      - `byte[] PhysEncodeData(uint xPixelsPerUnit, uint yPixelsPerUnit, byte unitSpecifier)`: Encodes a `pHYs` chunk.
      - `PhysChunkData PhysDecodeData(Chunk chunk)`: Decodes a `pHYs` chunk.
    - **Utility Methods**:
      - `uint ReadUInt32BigEndian(byte[] data, int offset)`: Reads a 32-bit unsigned integer in big-endian format.
      - `void WriteUInt32BigEndian(byte[] buffer, int offset, uint value)`: Writes a 32-bit unsigned integer in big-endian format.

## Compatibility

PngParser.Net targets **.NET 8** and higher, may make some work for compatible and migrate it to .Net Standard 2.0 if someone need it.

## Building from Source

1. **Clone the Repository**:

   ```bash
   git clone https://github.com/drunkenQCat/PngParser.Net.git
   ```

2. **Build the Project**:

   Navigate to the project directory and build the library:

   ```bash
   cd PngParser.Net
   dotnet build
   ```

3. **Run Tests**:

   If you have included unit tests, you can run them using:

   ```bash
   dotnet test
   ```

## Contributing

Contributions are welcome! Please open an issue or submit a pull request for any improvements or bug fixes.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE.txt) file for details.

## Acknowledgements

- The PNG file format specification and related documentation:
  -  [PNG Specification: Chunk Specifications (w3.org)](https://www.w3.org/TR/PNG-Chunks.html)
  - [The Metadata in PNG files - Exiv2](https://dev.exiv2.org/projects/exiv2/wiki/The_Metadata_in_PNG_files)

- Contributors to the open-source community who have provided guidance and inspiration.

Certainly! Below is the updated **README.md** for your `PngParser` project. This version reflects the latest modifications, including support for multiple textual chunk types (`tEXt`, `iTXt`, `zTXt`) with unique keywords, enhanced metadata handling, and updated usage examples and unit tests.

------

# PngParser

PngParser is a lightweight, pure C# library for reading and writing metadata in PNG images. It allows you to extract and modify various types of metadata chunks within PNG files, including textual chunks (`tEXt`, `iTXt`, `zTXt`) and physical dimension chunks (`pHYs`), without relying on any external dependencies.

## Features

- **Extract Metadata**: Read `tEXt`, `iTXt`, `zTXt`, and `pHYs` chunks from PNG images.
- **Modify Metadata**: Add, update, or remove `tEXt`, `iTXt`, `zTXt`, and `pHYs` chunks.
- **Support for Multiple Textual Chunks**: Handle multiple instances of textual chunks with unique keywords.
- **Lightweight**: No external dependencies beyond necessary .NET packages; pure C# implementation.
- **Cross-Platform**: Compatible with any .NET Standard 2.0 compliant framework.

## Installation

Install the package via NuGet:

```
bash


复制代码
Install-Package PngParser
```

Or via the .NET CLI:

```
bash


复制代码
dotnet add package PngParser
```

Ensure that the following dependencies are also included in your project:

- **System.Memory**: Provides `BinaryPrimitives` and `Span<T>`.

  ```
  bash
  
  
  复制代码
  dotnet add package System.Memory --version 4.5.4
  ```

- **System.IO.Hashing**: Provides CRC-32 hashing functionality.

  ```
  bash
  
  
  复制代码
  dotnet add package System.IO.Hashing --version 1.0.0
  ```

## Usage

### Reading Metadata

```
csharp复制代码using System;
using System.Collections.Generic;
using System.IO;
using PngParser;

class Program
{
    static void Main()
    {
        byte[] pngData = File.ReadAllBytes("input.png");

        // Extract all chunks
        List<Chunk> chunks = PngMetadata.ReadChunks(pngData);

        // Extract textual metadata
        var textData = new Dictionary<string, string>();
        foreach (var chunk in chunks)
        {
            if (PngUtilities.IsTextChunk(chunk.Name))
            {
                string keyword;
                string text;

                switch (chunk.Name)
                {
                    case "tEXt":
                        (keyword, text) = PngUtilities.TextDecode(chunk);
                        break;
                    case "iTXt":
                        var (iKeyword, _, _, _, iText) = PngUtilities.ITXtDecode(chunk);
                        keyword = iKeyword;
                        text = iText;
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

        // Extract physical dimension metadata
        var physChunk = chunks.Find(c => c.Name == "pHYs");
        PhysChunkData physData = null;
        if (physChunk != null)
        {
            physData = PngUtilities.PhysDecodeData(physChunk);
        }

        // Display tEXt metadata
        foreach (var kvp in textData)
            Console.WriteLine($"Keyword: {kvp.Key}, Text: {kvp.Value}");

        // Display pHYs metadata
        if (physData != null)
            Console.WriteLine($"pHYs Data - X: {physData.X}, Y: {physData.Y}, Unit: {physData.Unit}");
    }
}
```

### Writing Metadata

```
csharp复制代码using System;
using System.Collections.Generic;
using System.IO;
using PngParser;

class Program
{
    static void Main()
    {
        byte[] pngData = File.ReadAllBytes("input.png");

        // Extract all chunks
        List<Chunk> chunks = PngMetadata.ReadChunks(pngData);

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

        // Insert or update textual chunks
        foreach (var textChunk in newTextChunks)
        {
            PngMetadata.AddOrUpdateTextChunk(chunks, textChunk);
        }

        // Create or update pHYs chunk
        var physChunk = new Chunk
        {
            Name = "pHYs",
            Data = PngUtilities.PhysEncodeData(2835, 2835, (byte)ResolutionUnits.Meters)
        };
        PngMetadata.InsertOrReplaceChunk(chunks, physChunk);

        // Write the modified chunks back to PNG data
        byte[] newPngData = PngMetadata.WriteChunks(chunks);

        // Save the modified PNG file
        File.WriteAllBytes("output.png", newPngData);
    }
}
```

## API Reference

### Classes and Methods

#### **PngMetadata**: Static class providing methods to read and write PNG metadata.

- **`List<Chunk> ReadChunks(byte[] buffer)`**:
  Reads all chunks from a PNG byte array.

- **`byte[] WriteChunks(List<Chunk> chunks)`**:
  Encodes a list of chunks back into a PNG byte array.

- **`void InsertOrReplaceChunk(List<Chunk> chunks, Chunk newChunk)`**:
  Inserts or replaces a single-instance chunk (e.g., `pHYs`).

  - Parameters

    :

    - `chunks`: List of existing chunks.
    - `newChunk`: The chunk to insert or replace.

- **`void AddOrUpdateTextChunk(List<Chunk> chunks, Chunk newChunk)`**:
  Adds a new textual chunk (`tEXt`, `iTXt`, `zTXt`) or updates an existing one based on the keyword.

  - Parameters

    :

    - `chunks`: List of existing chunks.
    - `newChunk`: The textual chunk to add or update.

- **`void AddChunk(List<Chunk> chunks, Chunk newChunk)`**:
  Adds a multi-instance chunk (`iTXt`, `zTXt`) to the list without replacing existing ones.

  - Parameters

    :

    - `chunks`: List of existing chunks.
    - `newChunk`: The chunk to add.

- **`void RemoveChunk(List<Chunk> chunks, string chunkName)`**:
  Removes all chunks with the specified name.

  - Parameters

    :

    - `chunks`: List of existing chunks.
    - `chunkName`: The name of the chunk to remove.

#### **Chunk**: Represents a PNG chunk.

- Properties

  :

  - `string Name`: The name of the chunk (e.g., "IHDR", "tEXt").
  - `byte[] Data`: The data contained in the chunk.
  - `bool IsCritical`: Determines whether the chunk is critical (true) or ancillary (false).

#### **PhysChunkData**: Represents the data in a `pHYs` chunk.

- Properties

  :

  - `uint X`: Pixels per unit in the X direction.
  - `uint Y`: Pixels per unit in the Y direction.
  - `byte Unit`: Unit specifier (`0` for unknown, `1` for meters).

#### **ResolutionUnits**: Enum representing resolution units.

- `Undefined = 0`
- `Meters = 1`
- `Inches = 2`

#### **PngUtilities**: Static class providing helper methods for encoding and decoding chunks.

- **Textual Chunk Methods**:
  - `byte[] TextEncodeData(string keyword, string text)`: Encodes a `tEXt` chunk.
  - `(string Keyword, string Text) TextDecode(Chunk chunk)`: Decodes a `tEXt` chunk.
  - `byte[] ITXtEncodeData(string keyword, string text, bool compressed = false, string languageTag = "", string translatedKeyword = "")`: Encodes an `iTXt` chunk.
  - `(string Keyword, bool Compressed, string LanguageTag, string TranslatedKeyword, string Text) ITXtDecode(Chunk chunk)`: Decodes an `iTXt` chunk.
  - `byte[] ZTxtEncodeData(string keyword, string text)`: Encodes a `zTXt` chunk.
  - `(string Keyword, string Text) ZTxtDecode(Chunk chunk)`: Decodes a `zTXt` chunk.
  - `bool IsTextChunk(string chunkName)`: Determines if a chunk is a textual chunk.
- **Physical Chunk Methods**:
  - `byte[] PhysEncodeData(uint xPixelsPerUnit, uint yPixelsPerUnit, byte unitSpecifier)`: Encodes a `pHYs` chunk.
  - `PhysChunkData PhysDecodeData(Chunk chunk)`: Decodes a `pHYs` chunk.
- **Utility Methods**:
  - `uint ReadUInt32BigEndian(byte[] data, int offset)`: Reads a 32-bit unsigned integer in big-endian format.
  - `void WriteUInt32BigEndian(byte[] buffer, int offset, uint value)`: Writes a 32-bit unsigned integer in big-endian format.

### Usage Scenarios

#### **Handling Multiple Textual Chunks with Unique Keywords**

```
csharp复制代码using System;
using System.Collections.Generic;
using System.IO;
using PngParser;

class Program
{
    static void Main()
    {
        byte[] pngData = File.ReadAllBytes("input.png");

        // Extract all chunks
        List<Chunk> chunks = PngMetadata.ReadChunks(pngData);

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

        // Insert or update textual chunks
        foreach (var textChunk in newTextChunks)
        {
            PngMetadata.AddOrUpdateTextChunk(chunks, textChunk);
        }

        // Create or update pHYs chunk
        var physChunk = new Chunk
        {
            Name = "pHYs",
            Data = PngUtilities.PhysEncodeData(2835, 2835, (byte)ResolutionUnits.Meters)
        };
        PngMetadata.InsertOrReplaceChunk(chunks, physChunk);

        // Write the modified chunks back to PNG data
        byte[] newPngData = PngMetadata.WriteChunks(chunks);

        // Save the modified PNG file
        File.WriteAllBytes("output.png", newPngData);
    }
}
```

## Compatibility

PngParser targets **.NET Standard 2.0**, making it compatible with:

- .NET Framework 4.6.1 and later
- .NET Core 2.0 and later
- Mono 5.4 and later
- Xamarin.iOS 10.14 and later
- Xamarin.Mac 3.8 and later
- Xamarin.Android 8.0 and later
- Universal Windows Platform 10.0.16299 and later
- Unity 2018.1 and later

## Building from Source

1. **Clone the Repository**:

   ```
   bash
   
   
   复制代码
   git clone https://github.com/yourusername/PngParser.git
   ```

2. **Navigate to the Project Directory**:

   ```
   bash
   
   
   复制代码
   cd PngParser
   ```

3. **Restore Dependencies**:

   ```
   bash
   
   
   复制代码
   dotnet restore
   ```

4. **Build the Project**:

   ```
   bash
   
   
   复制代码
   dotnet build
   ```

5. **Run Unit Tests**:

   If you have included unit tests, you can run them using:

   ```
   bash
   
   
   复制代码
   dotnet test
   ```

   Ensure that all tests pass successfully.

## Unit Testing

PngParser includes a suite of unit tests to verify its functionality. The tests cover:

- Reading and writing of textual chunks (`tEXt`, `iTXt`, `zTXt`) with unique keywords.
- Inserting and replacing single-instance chunks like `pHYs`.
- Handling invalid PNG headers gracefully.

### Running the Tests

1. **Navigate to the Test Project Directory**:

   ```
   bash
   
   
   复制代码
   cd PngParser.Tests
   ```

2. **Restore Test Dependencies**:

   ```
   bash
   
   
   复制代码
   dotnet restore
   ```

3. **Run the Tests**:

   ```
   bash
   
   
   复制代码
   dotnet test
   ```

   All tests should pass, indicating that the library handles metadata correctly.

## Contributing

Contributions are welcome! Please follow these steps:

1. **Fork the Repository**: Click the "Fork" button on the repository page.

2. **Create a Feature Branch**:

   ```
   bash
   
   
   复制代码
   git checkout -b feature/YourFeatureName
   ```

3. **Commit Your Changes**:

   ```
   bash
   
   
   复制代码
   git commit -m "Add your descriptive commit message here"
   ```

4. **Push to Your Fork**:

   ```
   bash
   
   
   复制代码
   git push origin feature/YourFeatureName
   ```

5. **Create a Pull Request**: Navigate to your fork on GitHub and click the "New pull request" button.

Please ensure that your contributions adhere to the project's coding standards and include appropriate unit tests.

## License

This project is licensed under the MIT License. See the LICENSE file for details.

## Acknowledgements

- The [PNG (Portable Network Graphics) Specification](https://www.w3.org/TR/PNG/) for defining the PNG file format.
- Contributors to the open-source community who have provided guidance and inspiration.

------

## Additional Information

### **BinaryPrimitives and Span<T> in .NET Standard 2.0**

`BinaryPrimitives` and `Span<T>` are used extensively in PngParser for efficient byte manipulation, especially for handling big-endian data formats required by the PNG specification. These are available in .NET Standard 2.0 via the `System.Memory` package.

### **Handling Textual Chunks**

- **`tEXt`**: For uncompressed Latin-1 text data.
- **`iTXt`**: For UTF-8 encoded, optionally compressed international text data with language tags.
- **`zTXt`**: For compressed Latin-1 text data.

Each textual chunk type allows multiple instances but should maintain unique keywords to prevent duplication and ambiguity.

### **Physical Dimension Chunks (`pHYs`)**

The `pHYs` chunk stores the intended pixel size or aspect ratio of the image, including:

- **X**: Pixels per unit in the X direction.
- **Y**: Pixels per unit in the Y direction.
- **Unit**: Unit specifier (`0` for unknown, `1` for meters, `2` for inches).

This information is useful for applications that need to display the image with accurate physical dimensions.

### **Error Handling**

PngParser includes robust error handling to manage:

- Invalid PNG headers.
- Unexpected end of data while reading chunks.
- CRC mismatches indicating potential file corruption.
- Unsupported compression methods in textual chunks.

Ensure that you handle these exceptions appropriately in your application to maintain stability.

### **Extensibility**

PngParser is designed to be extensible. You can add support for additional chunk types by:

1. **Defining Encoding and Decoding Methods**: Implement methods similar to those in `PngUtilities` for new chunk types.
2. **Updating `IsMultiInstanceChunk` and `IsTextChunk` Methods**: Include new chunk types as needed.
3. **Modifying Insertion Logic**: Ensure that new chunk types are handled correctly in insertion and replacement methods.
