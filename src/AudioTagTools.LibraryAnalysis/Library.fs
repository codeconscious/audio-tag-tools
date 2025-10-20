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

        let uniqueArtists =
            tags
            |> Array.map (fun t -> Array.concat [| t.Artists; t.AlbumArtists |])
            |> Array.concat
            |> Array.distinct
            |> _.Length

        let topGenres =
            tags
            |> Array.map _.Genres
            |> Array.collect id
            |> Array.filter (fun a -> not <| String.IsNullOrEmpty a)
            |> Array.groupBy _.ToLowerInvariant()
            |> Array.map (fun (_, group) -> (group[0], group.Length))
            |> Array.sortByDescending snd
            |> Array.take 10

        let largestFiles =
            tags
            |> Array.sortByDescending _.FileSize
            |> Array.take 10

        printfn "Tag library analysis results:"
        printfn $"Tracks: {formatInt tags.Length}"
        printfn $"Unique artists: {formatInt uniqueArtists}"

        printfn "Top 10 genres:"
        topGenres |> Array.iter (fun (genre, count) -> printfn $"   • {genre}  {formatInt count}")

        printfn "Top 10 largest files:"
        largestFiles |> Array.iter (fun f ->
            let artist = String.concat ", " f.Artists
            printfn $"   • {artist} / {f.Title}  {formatBytes f.FileSize}")
    }

let start (args: string array) : Result<string, string> =
    match run args with
    | Ok _ -> Ok "Finished analysis successfully!"
    | Error e -> Error (message e)
