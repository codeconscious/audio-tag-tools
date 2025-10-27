module LibraryAnalysis.Analysis

open System.IO
open Shared.TagLibrary
open Shared.Utilities

let inline mostPopulous count (grouper: 'a -> 'a) (items: 'a array) =
    items
    |> List.ofArray // TODO: Remove after refactoring library tags to a list.
    |> List.groupBy grouper
    |> List.map (fun (_, group) -> (group[0], group.Length))
    |> List.sortByDescending snd
    |> List.truncate count

let albumArtPercentage (tags: LibraryTags array) =
    tags
    |> Array.choose (fun t -> if t.ImageCount > 0 then Some t.ImageCount else None)
    |> Array.length
    |> fun count -> float count / float tags.Length * 100.
    |> fun x -> $"With album art: %s{formatFloat x}%%"

let topBitRates count tags =
    tags
    |> Array.map _.BitRate
    |> mostPopulous count id
    |> List.map (fun (bitrate, count) -> [|$"{bitrate} kbps"; formatInt count|])

let topSampleRates count tags =
    tags
    |> Array.map _.SampleRate
    |> mostPopulous count id
    |> List.map (fun (sampleRate, count) -> [|$"{formatInt sampleRate}"; formatInt count|])

let topFormats count tags =
    tags
    |> Array.map (fun t -> (Path.GetExtension t.FileName)[1..] |> _.ToUpperInvariant())
    |> mostPopulous count id
    |> List.map (fun (ext, count) -> [|$"{ext}"; formatInt count|])

let topQualityData count tags =
    tags
    |> Array.map (fun t -> {| BitRate = t.BitRate
                              SampleRate = t.SampleRate
                              Extension = (Path.GetExtension t.FileName)[1..] |> _.ToUpperInvariant() |} )
    |> mostPopulous count id
    |> List.map (fun (data, count) ->
        [|
           data.Extension
           $"{data.BitRate} kbps"
           formatInt data.SampleRate
           formatInt count
        |])

