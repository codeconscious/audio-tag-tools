module LibraryAnalysis.Library

open Analysis
open System
open ArgValidation
open Errors
open IO
open Shared
open FsToolkit.ErrorHandling
open Spectre.Console

// type Justify = Spectre.Console.Justify

type QualityData = {
    BitRate: int
    Extension: string
    SampleRate: int
}

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

        printTable {
            Title = Some "Top Artists"
            Headers = Some ["Artist"; "Count"]
            Rows = Some (topArtists 20 tags)
            ColumnAlignments = [Justify.Left; Justify.Right]
        }

        printTable {
            Title = Some "Top Genres"
            Headers = Some ["Genre"; "Count"]
            Rows = Some (topGenres 10 tags)
            ColumnAlignments = [Justify.Left; Justify.Right]
        }

        printTable {
            Title = Some "Top Titles"
            Headers = Some ["Title"; "Count"]
            Rows = Some (topTitles 20 tags)
            ColumnAlignments = [Justify.Left; Justify.Right]
        }

        printTable {
            Title = Some "Largest files"
            Headers = Some ["Artist & Title"; "File Size"]
            Rows = Some (largestFiles 10 tags)
            ColumnAlignments = [Justify.Left; Justify.Right]
        }

        printTable {
            Title = Some "Top Formats"
            Headers = Some ["Extension"; "Count"]
            Rows = Some (topFormats 10 tags)
            ColumnAlignments = [Justify.Left; Justify.Right]
        }

        printTable {
            Title = Some "Top Bitrates"
            Headers = Some ["Bitrate"; "Count"]
            Rows = Some (topBitRates 10 tags)
            ColumnAlignments = [Justify.Right; Justify.Right]
        }

        printTable {
            Title = Some "Top Sample Rates"
            Headers = Some ["Sample Rate"; "Count"]
            Rows = Some (topSampleRates 10 tags)
            ColumnAlignments = [Justify.Right; Justify.Right]
        }

        printTable {
            Title = Some "Most Common Quality Cominbations"
            Headers = Some ["Format"; "Bit Rate"; "Sample Rate"; "Count"]
            Rows = Some (topQualityData 10 tags)
            ColumnAlignments = [Justify.Left; Justify.Right; Justify.Right; Justify.Right]
        }

        albumArtPercentage tags |> printfn "%s"
    }

let start (args: string array) : Result<string, string> =
    match run args with
    | Ok _ -> Ok "Finished analysis successfully!"
    | Error e -> Error (message e)
