module TagLibrary

open System
open System.IO
open System.Text.Json

type LibraryTags =
    {
        FileNameOnly: string
        DirectoryName: string
        Artists: string array
        AlbumArtists: string array
        Album: string
        TrackNo: uint
        Title: string
        Year: uint
        Genres: string array
        Duration: TimeSpan
        LastWriteTime: DateTimeOffset
        BitRate: int
        SampleRate: int
        FileSize: int64
    }

let blankTags (fileInfo: FileInfo) : LibraryTags =
    {
        FileNameOnly = fileInfo.Name
        DirectoryName = fileInfo.DirectoryName
        Artists = [| String.Empty |]
        AlbumArtists = [| String.Empty |]
        Album = String.Empty
        TrackNo = 0u
        Title = String.Empty
        Year = 0u
        Genres = [| String.Empty |]
        Duration = TimeSpan.Zero
        BitRate = 0
        SampleRate = 0
        FileSize = 0
        LastWriteTime = DateTimeOffset fileInfo.LastWriteTime
    }


let parseJsonToTags (json: string) : Result<LibraryTags array, string> =
    try Ok (JsonSerializer.Deserialize<LibraryTags array>(json))
    with e -> Error e.Message

