module DuplicateFinder.Errors

open CCFSharpUtils.Library

type Error =
    | ArgCountError
    | IoFileMissing of string list
    | ReadFileError of string
    | WriteFileError of string
    | SettingsParseError of string
    | TagParseError of string

let message = function
    | ArgCountError -> "Invalid arguments. Pass in two JSON file paths: (1) your settings file and (2) your tag library."
    | IoFileMissing errs ->
        match errs with
        | []    -> "An unspecified file was missing."
        | [err] -> err
        | errs  -> errs |> String.concat String.newLine
    | ReadFileError msg -> $"Read failure: {msg}"
    | WriteFileError msg -> $"Write failure: {msg}"
    | SettingsParseError msg -> $"Unable to parse the settings file: {msg}"
    | TagParseError msg -> $"Unable to parse the tag library file: {msg}"
