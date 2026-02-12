module GenreExtractor.Errors

open CCFSharpUtils.Library
open FSharpPlus.Data

type Error =
    | ArgCountError
    | IoFilesMissing of string NonEmptyList
    | IoReadError of string
    | IoWriteError of string
    | TagParseError of string

let message = function
    | ArgCountError -> "Invalid arguments. Pass in (1) your tag library path and (2) the desired path for your exported genres file."
    | IoFilesMissing errs -> errs |> String.concatNL
    | IoReadError msg -> $"I/O read failure: {msg}"
    | IoWriteError msg -> $"I/O write failure: {msg}"
    | TagParseError msg -> $"Unable to parse the tag library file: {msg}"
