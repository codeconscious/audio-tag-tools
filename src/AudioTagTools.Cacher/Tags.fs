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
    | FileUnchanged  // Library tags match file tags.
    | FileUpdated  // Library tags are older than file tags.
    | FileIsOlder
    | FileToAdd // No tags exist in library for file.
    | FileDeleted // Tags exist, but file is now missing.

type DeletedItemCount = DeletedItemCount of uint

type CategorizedTagsToCache =
    { Type: LibraryComparisonResult
      Tags: LibraryTags }

let createTagLibraryMap (libraryFile: FileInfo) : Result<LibraryTagMap, CommandError> =
    if libraryFile.Exists
    then
        libraryFile
        |> File.readText'
        >>= (Json >> parseJsonToTags)
        |>> (List.map groupByPath >> Map.ofList)
        |!! LibraryTagParseError
    else
        Ok Map.empty

let private prepareTagsToWrite tagLibraryMap fileInfos : CategorizedTagsToCache nseq =
    let copyCachedTags (libraryTags: LibraryTags) =
        { libraryTags with LastWriteTime = DateTimeOffset libraryTags.LastWriteTime.DateTime }

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

    let prepareTagsToCache tagLibraryMap (audioFile: FileInfo) : CategorizedTagsToCache =
        if tagLibraryMap |> Map.containsKey audioFile.FullName
        then
            let libraryTags = tagLibraryMap |> Map.find audioFile.FullName
            match compareWith libraryTags.LastWriteTime.DateTime audioFile.LastWriteTime with
            | GT -> { Type = FileUpdated; Tags = generateNewTags audioFile }
            | EQ -> { Type = FileUnchanged; Tags = copyCachedTags libraryTags }
            | LT -> { Type = FileIsOlder; Tags = generateNewTags audioFile }
        else { Type = FileToAdd; Tags = generateNewTags audioFile }

    fileInfos
    |> NSeq.map (prepareTagsToCache tagLibraryMap)

let private countDeletedFiles tagLibraryMap categorizedTags =
    let filePaths =
        categorizedTags
        |> NSeq.map (fun t -> filePath t.Tags)
        |> NList.ofNonEmptySeq

    let orphanedLibraryTagCount =
        tagLibraryMap
        |> Map.filter (fun libraryPath _ -> not (filePaths |> NList.contains libraryPath))
        |> _.Count
        |> uint

    (categorizedTags, DeletedItemCount orphanedLibraryTagCount)

let private reportResults (categorizedTags, DeletedItemCount deletedCount) : CategorizedTagsToCache nseq =
    let categoryTotals = categorizedTags |> NSeq.countBy _.Type |> Map.ofSeq

    let countOf comparisonResultType =
        categoryTotals
        |> Map.tryFindElse comparisonResultType 0
        |> String.formatInt

    let grandTotal = categoryTotals |> Map.values |> Seq.sum |> String.formatInt

    printfn "Results:"
    printfn "• New:          %s" (countOf FileToAdd)
    printfn "• Deleted:      %s" (String.formatNumber deletedCount)
    printfn "• File Updated: %s" (countOf FileUpdated)
    printfn "• Lib. Updated: %s" (countOf FileIsOlder) // TODO: Rare case. Maybe combine?
    printfn "• Unchanged:    %s" (countOf FileUnchanged)
    printfn "• Total:        %s" grandTotal

    categorizedTags

let generateJson tagMap fileInfos : Result<string, CommandError> =
    fileInfos
    |> prepareTagsToWrite tagMap
    |> countDeletedFiles tagMap
    |> reportResults
    |> NonEmptySeq.map _.Tags
    |> String.toJson
    |!! JsonSerializationError
