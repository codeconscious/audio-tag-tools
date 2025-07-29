module TagLibrary

open System
open System.IO
open System.Text.Json

type LibraryTags =
    {
        FileName: string
        DirectoryName: string
        Artists: string array
        AlbumArtists: string array
        Album: string
        DiscNo: uint
        TrackNo: uint
        Title: string
        Year: uint
        Genres: string array
        Duration: TimeSpan
        BitRate: int
        SampleRate: int
        FileSize: int64
        ImageCount: int
        LastWriteTime: DateTimeOffset
    }

let blankTags (fileInfo: FileInfo) : LibraryTags =
    {
        FileName = fileInfo.Name
        DirectoryName = fileInfo.DirectoryName
        Artists = [| String.Empty |]
        AlbumArtists = [| String.Empty |]
        Album = String.Empty
        DiscNo = 0u
        TrackNo = 0u
        Title = String.Empty
        Year = 0u
        Genres = [| String.Empty |]
        Duration = TimeSpan.Zero
        BitRate = 0
        SampleRate = 0
        FileSize = 0
        ImageCount = 1
        LastWriteTime = DateTimeOffset fileInfo.LastWriteTime
    }


let parseJsonToTags (json: string) : Result<LibraryTags array, string> =
    try Ok (JsonSerializer.Deserialize<LibraryTags array>(json))
    with e -> Error e.Message

