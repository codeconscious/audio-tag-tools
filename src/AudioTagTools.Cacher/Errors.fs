module Cacher.Errors

type Error =
    | InvalidArgCount
    | MediaDirectoryMissing of string
    | ReadFileError of string
    | WriteFileError of string
    | GeneralIoError of string
    | NoFilesFound of string
    | LibraryTagParseError of string
    | FileTagParseError of string
    | JsonSerializationError of string

let message = function
    | InvalidArgCount -> "Invalid arguments. Pass in (1) the directory containing your audio files and (2) a path to a JSON file containing cached tag data."
    | MediaDirectoryMissing dir -> $"Directory \"{dir}\" was not found."
    | ReadFileError msg -> $"Read failure: {msg}"
    | WriteFileError msg -> $"Write failure: {msg}"
    | GeneralIoError msg -> $"I/O failure: {msg}"
    | NoFilesFound dir -> $"No files found in \"{dir}\"."
    | LibraryTagParseError msg -> $"Library tag parse error: {msg}"
    | FileTagParseError msg -> $"File tag parse error: {msg}"
    | JsonSerializationError msg -> $"JSON serialization error: {msg}"
