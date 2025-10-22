module LibraryAnalysis.Library

open System
open ArgValidation
open Errors
open IO
open Shared
open FsToolkit.ErrorHandling

let private run (args: string array) : Result<unit, Error> =
    result {
        let! tagLibraryFile = validate args
        let! tags = tagLibraryFile |> readFile >>= parseJsonToTags

        printfn "Tag library analysis results:"
        printfn $"Tracks: {formatInt tags.Length}"

        let artists =
            tags
            |> Array.map (fun t ->
                Array.concat [| t.Artists; t.AlbumArtists |]
                |> Array.distinct
                |> Array.except [| String.Empty; " "; "Various Artists"; "<unknown>" |])
            |> Array.concat

        let uniqueArtists = artists |> Array.distinct
        printfn $"Unique artists: {formatInt uniqueArtists.Length}"

        let averageFileSize =
            let sizeTotal = tags |> Array.map _.FileSize |> Array.sum
            let fileCount = tags.Length
            sizeTotal / (int64 fileCount)
        printfn $"Average file size: %d{averageFileSize} bytes"

        let inline mostPopulous count (grouper: 'a -> 'a) (items: 'a array) =
            items
            |> Array.groupBy grouper
            |> Array.map (fun (_, group) -> (group[0], group.Length))
            |> Array.sortByDescending snd
            |> Array.truncate count

        let asLower (x: string) = x.ToLowerInvariant()
        let topTitles = mostPopulous 10 asLower (tags |> Array.map _.Title)
        let topGenres = mostPopulous 10 asLower (tags |> Array.map _.Genres |> Array.collect id)

        let mostCommonArtists =
            tags
            |> Array.map (fun t ->
                Array.concat [| t.Artists; t.AlbumArtists |]
                |> Array.distinct
                |> Array.except [| String.Empty; " "; "Various Artists"; "<unknown>" |])
            |> Array.concat
            |> mostPopulous 15 id
        printfn "Top 15 artists:"
        mostCommonArtists |> Array.iteri (fun i (artist, count) -> printfn $"   • {i + 1} {artist}  {formatInt count}")

        let largestFiles =
            tags
            |> Array.sortByDescending _.FileSize
            |> Array.truncate 10

        printfn "Top 10 genres:"
        topGenres |> Array.iter (fun (genre, count) -> printfn $"   • {genre}  {formatInt count}")

        printfn "Top 10 titles:"
        topTitles |> Array.iter (fun (title, count) -> printfn $"   • {title}  {formatInt count}")

        printfn "Top 10 largest files:"
        largestFiles |> Array.iter (fun f ->
            let artist = String.concat ", " f.Artists
            printfn $"   • {artist} / {f.Title}  {formatBytes f.FileSize}")

        let qualityData =
            tags
            |> Array.map (fun t -> {| BitRate = t.BitRate
                                      SampleRate = t.SampleRate
                                      Extension = (Path.GetExtension t.FileName)[1..] |> _.ToUpperInvariant() |} )

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

        let haveAlbumArtCount =
            tags
            |> Array.choose (fun t -> if t.ImageCount > 0 then Some t.ImageCount else None)
            |> Array.length
            |> fun count -> float count / float tags.Length * 100.
        printfn $"With album art: %s{formatFloat haveAlbumArtCount}%%"
    }

let start (args: string array) : Result<string, string> =
    match run args with
    | Ok _ -> Ok "Finished analysis successfully!"
    | Error e -> Error (message e)
