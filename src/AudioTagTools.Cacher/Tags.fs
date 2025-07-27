module Tags

open System
open System.Text.Json
open IO
open Errors
open Operators
open Utilities
open FsToolkit.ErrorHandling
open TagLibrary

type TagMap = Map<string, LibraryTags>

type LibraryComparisonResult =
    | Unchanged // Library tags match file tags.
    | OutOfDate // Library tags are older than file tags.
    | NotPresent // No tags exist in library for file.

type CategorizedTagsToCache =
    { Type: LibraryComparisonResult
      Tags: LibraryTags }

let createTagLibraryMap (libraryFile: FileInfo) : Result<TagMap, Error> =

    let audioFilePath (fileTags: LibraryTags) : string =
        Path.Combine [| fileTags.DirectoryName; fileTags.FileNameOnly |]

    if libraryFile.Exists
    then
        libraryFile.FullName
        |> readfile
        >>= IO.parseJsonToTags
        <!> Array.map (fun tags -> audioFilePath tags, tags)
        <!> Map.ofArray
    else
        Ok Map.empty

let private prepareTagsToWrite (tagLibraryMap: TagMap) (fileInfos: FileInfo seq)
    : CategorizedTagsToCache seq
    =
    let copyCachedTags (libraryTags: LibraryTags) =
        { libraryTags with LastWriteTime = DateTimeOffset libraryTags.LastWriteTime.DateTime }

    let generateNewTags (fileInfo: FileInfo) : LibraryTags =
       let tagsFromFile (fileInfo: FileInfo) (fileTags: FileTags) =
            {
                FileNameOnly = fileInfo.Name
                DirectoryName = fileInfo.DirectoryName
                Artists = fileTags.Tag.Performers |> Array.map _.Normalize()
                AlbumArtists = fileTags.Tag.AlbumArtists |> Array.map _.Normalize()
                Album = fileTags.Tag.Album
                        |> Option.ofObj
                        |> Option.map _.Normalize()
                        |> Option.defaultValue String.Empty
                TrackNo = fileTags.Tag.Track
                Title = fileTags.Tag.Title
                        |> Option.ofObj
                        |> Option.map _.Normalize()
                        |> Option.defaultValue String.Empty
                Year = fileTags.Tag.Year
                Genres = fileTags.Tag.Genres
                Duration = fileTags.Properties.Duration
                BitRate = fileTags.Properties.AudioBitrate
                SampleRate = fileTags.Properties.AudioSampleRate
                FileSize = fileInfo.Length
                LastWriteTime = DateTimeOffset fileInfo.LastWriteTime
            }

       match parseFileTags fileInfo.FullName with
       | Ok (Some tags) -> tagsFromFile fileInfo tags
       | _ -> blankTags fileInfo

    let prepareTagsToCache (tagLibraryMap: TagMap) (audioFile: FileInfo) : CategorizedTagsToCache =
        if Map.containsKey audioFile.FullName tagLibraryMap
        then
            let libraryTags = Map.find audioFile.FullName tagLibraryMap
            if libraryTags.LastWriteTime.DateTime < audioFile.LastWriteTime
            then { Type = OutOfDate; Tags = (generateNewTags audioFile) }
            else { Type = Unchanged; Tags = (copyCachedTags libraryTags) }
        else { Type = NotPresent; Tags = (generateNewTags audioFile) }

    fileInfos
    |> Seq.map (prepareTagsToCache tagLibraryMap)

let private reportResults (results: CategorizedTagsToCache seq) : CategorizedTagsToCache seq =
    let initialCounts = {| NotPresent = 0; OutOfDate = 0; Unchanged = 0 |}

    let totals =
        (initialCounts, Seq.map _.Type results)
        ||> Seq.fold (fun acc result ->
            match result with
            | NotPresent -> {| acc with NotPresent = acc.NotPresent + 1 |}
            | OutOfDate -> {| acc with OutOfDate = acc.OutOfDate + 1 |}
            | Unchanged -> {| acc with Unchanged = acc.Unchanged + 1 |})

    printfn "Results:"
    printfn "• New:       %s" (formatNumber totals.NotPresent)
    printfn "• Updated:   %s" (formatNumber totals.OutOfDate)
    printfn "• Unchanged: %s" (formatNumber totals.Unchanged)
    printfn "• Total:     %s" (formatNumber (Seq.length results))

    results

let generateNewJson
    (tagLibraryMap: TagMap)
    (fileInfos: FileInfo seq)
    : Result<string, Error>
    =
    fileInfos
    |> prepareTagsToWrite tagLibraryMap
    |> reportResults
    |> Seq.map _.Tags
    |> serializeToJson
    |> Result.mapError JsonSerializationError
