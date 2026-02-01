module DuplicateFinder.Errors

open CCFSharpUtils.Library

type Error =
    | ArgCountError
    | IoFileMissing of string list
    | IoFileReadError of string
    | IoFileWriteError of string
    | SettingsParseError of string
    | TagParseError of string

let message = function
    | ArgCountError -> "Invalid arguments. Pass in two JSON file paths: (1) your settings file and (2) your tag library."
    | IoFileMissing errs ->
        match errs with
        | []    -> "An unspecified file was missing."
        | [err] -> err
        | errs  -> errs |> String.concatNL
    | IoFileReadError msg -> $"Read failure: {msg}"
    | IoFileWriteError msg -> $"Write failure: {msg}"
    | SettingsParseError msg -> $"Unable to parse the settings file: {msg}"
    | TagParseError msg -> $"Unable to parse the tag library file: {msg}"
