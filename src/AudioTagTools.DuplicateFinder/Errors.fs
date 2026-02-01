module DuplicateFinder.Errors

open CCFSharpUtils.Library

type Error =
    | ArgValidationError of string
    | IoFileMissing of string
    | ReadFileError of string
    | WriteFileError of string
    | SettingsParseError of string
    | TagParseError of string

let message = function
    | ArgValidationError err -> $"Invalid arguments. Pass in two JSON file paths: (1) your settings file and (2) your tag library.{String.newLine}{err}"
    | IoFileMissing file -> $"The file \"{file}\" was not found."
    | ReadFileError msg -> $"Read failure: {msg}"
    | WriteFileError msg -> $"Write failure: {msg}"
    | SettingsParseError msg -> $"Unable to parse the settings file: {msg}"
    | TagParseError msg -> $"Unable to parse the tag library file: {msg}"
