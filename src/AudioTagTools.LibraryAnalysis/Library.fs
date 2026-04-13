module LibraryAnalysis.Library

open Analysis
open ArgValidation
open Errors
open Shared
open Shared.TagLibrary
open CCFSharpUtils
open CCFSharpUtils.Operators
open Spectre.Console
open FSharpPlus
open FsToolkit.ErrorHandling.Operator.Result

type QualityData =
    { BitRate: int
      Extension: string
      SampleRate: int }

let private run (args: string array) : Result<unit, AnalysisError> =
    monad' {
        let! tagLibraryFile = validate args
        let! tags = tagLibraryFile |> File.readText' >>= parseJsonToTags |! TagParseError

        printTable {
            Title = Some "General Data"
            Headers = None
            Rows =
                [ [ "Track count"; String.formatInt tags.Length ]
                  [ "Unique artists"; String.formatInt <| uniqueArtistCount tags ]
                  [ "Average file size"; String.formatBytes <| averageFileSize tags  ]
                  [ "Album art percentage"; $"%s{albumArtPercentage tags}" ] ]
            ColumnAlignments = [Justify.Left; Justify.Right]
            ShowRowSeparators = false
        }

        printTable {
            Title = Some "Top Artists"
            Headers = Some ["Artist"; "Count"; "Ratio"]
            Rows = topArtists 30 tags
            ColumnAlignments = [Justify.Left; Justify.Right; Justify.Right]
            ShowRowSeparators = false
        }

        printTable {
            Title = Some "Top Album Names"
            Headers = Some ["Album"; "Count"; "Ratio"]
            Rows = topAlbums 30 tags
            ColumnAlignments = [Justify.Left; Justify.Right; Justify.Right]
            ShowRowSeparators = false
        }

        printTable {
            Title = Some "Top Titles"
            Headers = Some ["Title"; "Count"]
            Rows = topTitles 30 tags
            ColumnAlignments = [Justify.Left; Justify.Right]
            ShowRowSeparators = false
        }

        printTable {
            Title = Some "Top Genres"
            Headers = Some ["Genre"; "Count"; "Ratio"]
            Rows = topGenres 30 tags
            ColumnAlignments = [Justify.Left; Justify.Right; Justify.Right]
            ShowRowSeparators = false
        }

        printTable {
            Title = Some "Artists With The Most Genres"
            Headers = Some ["Artist"; "Count"; "Genres"]
            Rows = artistsWithMostGenres 20 tags
            ColumnAlignments = [Justify.Left; Justify.Right; Justify.Left]
            ShowRowSeparators = false
        }

        printTable {
            Title = Some "Largest Files"
            Headers = Some ["File Size"; "Artist & Title"]
            Rows = largestFiles 20 tags |> List.map List.rev
            ColumnAlignments = [Justify.Right; Justify.Left]
            ShowRowSeparators = false
        }

        printTable {
            Title = Some "Top Formats"
            Headers = Some ["Extension"; "Count"]
            Rows = topFormats 10 tags
            ColumnAlignments = [Justify.Left; Justify.Right]
            ShowRowSeparators = false
        }

        printTable {
            Title = Some "Top Bitrates"
            Headers = Some ["Bitrate"; "Count"]
            Rows = topBitRates 10 tags
            ColumnAlignments = [Justify.Right; Justify.Right]
            ShowRowSeparators = false
        }

        printTable {
            Title = Some "Top Sample Rates"
            Headers = Some ["Sample Rate"; "Count"]
            Rows = topSampleRates 10 tags
            ColumnAlignments = [Justify.Right; Justify.Right]
            ShowRowSeparators = false
        }

        printTable {
            Title = Some "Top Quality Combinations"
            Headers = Some ["Format"; "Bit Rate"; "Sample Rate"; "Count"]
            Rows = topQualityData 10 tags
            ColumnAlignments = [Justify.Left; Justify.Right; Justify.Right; Justify.Right]
            ShowRowSeparators = false
        }

        printTable {
            Title = Some "Longest File Names"
            Headers = Some ["Artist & Title"; "Filename"; "Length"]
            Rows = longestFileNames 5 tags
            ColumnAlignments = [Justify.Left; Justify.Left; Justify.Center]
            ShowRowSeparators = true
        }
    }

let start args : Result<string, string> =
    match run args with
    | Ok ()   -> Ok "Finished analysis successfully."
    | Error e -> Error (message e)
