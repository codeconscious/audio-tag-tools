module Cacher.Tags

open System
open IO
open Errors
open Shared
open Shared.TagLibrary
open FsToolkit.ErrorHandling

type LibraryTagMap = Map<string, LibraryTags>

type LibraryComparisonResult =
    | Unchanged // Library tags match file tags.
    | OutOfDate // Library tags are older than file tags.
    | NotPresent // No tags exist in library for file.

type CategorizedTagsToCache =
    { Type: LibraryComparisonResult
      Tags: LibraryTags }

let createTagLibraryMap (libraryFile: FileInfo) : Result<LibraryTagMap, Error> =
    let audioFilePath (fileTags: LibraryTags) : string =
        Path.Combine [| fileTags.DirectoryName; fileTags.FileName |]

    if libraryFile.Exists
    then
        libraryFile.FullName
        |> readfile
        >>= IO.parseJsonToTags
        <!> Array.map (fun tags -> audioFilePath tags, tags)
        <!> Map.ofArray
    else
        Ok Map.empty

let private prepareTagsToWrite (tagLibraryMap: LibraryTagMap) (fileInfos: FileInfo seq)
    : CategorizedTagsToCache seq =

    let copyCachedTags (libraryTags: LibraryTags) =
        { libraryTags with LastWriteTime = DateTimeOffset libraryTags.LastWriteTime.DateTime }

    let generateNewTags (fileInfo: FileInfo) : LibraryTags =
       let tagsFromFile (fileInfo: FileInfo) (fileTags: FileTags) =
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

       match parseFileTags fileInfo.FullName with
       | Ok (Some tags) -> tagsFromFile fileInfo tags
       | _ -> blankTags fileInfo

    let prepareTagsToCache (tagLibraryMap: LibraryTagMap) (audioFile: FileInfo) : CategorizedTagsToCache =
        if Map.containsKey audioFile.FullName tagLibraryMap
        then
            let libraryTags = Map.find audioFile.FullName tagLibraryMap
            if libraryTags.LastWriteTime.DateTime < audioFile.LastWriteTime
            then { Type = OutOfDate; Tags = generateNewTags audioFile }
            else { Type = Unchanged; Tags = copyCachedTags libraryTags }
        else { Type = NotPresent; Tags = generateNewTags audioFile }

    fileInfos
    |> Seq.map (prepareTagsToCache tagLibraryMap)

let private reportResults (categorizedTags: CategorizedTagsToCache seq) : CategorizedTagsToCache seq =
    let categoryTotals =
        categorizedTags
        |> Seq.countBy _.Type
        |> Map.ofSeq

    let countOf comparisonResultType =
        categoryTotals
        |> Map.tryFind comparisonResultType
        |> Option.defaultValue 0
        |> formatInt

    let grandTotal =
        categoryTotals
        |> Map.values
        |> Seq.sum
        |> formatInt

    printfn "Results:"
    printfn "• New:       %s" (countOf NotPresent)
    printfn "• Updated:   %s" (countOf OutOfDate)
    printfn "• Unchanged: %s" (countOf Unchanged)
    printfn "• Total:     %s" grandTotal

    categorizedTags

let generateJson
    (tagLibraryMap: LibraryTagMap)
    (fileInfos: FileInfo seq)
    : Result<string, Error>
    =
    fileInfos
    |> prepareTagsToWrite tagLibraryMap
    |> reportResults
    |> Seq.map _.Tags
    |> serializeToJson
    |> Result.mapError JsonSerializationError
