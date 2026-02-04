module DuplicateFinder.Errors

open CCFSharpUtils.Library
open FSharpPlus.Data

type Error =
    | ArgCountError
    | IoFilesMissing of string NonEmptyList
    | IoFileReadError of string
    | IoFileWriteError of string
    | SettingsParseError of string
    | TagParseError of string

let message = function
    | ArgCountError -> "Invalid arguments. Pass in two JSON file paths: (1) your settings file and (2) your tag library."
    | IoFilesMissing errs -> errs |> String.concatNL
    | IoFileReadError msg -> $"I/O read failure: {msg}"
    | IoFileWriteError msg -> $"I/O write failure: {msg}"
    | SettingsParseError msg -> $"Failure parsing settings file: {msg}"
    | TagParseError msg -> $"Failure parsing tag library file: {msg}"
