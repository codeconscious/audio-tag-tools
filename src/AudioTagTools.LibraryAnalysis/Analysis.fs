module LibraryAnalysis.Analysis

open Shared.TagLibrary
open Shared.Utilities

let inline mostPopulous count (grouper: 'a -> 'a) (items: 'a array) =
    items
    |> Array.groupBy grouper
    |> Array.map (fun (_, group) -> (group[0], group.Length))
    |> Array.sortByDescending snd
    |> Array.truncate count

let albumArtPercentage (tags: LibraryTags array) =
    tags
    |> Array.choose (fun t -> if t.ImageCount > 0 then Some t.ImageCount else None)
    |> Array.length
    |> fun count -> float count / float tags.Length * 100.
    |> fun x -> $"With album art: %s{formatFloat x}%%"

let topBitRates tags =
    tags
    |> Array.map _.BitRate
    |> mostPopulous 10 id
    |> Array.map (fun (bitrate, count) -> $"   • {bitrate}  {formatInt count}")

(*

        let topBitRates = qualityData |> Array.map _.BitRate |> mostPopulous 10 id
        printfn "Top 10 bitrates:"
        topBitRates |> Array.iter (fun (bitrate, count) -> printfn $"   • {bitrate}  {formatInt count}")

        let topSampleRates = qualityData |> Array.map _.SampleRate |> mostPopulous 10 id
        printfn "Top 10 sample rates:"
        topSampleRates |> Array.iter (fun (sampleRate, count) -> printfn $"   • {formatInt sampleRate}  {formatInt count}")

        let topFormats = qualityData |> Array.map _.Extension |> mostPopulous 10 id
        printfn "Top 10 extensions:"
        topFormats |> Array.iter (fun (ext, count) -> printfn $"   • {ext}  {formatInt count}")

        let topCombo = qualityData |> mostPopulous 10 id
        printfn "Top 10 combos:"
        topCombo |> Array.iter (fun (x, count) -> printfn $"   • {x.Extension}, {x.BitRate}, {x.SampleRate} -> {formatInt count}")

*)
