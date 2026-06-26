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

type LibraryTagMap = Map<FilePath, LibraryTags>

type LibraryComparisonResult =
    | UpToDate // Library tags match file tags.
    | LibraryOutOfDate // Library tags are older than file tags.
    | FileOutOfDate // Library tags are newer than file tags.
    | NewFile // No tags for file exist in library yet.
    | FileDeleted // Library tags exist, but file is now missing.

type DeletedItemCount = DeletedFileCount of uint

type TagsToCache =
    { Type: LibraryComparisonResult
      Tags: LibraryTags }

let createTagLibraryMap (libFile: FileInfo) : Result<LibraryTagMap, CommandError> =
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
       | _ -> blankTags fileInfo

    let prepareTagsToCache tagLibMap (audioFile: FileInfo) : TagsToCache =
        if tagLibMap |> Map.containsKey audioFile.FullName
        then
            let libTags = tagLibMap |> Map.find audioFile.FullName
            match compareWith libTags.LastWriteTime.DateTime audioFile.LastWriteTime with
            | GT -> { Type = LibraryOutOfDate; Tags = generateNewTags audioFile }
            | EQ -> { Type = UpToDate; Tags = copyCachedTags libTags }
            | LT -> { Type = FileOutOfDate; Tags = generateNewTags audioFile }
        else { Type = NewFile; Tags = generateNewTags audioFile }

    fileInfos
    |> NSeq.map (prepareTagsToCache tagLibMap)

let private countDeletedFiles tagLibMap categorizedTags =
    let filePaths = categorizedTags |> NSeq.map (fun t -> filePath t.Tags) |> set

    let orphanedLibTagCount =
        tagLibMap
        |> Map.filter (fun libPath _ -> not (filePaths |> Set.contains libPath))
        |> _.Count
        |> uint

    (categorizedTags, DeletedFileCount orphanedLibTagCount)

let private reportResults (categorizedTags, DeletedFileCount deletedCount) : unit =
    let categoryTotals = categorizedTags |> NSeq.countBy _.Type |> Map.ofSeq

    let countOf comparisonResult =
        categoryTotals
        |> Map.tryFindElse comparisonResult 0
        |> String.formatInt

    let grandTotal = categoryTotals |> Map.values |> Seq.sum |> String.formatInt

    printfn "Results:"
    printfn "• New:         %s" (countOf NewFile)
    printfn "• Deleted:     %s" (String.formatNumber deletedCount)
    printfn "• Out of sync: %s" (countOf LibraryOutOfDate + countOf FileOutOfDate)
    printfn "• Unchanged:   %s" (countOf UpToDate)
    printfn "• Total:       %s" grandTotal

let generateJson tagMap fileInfos : Result<string, CommandError> =
    fileInfos
    |> prepareTagsToCache tagMap
    |> countDeletedFiles tagMap
    |- reportResults
    |> (fst >> map _.Tags)
    |> String.toJson
    |!! JsonSerializationError
