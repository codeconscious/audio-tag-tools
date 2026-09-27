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

type private ComparisonResult = Unchanged | OutOfSync | NewFile

type private CheckedLibTags = { Status: ComparisonResult; Tags: LibraryTags }

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

// TODO: Likely to relocate this to TagLibrary as well.
let private checkAudioFiles libMap audioFiles : CheckedLibTags nseq =
    let prepareTagsToCache tagLibMap (audioFile: FileInfo) : CheckedLibTags =
        match tagLibMap |> Map.tryFind audioFile.FullName with
        | Some libTags ->
            match libTags.LastWriteTime.DateTime </compare'/> audioFile.LastWriteTime with
            | EQ -> { Status = Unchanged; Tags = LibraryTags.Copy libTags }
            | _  -> { Status = OutOfSync; Tags = LibraryTags.Generate audioFile }
        | None ->   { Status = NewFile;   Tags = LibraryTags.Generate audioFile }

    audioFiles |> NSeq.map (prepareTagsToCache libMap)

let private countDeletedFiles libMap categorizedTags : CheckedLibTags nseq * DeletedCount =
    let filePaths = categorizedTags |> NSeq.map (fun t -> filePath t.Tags) |> set

    let deletedCount =
        libMap
        |> Map.filter (fun libPath _ -> not (filePaths |> Set.contains libPath))
        |> Map.values
        |> _.Count
        |> uint
        |> DeletedCount

    (categorizedTags, deletedCount)

let private printCounts (categorizedTags, DeletedCount deletedCount) : unit =
    let categoryTotals = categorizedTags |> NSeq.countBy _.Status |> Map.ofSeq

    let newItemCount = categoryTotals |> Map.values |> sum |> String.formatInt

    let countOf category = categoryTotals |> Map.tryFindElse category 0 |> String.formatInt

    printfn "Results:"
    printfn "  Deleted:     %s" (String.formatNumber deletedCount)
    printfn "  New:         %s" (countOf NewFile)
    printfn "  Out of sync: %s" (countOf OutOfSync)
    printfn "  Unchanged:   %s" (countOf Unchanged)
    printfn "  New Total:   %s" newItemCount

let generateJson tagMap audioFiles : Result<string, CommandError> =
    audioFiles
    |> checkAudioFiles tagMap
    |> countDeletedFiles tagMap
    |- printCounts
    |> (fst >> map _.Tags)
    |> String.toJson
    |!! JsonSerializationError
