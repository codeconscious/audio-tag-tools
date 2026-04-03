module Shared.TagLibrary

open CCFSharpUtils.Library
open System
open System.IO
open System.Text.Json
open FSharpPlus
open FSharpPlus.Data

type FileTags = TagLib.File

type LibraryTags =
    { FileName: string
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
      LastWriteTime: DateTimeOffset }

let blankTags (fileInfo: FileInfo) : LibraryTags =
    { FileName = fileInfo.Name
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
      LastWriteTime = DateTimeOffset fileInfo.LastWriteTime }

let parseJsonToTags (json: string) : Result<LibraryTags list, string> =
    try Ok (JsonSerializer.Deserialize<LibraryTags list>(json))
    with exn -> Error exn.Message

let parseJsonToNonEmptyTags (json: string) : Result<LibraryTags NonEmptyList option, string> =
    try
        json
        |> parseJsonToTags
        |>> List.toNonEmptyListOption
    with exn -> Error exn.Message

let parseFileTags (filePath: string) : Result<FileTags option, string> =
    try filePath |> FileTags.Create |> Option.ofObj |> Ok
    with exn -> Error exn.Message

let path tags : string =
    Path.Combine [| tags.DirectoryName; tags.FileName |]

let groupWithPath tags : string * LibraryTags =
    path tags, tags

let ignorableAlbumArtists =
    [ String.Empty
      "Various"
      "Various Artists"
      "Multiple Artists"
      "\u003Cunknown\u003E" ]

let allDistinctArtists (tags: LibraryTags) : string list =
    Array.concat [ tags.Artists; tags.AlbumArtists ]
    |> Array.distinct
    |> List.ofArray

let mainArtists (separator: string) (track: LibraryTags) : string =
    let hasNoIgnoredAlbumArtists artist =
        ignorableAlbumArtists
        |> List.exists _.Equals(artist, StringComparison.InvariantCultureIgnoreCase)
        |> not

    match track with
    | t when Array.isNotEmpty t.AlbumArtists && hasNoIgnoredAlbumArtists t.AlbumArtists[0] ->
        t.AlbumArtists
    | t ->
        t.Artists
    |> String.concat separator

let firstDistinctArtist (tags: LibraryTags) : string =
    tags |> allDistinctArtists |> List.head

let hasAnyArtist (tags: LibraryTags) : bool =
    Array.anyNotEmpty [| tags.Artists; tags.AlbumArtists |]

let hasTitle (tags: LibraryTags) : bool =
    String.hasText tags.Title

let hasArtistAndTitle track : bool =
    hasAnyArtist track && hasTitle track
