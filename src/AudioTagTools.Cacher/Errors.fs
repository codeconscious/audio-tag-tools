module Cacher.Errors

type Error =
    | ArgInvalidCount
    | MediaDirectoryMissing of string
    | FileReadError of string
    | FileWriteError of string
    | NoFilesFound of string
    | GeneralIoError of string
    | LibraryTagParseError of string
    | FileTagParseError of string
    | JsonSerializationError of string

let message = function
    | ArgInvalidCount -> "Invalid arguments. Pass in (1) the directory containing your audio files and (2) a path to a JSON file containing cached tag data."
    | MediaDirectoryMissing dir -> $"Directory \"{dir}\" was not found."
    | FileReadError msg -> $"Read failure: {msg}"
    | FileWriteError msg -> $"Write failure: {msg}"
    | NoFilesFound dir -> $"No files found in \"{dir}\"."
    | GeneralIoError msg -> $"I/O failure: {msg}"
    | LibraryTagParseError msg -> $"Library tag parse error: {msg}"
    | FileTagParseError msg -> $"File tag parse error: {msg}"
    | JsonSerializationError msg -> $"JSON serialization error: {msg}"
