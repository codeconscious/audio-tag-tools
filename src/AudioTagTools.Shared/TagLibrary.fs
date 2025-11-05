module Shared.TagLibrary

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

type MultipleLibraryTags = LibraryTags array

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
        ImageCount = 0
        LastWriteTime = DateTimeOffset fileInfo.LastWriteTime
    }

let parseJsonToTags (json: string) : Result<MultipleLibraryTags, string> =
    try Ok (JsonSerializer.Deserialize<MultipleLibraryTags>(json))
    with e -> Error e.Message

let ignorableArtists =
    [| String.Empty
       "Various"
       "Various Artists"
       "Multiple Artists"
       "\u003Cunknown\u003E" |]

let allDistinctArtists (t: LibraryTags) : string array =
    Array.concat [| t.Artists; t.AlbumArtists |]
    |> Array.distinct

let mainArtists (separator: string) (track: LibraryTags) : string =
    let hasNoForbiddenAlbumArtists artist =
        ignorableArtists
        |> Array.exists _.Equals(artist, StringComparison.InvariantCultureIgnoreCase)
        |> not

    match track with
    | t when t.AlbumArtists.Length > 0 && hasNoForbiddenAlbumArtists t.AlbumArtists[0] ->
        t.AlbumArtists
    | t ->
        t.Artists
    |> String.concat separator

let firstDistinctArtist (t: LibraryTags) : string =
    t |> allDistinctArtists |> Array.head

let hasAnyArtist (track: LibraryTags) : bool =
    track.Artists.Length > 0 ||
    track.AlbumArtists.Length > 0

let hasTitle (track: LibraryTags) : bool =
    not <| String.IsNullOrWhiteSpace track.Title

let hasArtistAndTitle track : bool =
    hasAnyArtist track && hasTitle track
