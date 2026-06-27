module Cacher.Tags

open System
open IO
open Errors
open Shared.TagLibrary
open Shared.Types
open CCFSharpUtils
open CCFSharpUtils.Collections
open CCFSharpUtils.IO
open CCFSharpUtils.Operators
open CCFSharpUtils.Text
open FSharpPlus.Data
open FSharpPlus.Operators

module NList = NonEmptyList
module NSeq = NonEmptySeq

type private LibTagMap = Map<FilePath, LibraryTags>

type ComparisonResult =
    | UpToDate // Library tags match file tags.
    | LibOutOfDate // Library tags are older than file tags.
    | FileOutOfDate // Library tags are newer than file tags.
    | NewFile // No tags for file exist in library yet.
    | FileDeleted // Library tags exist, but file is now missing.

type TagsToCache =
    { ComparisonResult: ComparisonResult
      Tags: LibraryTags option }

let createTagLibMap (libFile: FileInfo) : Result<LibTagMap, CommandError> =
    if libFile.Exists
    then
        File.readText' libFile
        >>= (Json >> parseJsonToTags)
        |>> (List.map groupByPath >> Map.ofList)
        |!! LibraryTagParseError
    else
        Ok Map.empty

let private prepareTagsToCache tagLibMap fileInfos : TagsToCache nseq =
    let copyCachedTags (libTags: LibraryTags) =
        { libTags with LastWriteTime = DateTimeOffset libTags.LastWriteTime.DateTime }

    let generateNewTags (fileInfo: FileInfo) : LibraryTags =
       let tagsFromFile (fileTags: FileTags) =
            {
                FileName = fileInfo.Name
                DirectoryName = fileInfo.DirectoryName
                Artists = fileTags.Tag.Performers |> Array.map _.Normalize()
                AlbumArtists = fileTags.Tag.AlbumArtists |> Array.map _.Normalize()
                Album = match fileTags.Tag.Album with
                        | null  -> String.Empty
                        | album -> album.Normalize()
                DiscNo = fileTags.Tag.Disc
                TrackNo = fileTags.Tag.Track
                Title = match fileTags.Tag.Title with
                        | null  -> String.Empty
                        | title -> title.Normalize()
                Year = fileTags.Tag.Year
                Genres = fileTags.Tag.Genres
                Duration = fileTags.Properties.Duration
                BitRate = fileTags.Properties.AudioBitrate
                SampleRate = fileTags.Properties.AudioSampleRate
                FileSize = fileInfo.Length
                ImageCount = fileTags.Tag.Pictures.Length
                LastWriteTime = DateTimeOffset fileInfo.LastWriteTime
            }

       match parseFileTags fileInfo with
       | Ok (Some tags) -> tagsFromFile tags
       | _              -> blankTags fileInfo

    let prepareTagsToCache tagLibMap (audioFile: FileInfo) : TagsToCache =
        if tagLibMap |> Map.containsKey audioFile.FullName
        then
            let libTags = tagLibMap |> Map.find audioFile.FullName
            match compareWith libTags.LastWriteTime.DateTime audioFile.LastWriteTime with
            | EQ -> { ComparisonResult = UpToDate; Tags = Some (copyCachedTags libTags) }
            | GT -> { ComparisonResult = LibOutOfDate;  Tags = Some (generateNewTags audioFile) }
            | LT -> { ComparisonResult = FileOutOfDate; Tags = Some (generateNewTags audioFile) }
        else { ComparisonResult = NewFile; Tags = Some (generateNewTags audioFile) }

    fileInfos |> NSeq.map (prepareTagsToCache tagLibMap)

let private addDeletedFiles tagLibMap categorizedTags =
    let filePaths =
        categorizedTags
        |> NSeq.choose (fun t -> match t.Tags with Some t -> Some (filePath t) | None -> None)
        |> set

    let orphanedLibTags =
        tagLibMap
        |> Map.filter (fun libPath _ -> not (filePaths |> Set.contains libPath))
        |> Map.values
        |> Seq.map (fun _ -> { ComparisonResult = FileDeleted; Tags = None })
        |> NSeq.tryOfSeq

    match orphanedLibTags with
    | Some t -> categorizedTags |> NSeq.append t
    | None   -> categorizedTags

let private reportResults categorizedTags : unit =
    let categoryTotals = categorizedTags |> NSeq.countBy _.ComparisonResult |> Map.ofSeq

    let countOf category = categoryTotals |> Map.tryFindElse category 0 |> String.formatInt

    let grandTotal = categoryTotals |> Map.values |> sum

    printfn "Results:"
    printfn "+ New:         %s" (countOf NewFile)
    printfn "+ Out of sync: %s" (countOf LibOutOfDate + countOf FileOutOfDate)
    printfn "+ Unchanged:   %s" (countOf UpToDate)
    printfn "- Deleted:     %s" (countOf FileDeleted)
    printfn "= New Total:   %s" (String.formatInt grandTotal)

let generateJson tagMap fileInfos : Result<string, CommandError> =
    fileInfos
    |> prepareTagsToCache tagMap
    |> addDeletedFiles tagMap
    |- reportResults
    |> map _.Tags
    |> String.toJson
    |!! JsonSerializationError
