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
module NSeq =  NonEmptySeq

type private LibPathTagMap = Map<FilePath, LibraryTags>
type private ComparisonResult = UpToDate | OutOfSync | NewFile
type private NewLibTags = { Status: ComparisonResult; Tags: LibraryTags }
type private DeletedCount = DeletedCount of uint

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
            | EQ -> { Status = UpToDate;  Tags = copyCachedTags libTags }
            | _  -> { Status = OutOfSync; Tags = generateNewTags audioFile }
        else { Status = NewFile; Tags = generateNewTags audioFile }

    audioFiles |> NSeq.map (prepareTagsToCache libMap)

let private countDeletedFiles libMap categorizedTags : NewLibTags nseq * DeletedCount =
    let filePaths = categorizedTags |> NSeq.map (fun t -> filePath t.Tags) |> set

    let deletedCount =
        libMap
        |> Map.filter (fun libPath _ -> not (filePaths |> Set.contains libPath))
        |> Map.values |> _.Count |> uint

    (categorizedTags, DeletedCount deletedCount)

let private printCounts (categorizedTags, DeletedCount deletedCount) : unit =
    let categoryTotals = categorizedTags |> NSeq.countBy _.Status |> Map.ofSeq

    let newItemCount = categoryTotals |> Map.values |> sum |> String.formatInt

    let countOf category = categoryTotals |> Map.tryFindElse category 0 |> String.formatInt

    printfn "Results:"
    printfn "  Deleted:     %s" (String.formatNumber deletedCount)
    printfn "  New:         %s" (countOf NewFile)
    printfn "  Out of sync: %s" (countOf OutOfSync)
    printfn "  Unchanged:   %s" (countOf UpToDate)
    printfn "  New Total:   %s" newItemCount

let generateJson tagMap audioFiles : Result<string, CommandError> =
    audioFiles
    |> generateNewLibTags tagMap
    |> countDeletedFiles tagMap
    |- printCounts
    |> (fst >> map _.Tags)
    |> String.toJson
    |!! JsonSerializationError
