module LibraryAnalysis.Library

open Analysis
open System
open ArgValidation
open Errors
open IO
open Shared
open FsToolkit.ErrorHandling
open Spectre.Console

type QualityData = { BitRate: int; Extension: string; SampleRate: int }

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

        // let qualityData =
        //     tags
        //     |> Array.map (fun t -> { BitRate = t.BitRate
        //                              SampleRate = t.SampleRate
        //                              Extension = (Path.GetExtension t.FileName)[1..] |> _.ToUpperInvariant() } )

        printTable {
            Title = Some "Top bitrates"
            Headers = Some ["Bitrate"; "Count"]
            Rows = Some (topBitRates tags)
            ColumnAlignments = [Justify.Right; Justify.Right]
        }

        albumArtPercentage tags |> printfn "%s"
    }

let start (args: string array) : Result<string, string> =
    match run args with
    | Ok _ -> Ok "Finished analysis successfully!"
    | Error e -> Error (message e)
