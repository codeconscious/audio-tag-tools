module Shared.TagLibrary

open Shared.Types
open CCFSharpUtils
open CCFSharpUtils.Collections
open CCFSharpUtils.Text
open System
open System.IO
open System.Text.Json
open FSharpPlus
open FSharpPlus.Data

module NSeq = NonEmptySeq

type FileTags = TagLib.File
type FilePath = string

/// Creates an instance representing a file's tags, which might or might not exists.
/// If tags do exist, they are wrapped in Some. Otherwise, then None is given.
let parseFileTags (file: FileInfo) : Result<FileTags option, string> =
    try file.FullName |> FileTags.Create |> Option.ofObj |> Ok
    with exn -> Error exn.Message

type LibraryTags =
    { FileName: string
      DirectoryName: string
      FileSize: int64
      Artists: string list
      AlbumArtists: string list
      Album: string
      DiscNo: uint
      TrackNo: uint
      Title: string
      Year: uint
      Genres: string list
      Duration: TimeSpan
      BitRate: int
      SampleRate: int
      ImageCount: int
      LastWriteTime: DateTimeOffset }
    with
        static member Empty (fileInfo: FileInfo) =
            { FileName = fileInfo.Name
              DirectoryName = fileInfo.DirectoryName
              FileSize = 0
              Artists = [String.Empty]
              AlbumArtists = [String.Empty]
              Album = String.Empty
              DiscNo = 0u
              TrackNo = 0u
              Title = String.Empty
              Year = 0u
              Genres = [String.Empty]
              Duration = TimeSpan.Zero
              BitRate = 0
              SampleRate = 0
              ImageCount = 0
              LastWriteTime = DateTimeOffset fileInfo.LastWriteTime }

        static member NewFromFileTags (file: FileInfo) (fileTags: FileTags) =
            { FileName = file.Name
              DirectoryName = file.DirectoryName
              FileSize = file.Length
              Artists = fileTags.Tag.Performers
                        |> List.ofArray
                        |> List.map _.Normalize()
              AlbumArtists = fileTags.Tag.AlbumArtists
                             |> List.ofArray
                             |> List.map _.Normalize()
              Album = fileTags.Tag.Album
                      |> Option.ofObj
                      |> Option.defaultValue String.Empty
                      |> _.Normalize()
              DiscNo = fileTags.Tag.Disc
              TrackNo = fileTags.Tag.Track
              Title = fileTags.Tag.Title
                      |> Option.ofObj
                      |> Option.defaultValue String.Empty
                      |> _.Normalize()
              Year = fileTags.Tag.Year
              Genres = fileTags.Tag.Genres |> List.ofArray
              Duration = fileTags.Properties.Duration
              BitRate = fileTags.Properties.AudioBitrate
              SampleRate = fileTags.Properties.AudioSampleRate
              ImageCount = fileTags.Tag.Pictures.Length
              LastWriteTime = DateTimeOffset file.LastWriteTime }

        static member Copy tags =
            { tags with LastWriteTime = DateTimeOffset tags.LastWriteTime.DateTime }

        static member Generate audioFile =
            match parseFileTags audioFile with
            | Ok (Some fileTags) -> LibraryTags.NewFromFileTags audioFile fileTags
            | _                  -> LibraryTags.Empty audioFile

type ComparisonResult = Unchanged | OutOfSync | NewFile

type CheckedLibTags = { Status: ComparisonResult; Tags: LibraryTags }

let checkAudioFiles libMap audioFiles : CheckedLibTags nseq =
    let prepareTagsToCache tagLibMap (audioFile: FileInfo) : CheckedLibTags =
        match tagLibMap |> Map.tryFind audioFile.FullName with
        | Some libTags ->
            match libTags.LastWriteTime.DateTime </compare'/> audioFile.LastWriteTime with
            | EQ -> { Status = Unchanged; Tags = LibraryTags.Copy libTags }
            | _  -> { Status = OutOfSync; Tags = LibraryTags.Generate audioFile }
        | None ->   { Status = NewFile;   Tags = LibraryTags.Generate audioFile }

    audioFiles |> NSeq.map (prepareTagsToCache libMap)

type DuplicateTags = LibraryTags nlist nlist

let parseJsonToTags (Json json) : Result<LibraryTags list, string> =
    try Ok (JsonSerializer.Deserialize<LibraryTags list> json)
    with exn -> Error exn.Message

let parseJsonToNonEmptyTags json : Result<LibraryTags nlist, string> =
    parseJsonToTags json >>= List.toNonEmptyListResult "No tags were found to parse."

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
    artists |> List.except ignoredArtists

let isNotIgnoredArtist artist =
    not (List.exists ((=) artist) ignoredArtists)

let tryMainArtistName tags : string option =
    tags.AlbumArtists
    |> List.tryHead
    |> Option.orElse (List.tryHead tags.Artists)

let tryMainArtist tags : Artist option =
    tags |> tryMainArtistName |> Option.map Artist

let allUniqueArtists tags : Artist list =
    List.concat [ tags.Artists; tags.AlbumArtists ]
    |> List.distinct
    |> List.map Artist

let tryFirstUniqueArtist tags : Artist option =
    tags |> allUniqueArtists |> List.tryHead

let mainArtistSummary separator tags : string =
    let albumArtists =
        tags.AlbumArtists
        |> List.map Artist
        |> dropIgnoredArtists
        |> List.map (fun (Artist name) -> name)

    (if List.isNotEmpty albumArtists then albumArtists else tags.Artists)
    |> String.concat separator

let hasAnyArtist tags : bool =
    List.anyNotEmpty [ tags.Artists; tags.AlbumArtists ]

let hasTitle tags : bool =
    String.hasText tags.Title

let hasArtistAndTitle tags : bool =
    hasAnyArtist tags && hasTitle tags
