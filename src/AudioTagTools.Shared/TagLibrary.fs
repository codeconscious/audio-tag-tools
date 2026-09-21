module Shared.TagLibrary

open Shared.Types
open CCFSharpUtils
open CCFSharpUtils.Collections
open CCFSharpUtils.Text
open System
open System.IO
open System.Text.Json
open FSharpPlus

type FileTags = TagLib.File
type FilePath = string

type LibraryTags =
    { FileName: string
      DirectoryName: string
      Artists: Artist array option
      AlbumArtists: Artist array option
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

type DuplicateTags = LibraryTags nlist nlist

let emptyTags (fileInfo: FileInfo) : LibraryTags =
    { FileName = fileInfo.Name
      DirectoryName = fileInfo.DirectoryName
      Artists = None
      AlbumArtists = None
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

let parseJsonToTags (Json json) : Result<LibraryTags list, string> =
    try Ok (JsonSerializer.Deserialize<LibraryTags list> json)
    with exn -> Error exn.Message

let parseJsonToNonEmptyTags json : Result<LibraryTags nlist, string> =
    parseJsonToTags json >>= List.toNonEmptyListResult "No tags were found to parse."

/// Creates an instance representing a file's tags. The file itself might
/// or might not already contain tags. If it does, the tags are wrapped in Some.
/// Otherwise (i.e., if the tags are null), then None is given.
let parseFileTags (file: FileInfo) : Result<FileTags option, string> =
    try file.FullName |> FileTags.Create |> Option.ofObj |> Ok
    with exn -> Error exn.Message

let filePath tags : FilePath =
    Path.Combine [| tags.DirectoryName; tags.FileName |]

let groupByPath tags : FilePath * LibraryTags =
    (filePath tags, tags)

let ignoredArtists =
    [ String.Empty
      "Various"
      "Various Artists"
      "Multiple Artists"
      "\u003Cunknown\u003E" ] // U+003C == `<` and \u003E == `>` ]
    |> List.map Artist

let dropIgnoredArtists artists =
    artists |> Array.except ignoredArtists

let isNotIgnoredArtist artist =
    not (List.exists ((=) artist) ignoredArtists)

let allUniqueArtists tags : Artist list =
    [ Option.toArray tags.Artists; Option.toArray tags.AlbumArtists ]
    |> Array.concat
    |> Array.concat
    |> Array.distinct
    |> List.ofArray

let tryFirstUniqueArtist tags : Artist option =
    tags |> allUniqueArtists |> List.tryHead

let mainArtistSummary separator tags : string =
    match tags.AlbumArtists, tags.Artists with
    | Some albumArtists, _ -> albumArtists
    | None, Some artists -> artists
    | _ -> [||]
    |> Array.map (fun (Artist a) -> a)
    |> String.concat separator

let hasAnyArtist tags : bool =
    tags.AlbumArtists.IsSome || tags.Artists.IsSome

let hasTitle tags : bool =
    String.hasText tags.Title

let hasArtistAndTitle tags : bool =
    hasAnyArtist tags && hasTitle tags
