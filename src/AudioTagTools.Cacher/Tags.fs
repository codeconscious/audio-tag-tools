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
module NSeq  = NonEmptySeq

type private LibPathTagMap = Map<FilePath, LibraryTags>

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

let private countDeleted libMap categorizedTags : CheckedLibTags nseq * DeletedCount =
    let filePaths =
        categorizedTags
        |> NSeq.map (fun t -> filePath t.Tags)
        |> set

    let isAudioFileMissing libPath _ =
        filePaths |> Set.doesNotContain libPath

    let deletedCount =
        libMap
        |> Map.filter isAudioFileMissing
        |> Map.values
        |> _.Count
        |> uint
        |> DeletedCount

    (categorizedTags, deletedCount)

let private printCounts (categorizedTags, DeletedCount deletedCount) : unit =
    let categoryTotals =
        categorizedTags |> NSeq.countBy _.Status |> Map.ofSeq

    let newFileCount =
        categoryTotals.Values |> sum |> String.formatInt

    let countOf category =
        categoryTotals |> Map.tryFindElse category 0 |> String.formatInt

    printfn "Results:"
    printfn "  Deleted:     %s" (String.formatNumber deletedCount)
    printfn "  New:         %s" (countOf NewFile)
    printfn "  Out of sync: %s" (countOf OutOfSync)
    printfn "  Unchanged:   %s" (countOf Unchanged)
    printfn "  New Total:   %s" newFileCount

let generateJson tagMap audioFiles : Result<string, CommandError> =
    audioFiles
    |> categorize tagMap
    |> countDeleted tagMap
    |- printCounts
    |> (fst >> map _.Tags)
    |> String.toJson
    |!! JsonSerializationError
