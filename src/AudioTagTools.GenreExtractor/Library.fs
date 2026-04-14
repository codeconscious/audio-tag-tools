module GenreExtractor.Library

open ArgValidation
open Errors
open Exporting
open IO
open Shared.TagLibrary
open Shared.Types
open Shared.Constants
open CCFSharpUtils
open CCFSharpUtils.Operators
open FSharpPlus

// The separator character should be rare and highly unlikely to appear in files' tags.
let private separator = "＼"

let private run args : Result<unit, CommandError> =
    monad' {
        let! tagLibFile, genreFile = validate args

        let! oldGenres = genreFile |> readGenres |. printOldSummary
        let! jsonTags = tagLibFile |> File.readText' |>> Json |! FileReadError
        let! parsedTags = jsonTags |> parseJsonToNonEmptyTags |. printTagCount |! TagParseError
        let! newGenres = parsedTags |> generateGenreData separator

        printChanges oldGenres newGenres

        do!
            backUpFile genreFile
            |>> printfn "Created backup file \"%O\"." // %O formats with ToString().
            |!  FileWriteError

        return!
            newGenres
            |> File.writeLines' genreFile
            |! FileWriteError
    }

let start args : Result<string, string> =
    match run args with
    | Ok ()   -> Ok "Finished exporting genres successfully."
    | Error e -> Error (message e)
