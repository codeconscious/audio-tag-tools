module GenreExtractor.Errors

open CCFSharpUtils.Library
open FSharpPlus.Data

type Error =
    | ArgCountError
    | ArgErrors of string NonEmptyList
    | IoFileReadError of string
    | IoFileWriteError of string
    | TagParseError of string

let message = function
    | ArgCountError -> "Invalid arguments. Pass in (1) your tag library path and (2) the desired path for your exported genres file."
    | ArgErrors errs -> errs |> String.concatNL
    | IoFileReadError msg -> $"I/O read failure: {msg}"
    | IoFileWriteError msg -> $"I/O write failure: {msg}"
    | TagParseError msg -> $"Unable to parse the tag library file: {msg}"
