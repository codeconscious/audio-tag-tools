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

type private LibPathTagMap = Map<FilePath, LibraryTags>

type private ComparisonResult = UpToDate | OutOfSync | NewFile | FileDeleted

type private NewLibTags = { Status: ComparisonResult; Tags: LibraryTags option }

let createTagLibMap (libFile: FileInfo) : Result<LibPathTagMap, CommandError> =
    if libFile.Exists
    then
        File.readText' libFile
        >>= (Json >> parseJsonToTags)
        |>> (map groupByPath >> Map.ofList)
        |!! LibraryTagParseError
    else
        Ok Map.empty

let private generateNewLibTags libMap audioFiles : NewLibTags nseq =
    let copyCachedTags libTags =
        { libTags with LastWriteTime = DateTimeOffset libTags.LastWriteTime.DateTime }

    let generateNewTags (file: FileInfo) : LibraryTags =
       let tagsFromFile (fileTags: FileTags) =
            {
                FileName = file.Name
                DirectoryName = file.DirectoryName
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
                FileSize = file.Length
                ImageCount = fileTags.Tag.Pictures.Length
                LastWriteTime = DateTimeOffset file.LastWriteTime
            }

       match parseFileTags file with
       | Ok (Some tags) -> tagsFromFile tags
       | _              -> blankTags file

    let prepareTagsToCache tagLibMap (audioFile: FileInfo) : NewLibTags =
        if tagLibMap |> Map.containsKey audioFile.FullName
        then
            let libTags = tagLibMap |> Map.find audioFile.FullName
            match compareWith libTags.LastWriteTime.DateTime audioFile.LastWriteTime with
            | EQ -> { Status = UpToDate;  Tags = Some (copyCachedTags libTags) }
            | _  -> { Status = OutOfSync; Tags = Some (generateNewTags audioFile) }
        else { Status = NewFile; Tags = Some (generateNewTags audioFile) }

    audioFiles |> NSeq.map (prepareTagsToCache libMap)

let private addDeletedFiles tagLibMap groupedNewLibTags : NewLibTags nseq =
    let filePaths =
        groupedNewLibTags
        |> NSeq.choose (fun t -> t.Tags |> Option.map filePath)
        |> set

    let orphanedLibFiles = // Only used for counts.
        tagLibMap
        |> Map.filter (fun libPath _ -> not (filePaths |> Set.contains libPath))
        |> Map.values
        |> Seq.map (fun _ -> { Status = FileDeleted; Tags = None })
        |> NSeq.tryOfSeq

    match orphanedLibFiles with
    | Some t -> groupedNewLibTags |> NSeq.append t
    | None   -> groupedNewLibTags

let private printCounts groupedNewLibTags : unit =
    let categoryTotals =
        groupedNewLibTags
        |> NSeq.countBy _.Status
        |> Map.ofSeq

    let countOf category =
        categoryTotals
        |> Map.tryFindElse category 0
        |> String.formatInt

    let libTagCount =
        categoryTotals
        |> Map.filter (fun x _ -> not x.IsFileDeleted)
        |> Map.values
        |> sum
        |> String.formatInt

    printfn "Results:"
    printfn "+ New:         %s" (countOf NewFile)
    printfn "+ Out of sync: %s" (countOf OutOfSync)
    printfn "+ Unchanged:   %s" (countOf UpToDate)
    printfn "- Deleted:     %s" (countOf FileDeleted)
    printfn "= New Total:   %s" libTagCount

let generateJson tagMap audioFiles : Result<string, CommandError> =
    audioFiles
    |> generateNewLibTags tagMap
    |> addDeletedFiles tagMap
    |- printCounts
    |> map _.Tags
    |> String.toJson
    |!! JsonSerializationError
