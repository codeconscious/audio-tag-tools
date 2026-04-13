module GenreExtractor.Errors

open CCFSharpUtils
open FSharpPlus.Data

type CommandError =
    | ArgCountError
    | ArgErrors of string nlist
    | FileReadError of string
    | FileWriteError of string
    | TagParseError of string
    | TagsMissing
    | InsufficientGenreData

let message = function
    | ArgCountError -> "Invalid arguments. Pass in (1) your tag library path and (2) the desired path for your exported genres file."
    | ArgErrors errs -> errs |> String.concatNL
    | FileReadError msg -> $"I/O read failure: {msg}"
    | FileWriteError msg -> $"I/O write failure: {msg}"
    | TagParseError msg -> $"Unable to parse the tag library file: {msg}"
    | TagsMissing -> "No tags found in the tag library file."
    | InsufficientGenreData -> "Insufficient genre data."
