# Audio Tag Tools

This small command line application performs four tasks:
1. Cache metadata tags from audio files into a "library file"
2. Report likely duplicate files based on their tags and personalized settings
3. Export a list of artists with their most common genres
4. Analyze library files to display various statistics

I originally created this tool to practice with [F#](https://fsharp.org/) [JSON type providers](https://fsprojects.github.io/FSharp.Data/library/JsonProvider.html), but I've kept working on it as it resolves a couple of small pain points for me as well.


# Requirements

- [.NET 10 runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- JSON settings file (only for the duplicate search) — plus comfort manually editing such JSON files


# Running

Ensure you are in the `AudioTagTools.Console` directory in your terminal.


## 1. Caching tags

Creates a "tag library," a JSON file containing the text tag data from the audio files in a specified directory.

> [!NOTE]
> If you have many files, especially on an external device, this process might take a while on its initial run, but will be much faster in subsequent ones.

Pass `cache-tags` with two arguments:

1. The path of the directory containing your audio files
2. The path of the library file that contains (or will contain, if it does not exist yet) your cached audio tags

Sample:

```sh
dotnet run -- cache-tags ~/Documents/Audio ~/Documents/Audio/tagLibrary.json
```

> [!TIP]
> The `--` is necessary to indicate that the command and arguments are for this program and not for `dotnet`.

If a library file already exists at the specified path, a backup copy will automatically be created in the same directory.

The file will be in this JSON format:

```json
[
  {
    "FileName": "FILENAME.m4a",
    "DirectoryName": "FULL_DIRECTORY_NAME",
    "Artists": [
      "SAMPLE ARTIST NAME"
    ],
    "AlbumArtists": [
      "SAMPLE ALBUM ARTIST NAME"
    ],
    "Album": "ALBUM_NAME",
    "TrackNo": 0,
    "Title": "TRACK_TITLE",
    "Year": 1950,
    "Genres": [
      "GENRE"
    ],
    "Duration": "00:03:39.6610000",
    "LastWriteTime": "2024-09-12T09:40:54+09:00"
  }
]
```

> [!NOTE]
> This is the basically same format that the `--cache-tags` option of [my AudioTagger utility](https://github.com/codeconscious/audiotagger) outputs. The advantage of using this tool instead is that it compares tag data against files' last-modified dates and only updates out-of-date tag information, making the operation considerably faster, particularly when your audio files are on a slow external drive, etc.


## 2. Finding duplicates

First, you must already have a tag library file containing your cached tag data. Check the section above if you don't have one yet.

Second, you must have a settings file containing the following:

1. Paths
   1. The directory in which to save the playlist of duplicates.
   2. The directory path substring to _remove_ from each file path within the playlist file for duplicates. Leave blank if unneeded.
   3. The directory path substring to _prepend_ to each file path within the playlist file for duplicates. Leave blank if unneeded.
   - Note: The final two items function as a pair, allowing you to modify the paths of your files for when your main audio directory's location differs across devices.
2. Exclusions
   - Regular expressions to identify tracks, by artist name and/or title, that you wish to ignore during comparison. These might be tracks with identical artists and track names that you know are actually disparate tracks. (A rare but mildly annoying occurrence.)
3. Equivalent artist names
   - Artists that should be considered identical during comparison. These are _not_ regular expressions.
   - Particularly useful for artists that release under multiple names or both in and without bands.
   - Example: `["Bon Jovi", "Jon Bon Jovi"]`
4. Artist replacements
   - Regular expressions to remove matching substrings from artist names. Case-insensitive.
   - Replacements are in memory for comparison purposes only. No file updates are made.
   - Example: `["The "]`, which would ensure "The Four Tops" and "Four Tops" are considered identically.
5. Title replacements
   - Regular expressions to remove matching substrings from titles. Case-insensitive.
   - Replacements are in memory for comparison purposes only. No file updates are made.

<details>
  <summary>Click to expand a sample settings file.</summary>

```json
{
  "playlist": {
    "saveDirectory": "/Users/me/Downloads/NewAudio",
    "pathSearchFor": "/Users/me/Documents/Media/",
    "pathReplaceWith": ""
  },
  "exclusionPatterns": [
    {
      "artist": "EXACT_ARTIST_NAME",
      "title": "TRACK_NAME_REGEX_PATTERN.*"
    },
    {
      "artist": "SAMPLE_ARTIST_NAME"
    },
    {
      "title": "SAMPLE_TRACK_NAME.*"
    }
  ],
  "equivalentArtists": [
      ["artistOldName", "artistNewName"],
      ["artistName", "bandThatArtistIsIn"],
  ],
  "artistReplacementPatterns": [
    "The\\s",
    "ザ・"
  ],
  "titleReplacementPatterns": [
    "feat(uring)?\\.?\\s?.+",
    "−",
    "—",
    "~",
    "～",
    "|",
    "｜",
    "=",
    "＝",
    "\\+",
    "＋",
    "✖",
    "❌",
  ]
}
```

</details>

To start, use the `find-duplicates` command like this:

```sh
dotnet run -- find-duplicates ~/Documents/Audio/findDupeSettings.json ~/Documents/Audio/tagLibrary.json
```

If any potential duplicates are found, they will be listed, grouped by artist. If you see false positives (i.e., tracks that were detected as duplicates, but are actually not), you can add entries to the exclusion patterns in your settings to ignore them in the future.


## 3. Exporting artist genres

Creates a text file containing a list of artists with the genre that they are most associated with in your tag library.

To use it, pass `export-genres` with two arguments:

1. The path of your library file
2. The path of the text file that contains (or will contain, if it does not exist yet) your artists with corresponding genres

Sample:

```sh
dotnet run -- export-genres ~/Downloads/Audio/tagLibrary.json ~/Downloads/Audio/genres.txt
```

If a genres file already exists at that path, a backup will be created automatically.


## 4. Analyzing cached tags

You must already have a tag library file containing your cached tag data. Check above if you don't have one yet.

To start, simply use the `analyze-library` command like this:

```sh
dotnet run -- analyze-library ~/Downloads/Audio/tagLibrary.json
```

Several categories of data (e.g., most common artists, largest files) will be displayed.


## Other

### Exit codes

The program returns the following exit codes:

| Code | Meaning                                           |
|------|---------------------------------------------------|
| 0    | Finished successfully                             |
| 1    | Invalid argument count                            |
| 2    | Invalid command                                   |
| 3    | Failure during the requested command's operation  |
