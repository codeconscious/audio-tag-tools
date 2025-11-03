module LibraryAnalysis.Library

open Analysis
open System
open ArgValidation
open Errors
open IO
open Shared
open FsToolkit.ErrorHandling
open Spectre.Console

type QualityData = {
    BitRate: int
    Extension: string
    SampleRate: int
}

let private run (args: string array) : Result<unit, Error> =
    result {
        let! tagLibraryFile = validate args
        let! tags = tagLibraryFile |> readFile >>= parseJsonToTags

        printTable {
            Title = Some "General Data"
            Headers = None
            Rows = [|
                [| "Track count"; formatInt tags.Length |]
                [| "Unique artists"; formatInt <| uniqueArtistCount tags |]
                [| "Average file size"; formatBytes <| averageFileSize tags  |]
                [| "Album art percentage"; $"%s{formatFloat <| albumArtPercentage tags}%%" |]
            |]
            ColumnAlignments = [Justify.Left; Justify.Right]
        }

        printTable {
            Title = Some "Top Artists"
            Headers = Some ["Artist"; "Count"]
            Rows = topArtists 30 tags
            ColumnAlignments = [Justify.Left; Justify.Right]
        }

        printTable {
            Title = Some "Top Album Names"
            Headers = Some ["Album"; "Count"]
            Rows = topAlbums 30 tags
            ColumnAlignments = [Justify.Left; Justify.Right]
        }

        printTable {
            Title = Some "Top Titles"
            Headers = Some ["Title"; "Count"]
            Rows = topTitles 30 tags
            ColumnAlignments = [Justify.Left; Justify.Right]
        }

        printTable {
            Title = Some "Top Genres"
            Headers = Some ["Genre"; "Count"]
            Rows = topGenres 30 tags
            ColumnAlignments = [Justify.Left; Justify.Right]
        }

        printTable {
            Title = Some "Artists With The Most Genres"
            Headers = Some ["Artist"; "Count"; "Genres"]
            Rows = artistsWithMostGenres 20 tags
            ColumnAlignments = [Justify.Left; Justify.Right; Justify.Left]
        }

        printTable {
            Title = Some "Largest files"
            Headers = Some ["File Size"; "Artist & Title"]
            Rows = largestFiles 20 tags |> Array.map Array.rev
            ColumnAlignments = [Justify.Right; Justify.Left]
        }

        printTable {
            Title = Some "Top Formats"
            Headers = Some ["Extension"; "Count"]
            Rows = topFormats 10 tags
            ColumnAlignments = [Justify.Left; Justify.Right]
        }

        printTable {
            Title = Some "Top Bitrates"
            Headers = Some ["Bitrate"; "Count"]
            Rows = topBitRates 10 tags
            ColumnAlignments = [Justify.Right; Justify.Right]
        }

        printTable {
            Title = Some "Top Sample Rates"
            Headers = Some ["Sample Rate"; "Count"]
            Rows = topSampleRates 10 tags
            ColumnAlignments = [Justify.Right; Justify.Right]
        }

        printTable {
            Title = Some "Top Quality Combinations"
            Headers = Some ["Format"; "Bit Rate"; "Sample Rate"; "Count"]
            Rows = topQualityData 10 tags
            ColumnAlignments = [Justify.Left; Justify.Right; Justify.Right; Justify.Right]
        }
    }

let start (args: string array) : Result<string, string> =
    match run args with
    | Ok _ -> Ok "Finished analysis successfully!"
    | Error e -> Error (message e)
