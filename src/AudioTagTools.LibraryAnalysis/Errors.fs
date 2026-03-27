module LibraryAnalysis.Errors

type Error =
    | ArgInvalidCount
    | FileMissing of string
    | FileReadError of string
    | TagParseError of string

let message = function
    | ArgInvalidCount -> "Invalid arguments. Pass only your tag library path."
    | FileMissing file -> $"File \"{file}\" was not found."
    | FileReadError msg -> $"I/O read failure: {msg}"
    | TagParseError msg -> $"Unable to parse the tag library file: {msg}"
