module DuplicateFinder.Errors

open CCFSharpUtils.Library
open FSharpPlus.Data

type Error =
    | ArgCountError
    | ArgErrors of string NonEmptyList
    | FileReadError of string
    | FileWriteError of string
    | SettingsParseError of string
    | TagParseError of string

let message = function
    | ArgCountError -> "Invalid arguments. Pass in two JSON file paths: (1) your settings file and (2) your tag library."
    | ArgErrors errs -> errs |> String.concatNL
    | FileReadError msg -> $"I/O read failure: {msg}"
    | FileWriteError msg -> $"I/O write failure: {msg}"
    | SettingsParseError msg -> $"Failure parsing settings file: {msg}"
    | TagParseError msg -> $"Failure parsing tag library file: {msg}"
