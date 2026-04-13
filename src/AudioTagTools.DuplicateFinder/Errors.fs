module DuplicateFinder.Errors

open CCFSharpUtils

type CommandError =
    | ArgCountError
    | ArgErrors of string nlist
    | FileReadError of string
    | FileWriteError of string
    | SettingParseError of string
    | TagParseError of string
    | NoFilesRemainAfterFiltering

let message = function
    | ArgCountError -> "Invalid arguments. Pass in two JSON file paths: (1) your settings file and (2) your tag library."
    | ArgErrors errs -> errs |> String.concatNL
    | FileReadError msg -> $"I/O read failure: {msg}"
    | FileWriteError msg -> $"I/O write failure: {msg}"
    | SettingParseError msg -> $"Failure parsing settings file: {msg}"
    | TagParseError msg -> $"Failure parsing tag library file: {msg}"
    | NoFilesRemainAfterFiltering -> "No files to check remained after filtering."
