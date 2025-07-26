module TagLibrary

open System
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
    }

let parseJsonToTags (json: string) : Result<LibraryTags array, string> =
    try Ok (JsonSerializer.Deserialize<LibraryTags array>(json))
    with e -> Error e.Message

